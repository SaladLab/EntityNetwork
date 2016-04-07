using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CodeWriter;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeGen
{
    internal class EntityCodeGenerator
    {
        public Options Options { get; set; }

        public void GenerateCode(InterfaceDeclarationSyntax idecl, CodeWriter.CodeWriter w)
        {
            var iname = idecl.Identifier.ToString();
            Console.WriteLine("GenerateCode: " + iname);

            w._($"#region {iname}");
            w._();

            var namespaceScope = idecl.GetNamespaceScope();
            var namespaceHandle = (string.IsNullOrEmpty(namespaceScope) == false)
                                      ? w.B($"namespace {idecl.GetNamespaceScope()}")
                                      : null;

            // Collect all methods and make payload type name for each one

            var methods = GetMethods(idecl);
            var method2PayloadTypeNameMap = GetPayloadTypeName(idecl, methods);

            // Collect all properties

            var snapshotProperty = GetSnapshotProperty(idecl);
            var trackableProperties = GetTrackableProperties(idecl);

            // Generate all

            var hasServerOnlyAttribute = idecl.AttributeLists.GetAttribute("ServerOnlyAttribute") != null;

            GeneratePayloadCode(idecl, w, methods, method2PayloadTypeNameMap,
                                snapshotProperty, trackableProperties);

            GenerateServerEntityBaseCode(idecl, w, methods, method2PayloadTypeNameMap,
                                         snapshotProperty, trackableProperties);

            if (hasServerOnlyAttribute == false)
            {
                GenerateClientEntityBaseCode(idecl, w, methods, method2PayloadTypeNameMap,
                                             snapshotProperty, trackableProperties);
            }

            namespaceHandle?.Dispose();

            w._();
            w._($"#endregion");
        }

        private void GeneratePayloadCode(
            InterfaceDeclarationSyntax idecl, CodeWriter.CodeWriter w,
            MethodDeclarationSyntax[] methods, Dictionary<MethodDeclarationSyntax, string> method2PayloadTypeNameMap,
            PropertyDeclarationSyntax snapshotProperty, PropertyDeclarationSyntax[] trackableProperties)
        {
            var className = Utility.GetPayloadTableClassName(idecl);

            w._($"[PayloadTableForEntity(typeof({idecl.Identifier}))]");
            using (w.B($"public static class {className}"))
            {
                // generate GetPayloadTypes method

                using (w.B("public static Type[] GetPayloadTypes()"))
                {
                    using (w.I("return new Type[] {", "};"))
                    {
                        foreach (var method in methods)
                            w._($"typeof({method2PayloadTypeNameMap[method]}),");
                    }
                }

                // generate payload classes for all methods

                foreach (var method in methods)
                {
                    var payloadTypeName = method2PayloadTypeNameMap[method];

                    if (Options.UseProtobuf)
                        w._("[ProtoContract, TypeAlias]");

                    using (w.B($"public class {payloadTypeName} : IInvokePayload"))
                    {
                        // Parameters

                        var parameters = method.ParameterList.Parameters;
                        for (var i = 0; i < parameters.Count; i++)
                        {
                            var parameter = parameters[i];

                            var attr = "";
                            var defaultValueExpression = "";

                            if (Options.UseProtobuf)
                            {
                                var defaultValueAttr =
                                    parameter.Default != null
                                        ? $", DefaultValue({parameter.Default.Value})"
                                        : "";
                                attr = $"[ProtoMember({i + 1}){defaultValueAttr}] ";

                                if (parameter.Default != null)
                                {
                                    defaultValueExpression = " " + parameter.Default;
                                }
                            }

                            var typeName = parameter.Type;
                            w._($"{attr}public {typeName} {parameter.Identifier}{defaultValueExpression};");
                        }

                        if (parameters.Any())
                            w._();

                        // Flags

                        var hasPassThroughAttribute = method.AttributeLists.GetAttribute("PassThroughAttribute") != null;
                        var hasToClientAttribute = method.AttributeLists.GetAttribute("ToClientAttribute") != null;
                        var hasToServerAttribute = method.AttributeLists.GetAttribute("ToServerAttribute") != null;
                        var hasAnyoneCanCallAttribute = method.AttributeLists.GetAttribute("AnyoneCanCallAttribute") != null;

                        var flags = new List<string>();
                        if (hasPassThroughAttribute)
                            flags.Add("PayloadFlags.PassThrough");
                        if (hasToClientAttribute)
                            flags.Add("PayloadFlags.ToServer");
                        if (hasToServerAttribute)
                            flags.Add("PayloadFlags.ToClient");
                        if (hasAnyoneCanCallAttribute)
                            flags.Add("PayloadFlags.AnyoneCanCall");
                        if (flags.Count == 0)
                            flags.Add("0");
                        w._($"public PayloadFlags Flags {{ get {{ return {string.Join(" | ", flags)}; }} }}");
                        w._();

                        // GetInterfaceType

                        var excludeServer = hasToClientAttribute || hasPassThroughAttribute;
                        var excludeClient = hasToServerAttribute;
                        var interfaceBodyName = idecl.Identifier.ToString().Substring(1);

                        /*
                        w._($"public Type GetInterfaceType() {{ return typeof({idecl.Identifier}); }}");
                        if (excludeServer)
                            w._($"public Type GetServerInterfaceType() {{ return null; }}");
                        else
                            w._($"public Type GetServerInterfaceType() {{ return typeof(IServer{interfaceBodyName}); }}");
                        if (excludeClient)
                            w._($"public Type GetClientInterfaceType() {{ return null; }}");
                        else
                            w._($"public Type GetClientInterfaceType() {{ return typeof(IClient{interfaceBodyName}); }}");
                        sb.AppendLine();
                        */

                        // Invoke

                        var parameterNames = string.Join(", ", parameters.Select(p => p.Identifier));

                        using (w.B("public void InvokeServer(IEntityServerHandler target)"))
                        {
                            if (excludeServer == false)
                                w._($"((I{interfaceBodyName}ServerHandler)target).On{method.Identifier}({parameterNames});");
                        }

                        using (w.B("public void InvokeClient(IEntityClientHandler target)"))
                        {
                            if (excludeClient == false)
                                w._($"((I{interfaceBodyName}ClientHandler)target).On{method.Identifier}({parameterNames});");
                        }
                    }
                }

                // generate a payload class for spawn

                if (snapshotProperty != null || (trackableProperties != null && trackableProperties.Length > 0))
                {
                    GeneratePayloadCodeForSpawn(idecl, w, snapshotProperty, trackableProperties);
                }

                // generate a payload class for update-change

                if (trackableProperties != null && trackableProperties.Length > 0)
                {
                    GeneratePayloadCodeForUpdateChange(idecl, w, trackableProperties);
                }
            }
        }

        private void GeneratePayloadCodeForSpawn(
            InterfaceDeclarationSyntax idecl, CodeWriter.CodeWriter w,
            PropertyDeclarationSyntax snapshotProperty, PropertyDeclarationSyntax[] trackableProperties)
        {
            if (Options.UseProtobuf)
                w._("[ProtoContract, TypeAlias]");

            using (w.B("public class Spawn : ISpawnPayload"))
            {
                // Members

                var protobufMemberIndex = 0;

                if (trackableProperties != null)
                {
                    foreach (var p in trackableProperties)
                    {
                        var attr = "";
                        if (Options.UseProtobuf)
                        {
                            protobufMemberIndex += 1;
                            attr = $"[ProtoMember({protobufMemberIndex})] ";
                        }

                        w._($"{attr}public Trackable{p.Type.ToString().Substring(1)} {p.Identifier};");
                    }
                }

                if (snapshotProperty != null)
                {
                    var attr = "";
                    if (Options.UseProtobuf)
                    {
                        protobufMemberIndex += 1;
                        attr = $"[ProtoMember({protobufMemberIndex})] ";
                    }

                    w._($"{attr}public {snapshotProperty.Type} {snapshotProperty.Identifier};");
                }

                w._();

                // Notify

                using (w.B("public void Gather(IServerEntity entity)"))
                {
                    w._($"var e = ({Utility.GetServerEntityBaseClassName(idecl)})entity;");

                    if (trackableProperties != null)
                    {
                        foreach (var p in trackableProperties)
                        {
                            w._($"{p.Identifier} = e.{p.Identifier};");
                        }
                    }

                    if (snapshotProperty != null)
                    {
                        w._($"{snapshotProperty.Identifier} = e.OnSnapshot();");
                    }
                }

                // Gather

                using (w.B("public void Notify(IClientEntity entity)"))
                {
                    w._($"var e = ({Utility.GetClientEntityBaseClassName(idecl)})entity;");

                    if (trackableProperties != null)
                    {
                        foreach (var p in trackableProperties)
                            w._($"e.{p.Identifier} = {p.Identifier};");
                    }

                    if (snapshotProperty != null)
                        w._($"e.OnSnapshot({snapshotProperty.Identifier});");
                }
            }
        }

        private void GeneratePayloadCodeForUpdateChange(
            InterfaceDeclarationSyntax idecl, CodeWriter.CodeWriter w,
            PropertyDeclarationSyntax[] trackableProperties)
        {
            if (Options.UseProtobuf)
                w._("[ProtoContract, TypeAlias]");

            using (w.B("public class UpdateChange : IUpdateChangePayload"))
            {
                // Members

                var protobufMemberIndex = 0;
                foreach (var p in trackableProperties)
                {
                    var attr = "";
                    if (Options.UseProtobuf)
                    {
                        protobufMemberIndex += 1;
                        attr = $"[ProtoMember({protobufMemberIndex})] ";
                    }

                    w._($"{attr}public {Utility.GetTrackerClassName(p.Type)} {p.Identifier}Tracker;");
                }

                w._();

                // Gather

                using (w.B("public void Gather(IServerEntity entity)"))
                {
                    w._($"var e = ({Utility.GetServerEntityBaseClassName(idecl)})entity;");

                    foreach (var p in trackableProperties)
                    {
                        using (w.b($"if (e.{p.Identifier}.Changed)"))
                        {
                            w._($"{p.Identifier}Tracker = ({Utility.GetTrackerClassName(p.Type)})e.{p.Identifier}.Tracker;");
                        }
                    }
                }

                // Notify

                using (w.B("public void Notify(IClientEntity entity)"))
                {
                    w._($"var e = ({Utility.GetClientEntityBaseClassName(idecl)})entity;");

                    for (int i = 0; i < trackableProperties.Length; i++)
                    {
                        var p = trackableProperties[i];
                        using (w.b($"if ({p.Identifier}Tracker != null)"))
                        {
                            w._($"e.OnTrackableDataChanging({i}, {p.Identifier}Tracker);");
                            w._($"{p.Identifier}Tracker.ApplyTo(e.{p.Identifier});");
                            w._($"e.OnTrackableDataChanged({i}, {p.Identifier}Tracker);");
                        }
                    }
                }
            }
        }

        private void GenerateServerEntityBaseCode(
            InterfaceDeclarationSyntax idecl, CodeWriter.CodeWriter w,
            MethodDeclarationSyntax[] methods, Dictionary<MethodDeclarationSyntax, string> method2PayloadTypeNameMap,
            PropertyDeclarationSyntax snapshotProperty, PropertyDeclarationSyntax[] trackableProperties)
        {
            var payloadTableClassName = Utility.GetPayloadTableClassName(idecl);

            var interfaceName = "I" + idecl.Identifier.ToString().Substring(1) + "ServerHandler";
            var baseClassName = Utility.GetServerEntityBaseClassName(idecl);

            // Interface

            var interfaceMethods = methods.Where(
                m => m.AttributeLists.GetAttribute("PassThroughAttribute") == null &&
                     m.AttributeLists.GetAttribute("ToClientAttribute") == null).ToArray();

            using (w.B($"public interface {interfaceName} : IEntityServerHandler"))
            {
                foreach (var method in interfaceMethods)
                {
                    w._($"{method.ReturnType} On{method.Identifier}{method.ParameterList};");
                }
            }

            // ServerEntityBase class

            using (w.B($"public abstract class {baseClassName} : {Options.ServerEntity ?? "ServerEntity"}"))
            {
                if (trackableProperties.Length > 0)
                {
                    foreach (var property in trackableProperties)
                        w._($"public Trackable{property.Type.ToString().Substring(1)} {property.Identifier} {{ get; set; }}");

                    w._();

                    using (w.B($"protected {baseClassName}()"))
                    {
                        foreach (var property in trackableProperties)
                            w._($"{property.Identifier} = new Trackable{property.Type.ToString().Substring(1)}();");
                    }
                }

                if (snapshotProperty != null)
                {
                    w._($"public override object Snapshot {{ get {{ return OnSnapshot(); }} }}");
                    w._();
                    w._($"public abstract {snapshotProperty.Type} OnSnapshot();");
                    w._();
                }

                w._($"public override int TrackableDataCount {{ get {{ return {trackableProperties.Length}; }} }}");
                w._();

                using (w.B("public override ITrackable GetTrackableData(int index)"))
                {
                    for (int i = 0; i < trackableProperties.Length; i++)
                        w._($"if (index == {i}) return {trackableProperties[i].Identifier};");

                    w._("return null;");
                }

                using (w.B("public override void SetTrackableData(int index, ITrackable trackable)"))
                {
                    for (int i = 0; i < trackableProperties.Length; i++)
                    {
                        w._($"if (index == {i}) {trackableProperties[i].Identifier} = " +
                            $"(Trackable{trackableProperties[i].Type.ToString().Substring(1)})trackable;");
                    }
                }

                if (trackableProperties.Length > 0 || snapshotProperty != null)
                {
                    using (w.B("public override ISpawnPayload GetSpawnPayload()"))
                    {
                        w._($"var payload = new {payloadTableClassName}.Spawn();",
                            $"payload.Gather(this);",
                            $"return payload;");
                    }
                }

                if (trackableProperties.Length > 0)
                {
                    using (w.B("public override IUpdateChangePayload GetUpdateChangePayload()"))
                    {
                        w._($"var payload = new {payloadTableClassName}.UpdateChange();",
                            $"payload.Gather(this);",
                            $"return payload;");
                    }
                }

                var classMethods = methods.Where(
                    m => m.AttributeLists.GetAttribute("ToServerAttribute") == null).ToArray();

                foreach (var method in classMethods)
                {
                    var messageName = method2PayloadTypeNameMap[method];
                    var parameters = method.ParameterList.Parameters;

                    var parameterTypeNames = string.Join(", ", parameters.Select(p => p.ToString()));
                    var parameterInits = string.Join(", ", parameters.Select(p => p.Identifier + " = " + p.Identifier));

                    using (w.B($"public void {method.Identifier}({parameterTypeNames})"))
                    {
                        w._($"var payload = new {payloadTableClassName}.{messageName} {{ {parameterInits} }};",
                            $"SendInvoke(payload);");
                    }
                }
            }
        }

        private void GenerateClientEntityBaseCode(
            InterfaceDeclarationSyntax idecl, CodeWriter.CodeWriter w,
            MethodDeclarationSyntax[] methods, Dictionary<MethodDeclarationSyntax, string> method2PayloadTypeNameMap,
            PropertyDeclarationSyntax snapshotProperty, PropertyDeclarationSyntax[] trackableProperties)
        {
            var payloadTableClassName = Utility.GetPayloadTableClassName(idecl);

            var sb = new StringBuilder();
            var interfaceName = "I" + idecl.Identifier.ToString().Substring(1) + "ClientHandler";
            var baseClassName = Utility.GetClientEntityBaseClassName(idecl);

            // IEntityClientHandler

            var interfaceMethods = methods.Where(
                    m => m.AttributeLists.GetAttribute("ToServerAttribute") == null).ToArray();

            using (w.B($"public interface {interfaceName} : IEntityClientHandler"))
            {
                foreach (var method in interfaceMethods)
                    w._($"{method.ReturnType} On{method.Identifier}{method.ParameterList};");
            }

            // ClientEntityBase class

            using (w.B($"public abstract class {baseClassName} : {Options.ClientEntity ?? "ClientEntity"}"))
            {
                if (trackableProperties.Length > 0)
                {
                    foreach (var property in trackableProperties)
                        w._($"public Trackable{property.Type.ToString().Substring(1)} {property.Identifier} {{ get; set; }}");

                    w._();

                    using (w.B($"protected {baseClassName}()"))
                    {
                        foreach (var property in trackableProperties)
                            w._($"{property.Identifier} = new Trackable{property.Type.ToString().Substring(1)}();");
                    }
                }

                if (snapshotProperty != null)
                {
                    w._($"public override object Snapshot {{ set {{ OnSnapshot(({snapshotProperty.Type})value); }} }}");
                    w._();
                    w._($"public abstract void OnSnapshot({snapshotProperty.Type} snapshot);");
                    w._();
                }

                w._($"public override int TrackableDataCount {{ get {{ return {trackableProperties.Length}; }} }}");
                w._();

                using (w.B("public override ITrackable GetTrackableData(int index)"))
                {
                    for (int i = 0; i < trackableProperties.Length; i++)
                    {
                        w._($"if (index == {i}) return {trackableProperties[i].Identifier};");
                    }
                    w._("return null;");
                }

                using (w.B("public override void SetTrackableData(int index, ITrackable trackable)"))
                {
                    for (int i = 0; i < trackableProperties.Length; i++)
                    {
                        w._($"if (index == {i}) {trackableProperties[i].Identifier} = " +
                            $"(Trackable{trackableProperties[i].Type.ToString().Substring(1)})trackable;");
                    }
                }

                var classMethods = methods.Where(
                    m => m.AttributeLists.GetAttribute("ToClientAttribute") == null).ToArray();

                foreach (var method in classMethods)
                {
                    var messageName = method2PayloadTypeNameMap[method];
                    var parameters = method.ParameterList.Parameters;

                    var parameterTypeNames = string.Join(", ", parameters.Select(p => p.ToString()));
                    var parameterInits = string.Join(", ", parameters.Select(p => p.Identifier + " = " + p.Identifier));

                    using (w.B($"public void {method.Identifier}({parameterTypeNames})"))
                    {
                        w._($"var payload = new {payloadTableClassName}.{messageName} {{ {parameterInits} }};",
                            $"SendInvoke(payload);");
                    }
                }
            }
        }

        private MethodDeclarationSyntax[] GetMethods(InterfaceDeclarationSyntax idecl)
        {
            var methods = new List<MethodDeclarationSyntax>();
            foreach (var member in idecl.Members)
            {
                var method = member as MethodDeclarationSyntax;
                if (method != null)
                {
                    methods.Add(method);
                }
            }
            return methods.OrderBy(m => m, new MethodDeclarationSyntaxComparer()).ToArray();
        }

        private PropertyDeclarationSyntax GetSnapshotProperty(InterfaceDeclarationSyntax idecl)
        {
            foreach (var member in idecl.Members)
            {
                var p = member as PropertyDeclarationSyntax;
                if (p != null && p.Identifier.ToString() == "Snapshot")
                    return p;
            }
            return null;
        }

        private PropertyDeclarationSyntax[] GetTrackableProperties(InterfaceDeclarationSyntax idecl)
        {
            var properties = new List<PropertyDeclarationSyntax>();
            foreach (var member in idecl.Members)
            {
                var p = member as PropertyDeclarationSyntax;
                if (p != null && p.Identifier.ToString() != "Snapshot")
                {
                    properties.Add(p);
                }
            }
            return properties.ToArray();
        }

        private Dictionary<MethodDeclarationSyntax, string> GetPayloadTypeName(InterfaceDeclarationSyntax idecl, MethodDeclarationSyntax[] methods)
        {
            var method2PayloadTypeNameMap = new Dictionary<MethodDeclarationSyntax, string>();
            for (var i = 0; i < methods.Length; i++)
            {
                var method = methods[i];
                var ordinal = methods.Take(i).Count(m => m.Identifier.Text == method.Identifier.Text) + 1;
                var ordinalStr = (ordinal <= 1) ? "" : string.Format("_{0}", ordinal);

                method2PayloadTypeNameMap[method] = string.Format("{0}{1}_Invoke", method.Identifier, ordinalStr);
            }
            return method2PayloadTypeNameMap;
        }
    }
}

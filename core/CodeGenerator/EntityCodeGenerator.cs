using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeGen
{
    internal class EntityCodeGenerator
    {
        public Options Options { get; set; }

        public void GenerateCode(InterfaceDeclarationSyntax idecl, ICodeGenWriter writer)
        {
            var iname = idecl.Identifier.ToString();
            Console.WriteLine("GenerateCode: " + iname);

            writer.PushRegion(iname);
            writer.PushNamespace(idecl.GetNamespaceScope());

            // Collect all methods and make payload type name for each one

            var methods = GetMethods(idecl);
            var method2PayloadTypeNameMap = GetPayloadTypeName(idecl, methods);

            // Collect all properties

            var snapshotProperty = GetSnapshotProperty(idecl);
            var trackableProperties = GetTrackableProperties(idecl);

            // Generate all

            var hasServerOnlyAttribute = idecl.AttributeLists.GetAttribute("ServerOnlyAttribute") != null;

            GeneratePayloadCode(idecl, writer, methods, method2PayloadTypeNameMap,
                                snapshotProperty, trackableProperties);

            GenerateServerEntityBaseCode(idecl, writer, methods, method2PayloadTypeNameMap,
                                         snapshotProperty, trackableProperties);

            if (hasServerOnlyAttribute == false)
            {
                GenerateClientEntityBaseCode(idecl, writer, methods, method2PayloadTypeNameMap,
                                             snapshotProperty, trackableProperties);
            }

            writer.PopNamespace();
            writer.PopRegion();
        }

        private void GeneratePayloadCode(
            InterfaceDeclarationSyntax idecl, ICodeGenWriter writer,
            MethodDeclarationSyntax[] methods, Dictionary<MethodDeclarationSyntax, string> method2PayloadTypeNameMap,
            PropertyDeclarationSyntax snapshotProperty, PropertyDeclarationSyntax[] trackableProperties)
        {
            var sb = new StringBuilder();
            var className = Utility.GetPayloadTableClassName(idecl);

            sb.Append($"[PayloadTableForEntity(typeof({idecl.Identifier}))]\n");
            sb.Append($"public static class {className}\n");
            sb.Append("{\n");

            // generate GetPayloadTypes method

            sb.Append("\tpublic static Type[] GetPayloadTypes()\n");
            sb.Append("\t{\n");
            sb.Append("\t\treturn new Type[]\n");
            sb.Append("\t\t{\n");

            foreach (var method in methods)
            {
                var typeName = method2PayloadTypeNameMap[method];
                sb.Append($"\t\t\ttypeof({typeName}),\n");
            }

            sb.Append("\t\t};\n");
            sb.Append("\t}\n");

            // generate payload classes for all methods

            foreach (var method in methods)
            {
                var payloadTypeName = method2PayloadTypeNameMap[method];

                sb.AppendLine();

                if (Options.UseProtobuf)
                    sb.Append("\t[ProtoContract, TypeAlias]\n");

                sb.AppendFormat("\tpublic class {0} : IInvokePayload\n", payloadTypeName);
                sb.Append("\t{\n");

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
                    sb.Append($"\t\t{attr}public {typeName} {parameter.Identifier}{defaultValueExpression};\n");
                }

                if (parameters.Count > 0)
                    sb.AppendLine();

                // Flags

                var hasPassThroughAttribute = method.AttributeLists.GetAttribute("PassThroughAttribute") != null;
                var hasToClientAttribute = method.AttributeLists.GetAttribute("ToClientAttribute") != null;
                var hasToServerAttribute = method.AttributeLists.GetAttribute("ToServerAttribute") != null;
                var hasAnyoneCanCallAttribute = method.AttributeLists.GetAttribute("AnyoneCanCallAttribute") != null;

                var flags = new List<String>();
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
                sb.Append($"\t\tpublic PayloadFlags Flags {{ get {{ return {string.Join(" | ", flags)}; }} }}\n");
                sb.AppendLine();

                // GetInterfaceType

                var excludeServer = hasToClientAttribute || hasPassThroughAttribute;
                var excludeClient = hasToServerAttribute;
                var interfaceBodyName = idecl.Identifier.ToString().Substring(1);

                /*
                sb.Append($"\t\tpublic Type GetInterfaceType() {{ return typeof({idecl.Identifier}); }}\n");
                if (excludeServer)
                    sb.Append($"\t\tpublic Type GetServerInterfaceType() {{ return null; }}\n");
                else
                    sb.Append($"\t\tpublic Type GetServerInterfaceType() {{ return typeof(IServer{interfaceBodyName}); }}\n");
                if (excludeClient)
                    sb.Append($"\t\tpublic Type GetClientInterfaceType() {{ return null; }}\n");
                else
                    sb.Append($"\t\tpublic Type GetClientInterfaceType() {{ return typeof(IClient{interfaceBodyName}); }}\n");
                sb.AppendLine();
                */

                // Invoke

                var parameterNames = string.Join(", ", parameters.Select(p => p.Identifier));

                sb.Append("\t\tpublic void InvokeServer(IEntityServerHandler target)\n");
                sb.Append("\t\t{\n");
                if (excludeServer == false)
                    sb.AppendFormat("\t\t\t((I{0}ServerHandler)target).On{1}({2});\n", interfaceBodyName, method.Identifier, parameterNames);
                sb.Append("\t\t}\n");
                sb.AppendLine();

                sb.Append("\t\tpublic void InvokeClient(IEntityClientHandler target)\n");
                sb.Append("\t\t{\n");
                if (excludeClient == false)
                    sb.AppendFormat("\t\t\t((I{0}ClientHandler)target).On{1}({2});\n", interfaceBodyName, method.Identifier, parameterNames);
                sb.Append("\t\t}\n");

                sb.Append("\t}\n");
            }

            // generate a payload class for spawn

            if (snapshotProperty != null || (trackableProperties != null && trackableProperties.Length > 0))
            {
                sb.AppendLine();
                GeneratePayloadCodeForSpawn(idecl, sb, snapshotProperty, trackableProperties);
            }

            // generate a payload class for update-change

            if (trackableProperties != null && trackableProperties.Length > 0)
            {
                sb.AppendLine();
                GeneratePayloadCodeForUpdateChange(idecl, sb, trackableProperties);
            }

            sb.Append("}");

            writer.AddCode(sb.ToString());
        }

        private void GeneratePayloadCodeForSpawn(
            InterfaceDeclarationSyntax idecl, StringBuilder sb,
            PropertyDeclarationSyntax snapshotProperty, PropertyDeclarationSyntax[] trackableProperties)
        {
            if (Options.UseProtobuf)
                sb.Append("\t[ProtoContract, TypeAlias]\n");

            sb.AppendFormat("\tpublic class Spawn : ISpawnPayload\n");
            sb.Append("\t{\n");

            // Members

            var protobufMemberIndex = 0;

            if (trackableProperties != null)
            {
                foreach (var p in trackableProperties)
                {
                    sb.Append("\t\t");
                    if (Options.UseProtobuf)
                    {
                        protobufMemberIndex += 1;
                        sb.Append($"[ProtoMember({protobufMemberIndex})] ");
                    }

                    sb.Append($"public Trackable{p.Type.ToString().Substring(1)} {p.Identifier};\n");
                }
            }

            if (snapshotProperty != null)
            {
                sb.Append("\t\t");
                if (Options.UseProtobuf)
                {
                    protobufMemberIndex += 1;
                    sb.Append($"[ProtoMember({protobufMemberIndex})] ");
                }

                sb.Append($"public {snapshotProperty.Type} {snapshotProperty.Identifier};\n");
            }

            // Notify

            sb.AppendLine();
            sb.Append("\t\tpublic void Gather(IServerEntity entity)\n");
            sb.Append("\t\t{\n");
            sb.Append($"\t\t\tvar e = ({Utility.GetServerEntityBaseClassName(idecl)})entity;\n");

            if (trackableProperties != null)
            {
                foreach (var p in trackableProperties)
                {
                    sb.Append($"\t\t\t{p.Identifier} = e.{p.Identifier};\n");
                }
            }

            if (snapshotProperty != null)
            {
                sb.Append($"\t\t\t{snapshotProperty.Identifier} = e.OnSnapshot();\n");
            }

            sb.Append("\t\t}\n");

            // Gather

            sb.AppendLine();
            sb.Append("\t\tpublic void Notify(IClientEntity entity)\n");
            sb.Append("\t\t{\n");
            sb.Append($"\t\t\tvar e = ({Utility.GetClientEntityBaseClassName(idecl)})entity;\n");

            if (trackableProperties != null)
            {
                foreach (var p in trackableProperties)
                {
                    sb.Append($"\t\t\te.{p.Identifier} = {p.Identifier};\n");
                }
            }

            if (snapshotProperty != null)
            {
                sb.Append($"\t\t\te.OnSnapshot({snapshotProperty.Identifier});\n");
            }

            sb.Append("\t\t}\n");

            sb.Append("\t}\n");
        }

        private void GeneratePayloadCodeForUpdateChange(
            InterfaceDeclarationSyntax idecl, StringBuilder sb,
            PropertyDeclarationSyntax[] trackableProperties)
        {
            if (Options.UseProtobuf)
                sb.Append("\t[ProtoContract, TypeAlias]\n");

            sb.AppendFormat("\tpublic class UpdateChange : IUpdateChangePayload\n");
            sb.Append("\t{\n");

            // Members

            var protobufMemberIndex = 0;
            foreach (var p in trackableProperties)
            {
                sb.Append("\t\t");
                if (Options.UseProtobuf)
                {
                    protobufMemberIndex += 1;
                    sb.Append($"[ProtoMember({protobufMemberIndex})] ");
                }

                sb.Append($"public {Utility.GetTrackerClassName(p.Type)} {p.Identifier}Tracker;\n");
            }

            // Gather

            sb.AppendLine();
            sb.Append("\t\tpublic void Gather(IServerEntity entity)\n");
            sb.Append("\t\t{\n");
            sb.Append($"\t\t\tvar e = ({Utility.GetServerEntityBaseClassName(idecl)})entity;\n");

            foreach (var p in trackableProperties)
            {
                sb.Append($"\t\t\tif (e.{p.Identifier}.Changed)\n");
                sb.Append($"\t\t\t\t{p.Identifier}Tracker = ({Utility.GetTrackerClassName(p.Type)})e.{p.Identifier}.Tracker;\n");
            }

            sb.Append("\t\t}\n");

            // Notify

            sb.AppendLine();
            sb.Append("\t\tpublic void Notify(IClientEntity entity)\n");
            sb.Append("\t\t{\n");
            sb.Append($"\t\t\tvar e = ({Utility.GetClientEntityBaseClassName(idecl)})entity;\n");

            for (int i = 0; i < trackableProperties.Length; i++)
            {
                var p = trackableProperties[i];
                sb.Append($"\t\t\tif ({p.Identifier}Tracker != null)\n");
                sb.Append("\t\t\t{\n");
                sb.Append($"\t\t\t\te.OnTrackableDataChanging({i}, {p.Identifier}Tracker);\n");
                sb.Append($"\t\t\t\t{p.Identifier}Tracker.ApplyTo(e.{p.Identifier});\n");
                sb.Append($"\t\t\t\te.OnTrackableDataChanged({i}, {p.Identifier}Tracker);\n");
                sb.Append("\t\t\t}\n");
            }

            sb.Append("\t\t}\n");

            sb.Append("\t}\n");
        }

        private void GenerateServerEntityBaseCode(
            InterfaceDeclarationSyntax idecl, ICodeGenWriter writer,
            MethodDeclarationSyntax[] methods, Dictionary<MethodDeclarationSyntax, string> method2PayloadTypeNameMap,
            PropertyDeclarationSyntax snapshotProperty, PropertyDeclarationSyntax[] trackableProperties)
        {
            var payloadTableClassName = Utility.GetPayloadTableClassName(idecl);

            var sb = new StringBuilder();
            var interfaceName = "I" + idecl.Identifier.ToString().Substring(1) + "ServerHandler";
            var baseClassName = Utility.GetServerEntityBaseClassName(idecl);

            // Interface

            var interfaceMethods = methods.Where(
                m => m.AttributeLists.GetAttribute("PassThroughAttribute") == null &&
                     m.AttributeLists.GetAttribute("ToClientAttribute") == null).ToArray();

            sb.Append($"public interface {interfaceName} : IEntityServerHandler\n");
            sb.Append("{\n");
            foreach (var method in interfaceMethods)
            {
                sb.Append("\t" + method.ReturnType + " On" + method.Identifier + method.ParameterList + ";\n");
            }
            sb.Append("}\n");
            sb.AppendLine();

            // ServerEntityBase class

            sb.Append($"public abstract class {baseClassName} : {Options.ServerEntity ?? "ServerEntity"}\n");
            sb.Append("{");

            if (trackableProperties.Length > 0)
            {
                sb.AppendLine();
                foreach (var property in trackableProperties)
                {
                    sb.Append($"\tpublic Trackable{property.Type.ToString().Substring(1)} {property.Identifier} {{ get; set; }}\n");
                }
                sb.AppendLine();

                sb.Append($"\tprotected {baseClassName}()\n");
                sb.Append("\t{\n");
                foreach (var property in trackableProperties)
                {
                    sb.Append($"\t\t{property.Identifier} = new Trackable{property.Type.ToString().Substring(1)}();\n");
                }
                sb.Append("\t}\n");
            }

            if (snapshotProperty != null)
            {
                sb.AppendLine();
                sb.Append("\tpublic override object Snapshot { get { return OnSnapshot(); } }\n");
                sb.AppendLine();
                sb.Append($"\tpublic abstract {snapshotProperty.Type} OnSnapshot();\n");
            }

            sb.AppendLine();
            sb.Append($"\tpublic override int TrackableDataCount {{ get {{ return {trackableProperties.Length}; }} }}\n");
            sb.AppendLine();
            sb.Append("\tpublic override ITrackable GetTrackableData(int index)\n");
            sb.Append("\t{\n");
            for (int i=0; i<trackableProperties.Length; i++)
            {
                sb.Append($"\t\tif (index == {i}) return {trackableProperties[i].Identifier};\n");
            }
            sb.Append("\t\treturn null;\n");
            sb.Append("\t}\n");
            sb.AppendLine();
            sb.Append("\tpublic override void SetTrackableData(int index, ITrackable trackable)\n");
            sb.Append("\t{\n");
            for (int i = 0; i < trackableProperties.Length; i++)
            {
                sb.Append($"\t\tif (index == {i}) {trackableProperties[i].Identifier} = (Trackable{trackableProperties[i].Type.ToString().Substring(1)})trackable;\n");
            }
            sb.Append("\t}\n");

            if (trackableProperties.Length > 0 || snapshotProperty != null)
            {
                sb.AppendLine();
                sb.Append("\tpublic override ISpawnPayload GetSpawnPayload()\n");
                sb.Append("\t{\n");
                sb.Append($"\t\tvar payload = new {payloadTableClassName}.Spawn();\n");
                sb.Append($"\t\tpayload.Gather(this);\n");
                sb.Append($"\t\treturn payload;\n");
                sb.Append("\t}\n");
            }

            if (trackableProperties.Length > 0)
            {
                sb.AppendLine();
                sb.Append("\tpublic override IUpdateChangePayload GetUpdateChangePayload()\n");
                sb.Append("\t{\n");
                sb.Append($"\t\tvar payload = new {payloadTableClassName}.UpdateChange();\n");
                sb.Append($"\t\tpayload.Gather(this);\n");
                sb.Append($"\t\treturn payload;\n");
                sb.Append("\t}\n");
            }

            var classMethods = methods.Where(
                m => m.AttributeLists.GetAttribute("ToServerAttribute") == null).ToArray();

            foreach (var method in classMethods)
            {
                var messageName = method2PayloadTypeNameMap[method];
                var parameters = method.ParameterList.Parameters;

                var parameterTypeNames = string.Join(", ", parameters.Select(p => p.ToString()));
                var parameterInits = string.Join(", ", parameters.Select(p => p.Identifier + " = " + p.Identifier));

                sb.AppendLine();

                sb.AppendFormat("\tpublic void {0}({1})\n", method.Identifier, parameterTypeNames);

                sb.Append("\t{\n");

                sb.AppendFormat("\t\tvar payload = new {0}.{1} {{ {2} }};\n", payloadTableClassName, messageName, parameterInits);
                sb.AppendFormat("\t\tSendInvoke(payload);\n");

                sb.Append("\t}\n");
            }

            sb.Append("}");
            writer.AddCode(sb.ToString());
        }

        private void GenerateClientEntityBaseCode(
            InterfaceDeclarationSyntax idecl, ICodeGenWriter writer,
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

            sb.Append($"public interface {interfaceName} : IEntityClientHandler\n");
            sb.Append("{\n");
            foreach (var method in interfaceMethods)
            {
                sb.Append("\t" + method.ReturnType + " On" + method.Identifier + method.ParameterList + ";\n");
            }
            sb.Append("}\n");
            sb.AppendLine();

            // ClientEntityBase class

            sb.Append($"public abstract class {baseClassName} : {Options.ClientEntity ?? "ClientEntity"}\n");
            sb.Append("{");

            if (trackableProperties.Length > 0)
            {
                sb.AppendLine();
                foreach (var property in trackableProperties)
                {
                    sb.Append($"\tpublic Trackable{property.Type.ToString().Substring(1)} {property.Identifier} {{ get; set; }}\n");
                }
                sb.AppendLine();

                sb.Append($"\tprotected {baseClassName}()\n");
                sb.Append("\t{\n");
                foreach (var property in trackableProperties)
                {
                    sb.Append($"\t\t{property.Identifier} = new Trackable{property.Type.ToString().Substring(1)}();\n");
                }
                sb.Append("\t}\n");
            }

            if (snapshotProperty != null)
            {
                sb.AppendLine();
                sb.Append($"\tpublic override object Snapshot {{ set {{ OnSnapshot(({snapshotProperty.Type})value); }} }}\n");
                sb.AppendLine();
                sb.Append($"\tpublic abstract void OnSnapshot({snapshotProperty.Type} snapshot);\n");
            }

            sb.AppendLine();
            sb.Append($"\tpublic override int TrackableDataCount {{ get {{ return {trackableProperties.Length}; }} }}\n");
            sb.AppendLine();
            sb.Append("\tpublic override ITrackable GetTrackableData(int index)\n");
            sb.Append("\t{\n");
            for (int i = 0; i < trackableProperties.Length; i++)
            {
                sb.Append($"\t\tif (index == {i}) return {trackableProperties[i].Identifier};\n");
            }
            sb.Append("\t\treturn null;\n");
            sb.Append("\t}\n");
            sb.AppendLine();
            sb.Append("\tpublic override void SetTrackableData(int index, ITrackable trackable)\n");
            sb.Append("\t{\n");
            for (int i = 0; i < trackableProperties.Length; i++)
            {
                sb.Append($"\t\tif (index == {i}) {trackableProperties[i].Identifier} = (Trackable{trackableProperties[i].Type.ToString().Substring(1)})trackable;\n");
            }
            sb.Append("\t}\n");

            var classMethods = methods.Where(
                m => m.AttributeLists.GetAttribute("ToClientAttribute") == null).ToArray();

            foreach (var method in classMethods)
            {
                var messageName = method2PayloadTypeNameMap[method];
                var parameters = method.ParameterList.Parameters;

                var parameterTypeNames = string.Join(", ", parameters.Select(p => p.ToString()));
                var parameterInits = string.Join(", ", parameters.Select(p => p.Identifier + " = " + p.Identifier));

                sb.AppendLine();

                sb.AppendFormat("\tpublic void {0}({1})\n", method.Identifier, parameterTypeNames);

                sb.Append("\t{\n");

                sb.AppendFormat("\t\tvar payload = new {0}.{1} {{ {2} }};\n", payloadTableClassName, messageName, parameterInits);
                sb.AppendFormat("\t\tSendInvoke(payload);\n");

                sb.Append("\t}\n");
            }

            sb.Append("}");
            writer.AddCode(sb.ToString());
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

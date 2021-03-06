﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CommandLine;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using CodeWriter;

namespace CodeGen
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            // Parse command line options

            if (args.Length == 1 && args[0].StartsWith("@"))
            {
                var argFile = args[0].Substring(1);
                if (File.Exists(argFile))
                {
                    args = File.ReadAllLines(argFile);
                }
                else
                {
                    Console.WriteLine("File not found: " + argFile);
                    return 1;
                }
            }

            var parser = new Parser(config => config.HelpWriter = Console.Out);
            if (args.Length == 0)
            {
                parser.ParseArguments<Options>(new[] { "--help" });
                return 1;
            }

            Options options = null;
            var result = parser.ParseArguments<Options>(args)
                               .WithParsed(r => { options = r; });

            // Run process !

            if (options != null)
                return Process(options);
            else
                return 1;
        }

        private static int Process(Options options)
        {
            try
            {
                Console.WriteLine("Start Process!");

                // Resolve options

                var basePath = Path.GetFullPath(options.Path ?? ".");
                var sources =
                    options.Sources.Where(p => string.IsNullOrWhiteSpace(p) == false &&
                                               p.ToLower().IndexOf(".codegen.cs") == -1)
                           .Select(p => MakeFullPath(p, basePath))
                           .ToArray();
                var references =
                    options.References.Where(p => string.IsNullOrWhiteSpace(p) == false)
                           .Select(p => MakeFullPath(p, basePath))
                           .ToArray();
                var targetDefaultPath = @".\Properties\EntityNetwork.CodeGen.cs";
                var targetPath = MakeFullPath(options.TargetFile ?? targetDefaultPath, basePath);

                // Parse sources and extract interfaces

                Console.WriteLine("- Parse sources");

                var syntaxTrees = sources.Select(
                    file => CSharpSyntaxTree.ParseText(File.ReadAllText(file), path: file)).ToArray();
                var interfaceDeclarations = syntaxTrees.SelectMany(
                    st => st.GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>()).ToArray();

                // Generate code

                Console.WriteLine("- Generate code");

                var settings = new CodeWriterSettings(CodeWriterSettings.CSharpDefault);
                settings.TranslationMapping["`"] = "\"";

                var w = new CodeWriter.CodeWriter(settings);
                w.HeadLines = new List<string>()
                {
                    "// ------------------------------------------------------------------------------",
                    "// <auto-generated>",
                    "//     This code was generated by EntityNetwork CodeGenerator.",
                    "//",
                    "//     Changes to this file may cause incorrect behavior and will be lost if",
                    "//     the code is regenerated.",
                    "// </auto-generated>",
                    "// ------------------------------------------------------------------------------",
                    "",
                    "using System;",
                    "using System.Collections.Generic;",
                    "using System.Reflection;",
                    "using System.Runtime.Serialization;",
                    "using System.Linq;",
                    "using System.Text;",
                    "using EntityNetwork;",
                    "using TrackableData;",
                };

                if (options.UseProtobuf)
                {
                    w.HeadLines.Add("using ProtoBuf;");
                    w.HeadLines.Add("using TypeAlias;");
                    w.HeadLines.Add("using System.ComponentModel;");
                }

                w.HeadLines.Add("");

                // Entity

                var relatedSourceTrees = new HashSet<SyntaxNode>();

                var pocoCodeGen = new EntityCodeGenerator() { Options = options };
                foreach (var idecl in interfaceDeclarations)
                {
                    if (idecl.HasBase("EntityNetwork.IEntityPrototype"))
                    {
                        pocoCodeGen.GenerateCode(idecl, w);
                        relatedSourceTrees.Add(idecl.GetRootNode());
                    }
                }

                // Resolve referenced using

                var usingDirectives = new HashSet<string>(relatedSourceTrees.SelectMany(
                    st => st.DescendantNodes().OfType<UsingDirectiveSyntax>()).Select(x => x.Name.ToString()));

                foreach (var usingDirective in usingDirectives)
                    EnsureUsing(w, usingDirective);

                // Save generated code

                Console.WriteLine("- Save code");

                if (w.WriteAllText(targetPath, true) == false)
                    Console.WriteLine("Nothing changed. Skip writing.");

                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in processing:\n" + e);
                return 1;
            }
        }

        private static IEnumerable<Type> GetTypesSafely(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                Console.WriteLine(ex);
                foreach (var e in ex.LoaderExceptions)
                    Console.WriteLine(e);
                return ex.Types.Where(x => x != null);
            }
        }

        private static string MakeFullPath(string path, string basePath)
        {
            if (Path.IsPathRooted(path))
                return path;
            else
                return Path.Combine(basePath, path);
        }

        private static void EnsureUsing(CodeWriter.CodeWriter w, string ns)
        {
            var line = $"using {ns};";
            if (w.HeadLines.Contains(line))
                return;

            var idx = w.HeadLines.FindLastIndex(l => l.StartsWith("using "));
            w.HeadLines.Insert(idx + 1, line);
        }
    }
}

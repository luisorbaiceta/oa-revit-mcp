using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Autodesk.Revit.UI;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace RevitMCPCommandSet.Services
{
    public class CodeExecutor
    {
        public object Execute(UIApplication app, string userCode)
        {
            if (string.IsNullOrWhiteSpace(userCode))
            {
                throw new InvalidOperationException("No code to execute.");
            }

            var doc = app.ActiveUIDocument.Document;
            string fullCode = GenerateFullCode(userCode);
            var compilation = CreateCompilation(fullCode);

            using (var ms = new MemoryStream())
            {
                EmitResult result = compilation.Emit(ms);

                if (!result.Success)
                {
                    var failures = result.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error);

                    StringBuilder errors = new StringBuilder();
                    errors.AppendLine("Compilation failed:");
                    foreach (var diagnostic in failures)
                    {
                        errors.AppendLine($"{diagnostic.Id}: {diagnostic.GetMessage()} at {diagnostic.Location}");
                    }
                    throw new Exception(errors.ToString());
                }

                ms.Seek(0, SeekOrigin.Begin);
                Assembly assembly = Assembly.Load(ms.ToArray());
                Type type = assembly.GetType("RevitMCP.DynamicCode.Executor");
                object instance = Activator.CreateInstance(type);

                MethodInfo method = type.GetMethod("Run");
                return method.Invoke(instance, new object[] { app, doc });
            }
        }

        private string GenerateFullCode(string userCode)
        {
            return $@"
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Structure;

namespace RevitMCP.DynamicCode
{{
    public class Executor
    {{
        public object Run(UIApplication app, Document doc)
        {{
            // User code starts here
            {userCode}
            // User code ends here
            return null; // Or return a result
        }}
    }}
}}";
        }

        private CSharpCompilation CreateCompilation(string code)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(code);

            // Get the directory of the currently executing assembly to find RevitAPI.dll
            string assemblyLocation = Path.GetDirectoryName(typeof(CodeExecutor).Assembly.Location);

            // Get the path to the .NET framework directory
            string dotNetPath = Path.GetDirectoryName(typeof(object).Assembly.Location);

            var references = new List<MetadataReference>
            {
                // Add specific .NET assemblies
                MetadataReference.CreateFromFile(Path.Combine(dotNetPath, "mscorlib.dll")),
                MetadataReference.CreateFromFile(Path.Combine(dotNetPath, "System.dll")),
                MetadataReference.CreateFromFile(Path.Combine(dotNetPath, "System.Core.dll")),
                MetadataReference.CreateFromFile(Path.Combine(dotNetPath, "System.Runtime.dll")),

                // Add Revit API assemblies
                MetadataReference.CreateFromFile(Path.Combine(assemblyLocation, "RevitAPI.dll")),
                MetadataReference.CreateFromFile(Path.Combine(assemblyLocation, "RevitAPIUI.dll"))
            };

            return CSharpCompilation.Create(
                "RevitMCP.DynamicAssembly",
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );
        }
    }
}

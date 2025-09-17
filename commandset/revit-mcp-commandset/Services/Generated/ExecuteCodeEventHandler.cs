using System.CodeDom.Compiler;
using System.Reflection;
using Autodesk.Revit.Exceptions;
using Autodesk.Revit.UI;
using Microsoft.CSharp;
using Newtonsoft.Json;
using RevitMCPSDK.API.Interfaces;

using System.CodeDom.Compiler;
using System.Reflection;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.CSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Interfaces;
using RevitMCPCommandSet.Models.Common;

namespace RevitMCPCommandSet.Services.Generated
{
    public class ExecuteCodeEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        public object Result { get; private set; }
        public bool TaskCompleted { get; private set; }
        private readonly System.Threading.ManualResetEvent _resetEvent = new System.Threading.ManualResetEvent(false);

        private string _generatedCode;
        private object[] _executionParameters;

        public void SetParameters(JObject parameters)
        {
            _generatedCode = parameters["code"]?.Value<string>();
            _executionParameters = parameters["params"]?.ToObject<object[]>() ?? Array.Empty<object>();
        }

        public void Execute(UIApplication app)
        {
            try
            {
                var doc = app.ActiveUIDocument.Document;
                using (var transaction = new Transaction(doc, "Execute AI Code"))
                {
                    transaction.Start();
                    var result = CompileAndExecuteCode(doc, _generatedCode, _executionParameters);
                    transaction.Commit();
                    Result = new ExecutionResultInfo { Success = true, Result = JsonConvert.SerializeObject(result) };
                }
            }
            catch (Exception ex)
            {
                Result = new ExecutionResultInfo { Success = false, ErrorMessage = $"Execution failed: {ex.Message}" };
            }
            finally
            {
                TaskCompleted = true;
                _resetEvent.Set();
            }
        }

        private object CompileAndExecuteCode(Document doc, string code, object[] parameters)
        {
            var compilerParams = new CompilerParameters
            {
                GenerateInMemory = true,
                GenerateExecutable = false,
                ReferencedAssemblies =
                {
                    "System.dll",
                    "System.Core.dll",
                    typeof(Document).Assembly.Location,
                    typeof(UIApplication).Assembly.Location
                }
            };

            var wrappedCode = $@"
using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.Generic;
namespace AIGeneratedCode
{{
    public static class CodeExecutor
    {{
        public static object Execute(Document document, object[] parameters)
        {{
            {code}
        }}
    }}
}}";

            using (var provider = new CSharpCodeProvider())
            {
                var compileResults = provider.CompileAssemblyFromSource(compilerParams, wrappedCode);
                if (compileResults.Errors.HasErrors)
                {
                    var errors = string.Join("\n", compileResults.Errors.Cast<CompilerError>().Select(e => $"Line {e.Line}: {e.ErrorText}"));
                    throw new Exception($"Code compilation error:\n{errors}");
                }

                var assembly = compileResults.CompiledAssembly;
                var executorType = assembly.GetType("AIGeneratedCode.CodeExecutor");
                var executeMethod = executorType.GetMethod("Execute");

                try
                {
                    return executeMethod.Invoke(null, new object[] { doc, parameters });
                }
                catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException)
                {
                    throw new InvalidOperationException("UI-related operations (e.g., TaskDialog.Show) failed. This usually happens when performing UI operations in a non-UI thread. Please modify your code to remove UI-related calls.", ex.InnerException);
                }
            }
        }

        public string GetName()
        {
            return "send_code_to_revit";
        }

        public bool WaitForCompletion(int timeoutMilliseconds = 15000)
        {
            return _resetEvent.WaitOne(timeoutMilliseconds);
        }
    }
}

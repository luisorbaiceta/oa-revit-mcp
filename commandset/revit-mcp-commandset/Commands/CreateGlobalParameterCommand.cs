using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Services;
using System;
using Autodesk.Revit.DB;

namespace RevitMCPCommandSet.Commands
{
    public class CreateGlobalParameterCommand : ExternalEventCommandBase
    {
        public override string CommandName => "create_global_parameter";

        public CreateGlobalParameterCommand(UIApplication uiApp)
            : base(new CreateGlobalParameterEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                var name = parameters["name"].ToObject<string>();
                var type = (ParameterType)Enum.Parse(typeof(ParameterType), parameters["type"].ToObject<string>());
                var spec = new ForgeTypeId(parameters["spec"].ToObject<string>());
                var isReporting = parameters["isReporting"].ToObject<bool>();

                var handler = (CreateGlobalParameterEventHandler)Handler;
                handler.Name = name;
                handler.Type = type;
                handler.Spec = spec;
                handler.IsReporting = isReporting;


                if (RaiseAndWaitForCompletion(10000))
                {
                    return handler.Result;
                }
                else
                {
                    throw new TimeoutException("Timeout creating global parameter.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create global parameter: {ex.Message}");
            }
        }
    }
}

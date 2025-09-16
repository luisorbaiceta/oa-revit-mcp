using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Services;
using System;
using Autodesk.Revit.DB;

namespace RevitMCPCommandSet.Commands
{
    public class AssignGlobalParameterToElementCommand : ExternalEventCommandBase
    {
        public override string CommandName => "assign_global_parameter_to_element";

        public AssignGlobalParameterToElementCommand(UIApplication uiApp)
            : base(new AssignGlobalParameterToElementEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                var elementId = new ElementId(parameters["elementId"].ToObject<int>());
                var parameterName = parameters["parameterName"].ToObject<string>();
                var globalParameterId = new ElementId(parameters["globalParameterId"].ToObject<int>());

                var handler = (AssignGlobalParameterToElementEventHandler)Handler;
                handler.ElementId = elementId;
                handler.ParameterName = parameterName;
                handler.GlobalParameterId = globalParameterId;

                if (RaiseAndWaitForCompletion(10000))
                {
                    return handler.Result;
                }
                else
                {
                    throw new TimeoutException("Timeout assigning global parameter to element.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to assign global parameter to element: {ex.Message}");
            }
        }
    }
}

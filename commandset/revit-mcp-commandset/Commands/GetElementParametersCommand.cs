using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Services;
using System;

namespace RevitMCPCommandSet.Commands
{
    public class GetElementParametersCommand : ExternalEventCommandBase
    {
        public override string CommandName => "get_element_parameters";

        public GetElementParametersCommand(UIApplication uiApp)
            : base(new GetElementParametersEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                var elementId = parameters["elementId"].ToObject<int>();
                var handler = (GetElementParametersEventHandler)Handler;
                handler.ElementId = elementId;

                if (RaiseAndWaitForCompletion(10000))
                {
                    return handler.Result;
                }
                else
                {
                    throw new TimeoutException("Timeout getting element parameters.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get element parameters: {ex.Message}");
            }
        }
    }
}

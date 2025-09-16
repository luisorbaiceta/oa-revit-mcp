using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Services;
using System;

namespace RevitMCPCommandSet.Commands
{
    public class GetProjectParametersCommand : ExternalEventCommandBase
    {
        public override string CommandName => "get_project_parameters";

        public GetProjectParametersCommand(UIApplication uiApp)
            : base(new GetProjectParametersEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                var handler = (GetProjectParametersEventHandler)Handler;

                if (RaiseAndWaitForCompletion(10000))
                {
                    return handler.Result;
                }
                else
                {
                    throw new TimeoutException("Timeout getting project parameters.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get project parameters: {ex.Message}");
            }
        }
    }
}

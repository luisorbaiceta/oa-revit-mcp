using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Services;
using System;

namespace RevitMCPCommandSet.Commands
{
    public class GetSharedParametersCommand : ExternalEventCommandBase
    {
        public override string CommandName => "get_shared_parameters";

        public GetSharedParametersCommand(UIApplication uiApp)
            : base(new GetSharedParametersEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                var handler = (GetSharedParametersEventHandler)Handler;

                if (RaiseAndWaitForCompletion(10000))
                {
                    return handler.Result;
                }
                else
                {
                    throw new TimeoutException("Timeout getting shared parameters.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get shared parameters: {ex.Message}");
            }
        }
    }
}

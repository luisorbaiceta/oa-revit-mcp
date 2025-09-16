using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Services;
using System;

namespace RevitMCPCommandSet.Commands
{
    public class GetAllGlobalParametersCommand : ExternalEventCommandBase
    {
        public override string CommandName => "get_all_global_parameters";

        public GetAllGlobalParametersCommand(UIApplication uiApp)
            : base(new GetAllGlobalParametersEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                var handler = (GetAllGlobalParametersEventHandler)Handler;

                if (RaiseAndWaitForCompletion(10000))
                {
                    return handler.Result;
                }
                else
                {
                    throw new TimeoutException("Timeout getting all global parameters.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get all global parameters: {ex.Message}");
            }
        }
    }
}

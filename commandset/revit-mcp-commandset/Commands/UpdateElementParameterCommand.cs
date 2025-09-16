using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Services;
using System;

namespace RevitMCPCommandSet.Commands
{
    public class UpdateElementParameterCommand : ExternalEventCommandBase
    {
        public override string CommandName => "update_element_parameter";

        public UpdateElementParameterCommand(UIApplication uiApp)
            : base(new UpdateElementParameterEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                var updateInfo = parameters.ToObject<ParameterUpdateInfo>();
                var handler = (UpdateElementParameterEventHandler)Handler;
                handler.UpdateInfo = updateInfo;

                if (RaiseAndWaitForCompletion(10000))
                {
                    return handler.Result;
                }
                else
                {
                    throw new TimeoutException("Timeout updating element parameter.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to update element parameter: {ex.Message}");
            }
        }
    }
}

using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Services;
using System;

namespace RevitMCPCommandSet.Commands
{
    public class ModifyElementCommand : ExternalEventCommandBase
    {
        private ModifyElementEventHandler _handler => (ModifyElementEventHandler)Handler;
        public override string CommandName => "modify_element";
        public ModifyElementCommand(UIApplication uiApp)
            : base(new ModifyElementEventHandler(), uiApp)
        {
        }
        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                var data = parameters["data"]?.ToObject<ModifyElementSetting>();
                if (data == null)
                    throw new ArgumentNullException(nameof(data), "AI传入数据为空");

                _handler.SetParameters(data);

                if (RaiseAndWaitForCompletion(15000))
                {
                    return _handler.Result;
                }
                else
                {
                    throw new TimeoutException("Modify element timeout");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Modify element failed: {ex.Message}");
            }
        }
    }
}

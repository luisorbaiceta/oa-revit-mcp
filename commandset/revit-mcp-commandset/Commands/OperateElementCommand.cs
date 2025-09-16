using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Services;
using System;

namespace RevitMCPCommandSet.Commands
{
    public class OperateElementCommand : ExternalEventCommandBase
    {
        private OperateElementEventHandler _handler => (OperateElementEventHandler)Handler;
        public override string CommandName => "operate_element";
        public OperateElementCommand(UIApplication uiApp)
            : base(new OperateElementEventHandler(), uiApp)
        {
        }
        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                var data = parameters["data"]?.ToObject<OperationSetting>();
                if (data == null)
                    throw new ArgumentNullException(nameof(data), "AI传入数据为空");

                _handler.SetParameters(data);

                if (RaiseAndWaitForCompletion(15000))
                {
                    return _handler.Result;
                }
                else
                {
                    throw new TimeoutException("Operate element timeout");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Operate element failed: {ex.Message}");
            }
        }
    }
}

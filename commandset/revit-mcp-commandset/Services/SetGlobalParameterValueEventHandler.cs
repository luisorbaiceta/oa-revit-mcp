using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Utils;
using System;
using System.Threading;

namespace RevitMCPCommandSet.Services
{
    public class SetGlobalParameterValueEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);
        public ElementId Id { get; set; }
        public object Value { get; set; }
        public AIResult<string> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            try
            {
                var doc = uiapp.ActiveUIDocument.Document;
                using (var trans = new Transaction(doc, "Set Global Parameter Value"))
                {
                    trans.Start();
                    ParameterUtils.SetGlobalParameterValue(doc, Id, Value);
                    trans.Commit();
                }
                Result = new AIResult<string> { Success = true, Message = "Global parameter value updated successfully." };
            }
            catch (Exception ex)
            {
                Result = new AIResult<string> { Success = false, Message = ex.Message };
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        public string GetName() => "Set Global Parameter Value";

        public bool WaitForCompletion(int timeoutMilliseconds = 10000)
        {
            return _resetEvent.WaitOne(timeoutMilliseconds);
        }
    }
}

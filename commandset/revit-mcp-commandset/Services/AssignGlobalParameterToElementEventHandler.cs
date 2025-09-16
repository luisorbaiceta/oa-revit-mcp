using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Utils;
using System;
using System.Threading;
using ParameterUtils = RevitMCPCommandSet.Utils.ParameterUtils;

namespace RevitMCPCommandSet.Services
{
    public class AssignGlobalParameterToElementEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);
        public ElementId ElementId { get; set; }
        public string ParameterName { get; set; }
        public ElementId GlobalParameterId { get; set; }
        public AIResult<string> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            try
            {
                var doc = uiapp.ActiveUIDocument.Document;
                using (var trans = new Transaction(doc, "Assign Global Parameter"))
                {
                    trans.Start();
                    ParameterUtils.AssignGlobalParameterToElement(doc, ElementId, ParameterName, GlobalParameterId);
                    trans.Commit();
                }
                Result = new AIResult<string> { Success = true, Message = "Global parameter assigned successfully." };
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

        public string GetName() => "Assign Global Parameter to Element";

        public bool WaitForCompletion(int timeoutMilliseconds = 10000)
        {
            return _resetEvent.WaitOne(timeoutMilliseconds);
        }
    }
}

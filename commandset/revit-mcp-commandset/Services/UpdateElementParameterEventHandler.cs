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
    public class UpdateElementParameterEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);
        public ParameterUpdateInfo UpdateInfo { get; set; }
        public AIResult<string> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            try
            {
                var doc = uiapp.ActiveUIDocument.Document;
                using (var trans = new Transaction(doc, "Update Parameter"))
                {
                    trans.Start();
                    var element = doc.GetElement(new ElementId(UpdateInfo.ElementId));
                    if (element == null)
                    {
                        throw new Exception("Element not found.");
                    }

                    ParameterUtils.SetParameterValue(element, UpdateInfo.ParameterName, UpdateInfo.ParameterValue);
                    trans.Commit();
                }
                Result = new AIResult<string> { Success = true, Message = "Parameter updated successfully." };
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

        public string GetName() => "Update Element Parameter";

        public bool WaitForCompletion(int timeoutMilliseconds = 10000)
        {
            return _resetEvent.WaitOne(timeoutMilliseconds);
        }
    }
}

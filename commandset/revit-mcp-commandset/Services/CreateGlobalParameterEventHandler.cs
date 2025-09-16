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
    public class CreateGlobalParameterEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);
        public string Name { get; set; }
        public ForgeTypeId Spec { get; set; }
        public bool IsReporting { get; set; }
        public AIResult<int> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            try
            {
                var doc = uiapp.ActiveUIDocument.Document;
                using (var trans = new Transaction(doc, "Create Global Parameter"))
                {
                    trans.Start();
                    var id = ParameterUtils.CreateGlobalParameter(doc, Name, Spec, IsReporting);
                    trans.Commit();
                    Result = new AIResult<int> { Success = true, Response = id.IntegerValue };
                }
            }
            catch (Exception ex)
            {
                Result = new AIResult<int> { Success = false, Message = ex.Message };
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        public string GetName() => "Create Global Parameter";

        public bool WaitForCompletion(int timeoutMilliseconds = 10000)
        {
            return _resetEvent.WaitOne(timeoutMilliseconds);
        }
    }
}

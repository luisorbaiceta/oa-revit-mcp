using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Utils;
using System;
using System.Collections.Generic;
using System.Threading;
using ParameterUtils = RevitMCPCommandSet.Utils.ParameterUtils;

namespace RevitMCPCommandSet.Services
{
    public class GetProjectParametersEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);
        public AIResult<List<RevitMCPCommandSet.Models.Common.ParameterInfo>> Result { get; private set; }

        public void Execute(UIApplication uiapp)
        {
            try
            {
                var doc = uiapp.ActiveUIDocument.Document;
                var parameters = ParameterUtils.GetAllProjectParameters(doc);
                Result = new AIResult<List<RevitMCPCommandSet.Models.Common.ParameterInfo>> { Success = true, Response = parameters };
            }
            catch (Exception ex)
            {
                Result = new AIResult<List<RevitMCPCommandSet.Models.Common.ParameterInfo>> { Success = false, Message = ex.Message };
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        public string GetName() => "Get Project Parameters";

        public bool WaitForCompletion(int timeoutMilliseconds = 10000)
        {
            return _resetEvent.WaitOne(timeoutMilliseconds);
        }
    }
}

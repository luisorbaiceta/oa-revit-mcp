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
    public class GetElementParametersEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);
        public int ElementId { get; set; }
        public AIResult<List<Models.Common.ParameterInfo>> Result { get; private set; } // Updated to use the correct namespace

        public void Execute(UIApplication uiapp)
        {
            try
            {
                var doc = uiapp.ActiveUIDocument.Document;
                var element = doc.GetElement(new ElementId(ElementId));
                if (element == null)
                {
                    throw new Exception("Element not found.");
                }

                var parameters = ParameterUtils.GetAllParameters(element);
                Result = new AIResult<List<Models.Common.ParameterInfo>> { Success = true, Response = parameters }; // Updated to use the correct namespace
            }
            catch (Exception ex)
            {
                Result = new AIResult<List<Models.Common.ParameterInfo>> { Success = false, Message = ex.Message }; // Updated to use the correct namespace
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        public string GetName() => "Get Element Parameters";

        public bool WaitForCompletion(int timeoutMilliseconds = 10000)
        {
            return _resetEvent.WaitOne(timeoutMilliseconds);
        }
    }
}

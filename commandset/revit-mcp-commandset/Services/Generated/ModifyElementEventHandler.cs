using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using RevitMCPCommandSet.Models.Common;
using System;
using System.Threading;
using System.Collections.Generic;

using Newtonsoft.Json.Linq;

namespace RevitMCPCommandSet.Services.Generated
{
    public class ModifyElementEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        public object Result { get; private set; }
        public bool TaskCompleted { get; private set; }
        private readonly System.Threading.ManualResetEvent _resetEvent = new System.Threading.ManualResetEvent(false);

        private ModifyElementSetting _modifyData;

        public void SetParameters(JObject parameters)
        {
            _modifyData = parameters.ToObject<ModifyElementSetting>();
        }

        public void Execute(UIApplication uiapp)
        {
            try
            {
                var doc = uiapp.ActiveUIDocument.Document;
                using (Transaction trans = new Transaction(doc, "Modify Element"))
                {
                    trans.Start();
                    Element element = doc.GetElement(new ElementId(_modifyData.ElementId));
                    if (element == null)
                    {
                        throw new Exception("Element not found.");
                    }

                    foreach (KeyValuePair<string, string> entry in _modifyData.Parameters)
                    {
                        Parameter param = element.LookupParameter(entry.Key);
                        if (param != null && !param.IsReadOnly)
                        {
                            param.Set(entry.Value);
                        }
                    }
                    trans.Commit();
                }

                Result = new AIResult<string>
                {
                    Success = true,
                    Message = "Successfully modified element"
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<string>
                {
                    Success = false,
                    Message = $"Error modifying element: {ex.Message}"
                };
            }
            finally
            {
                TaskCompleted = true;
                _resetEvent.Set();
            }
        }

        public bool WaitForCompletion(int timeoutMilliseconds = 15000)
        {
            return _resetEvent.WaitOne(timeoutMilliseconds);
        }

        public string GetName()
        {
            return "modify_element";
        }
    }
}

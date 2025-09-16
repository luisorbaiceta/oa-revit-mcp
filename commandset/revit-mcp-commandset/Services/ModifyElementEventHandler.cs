using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using RevitMCPCommandSet.Models.Common;
using System;
using System.Threading;
using System.Collections.Generic;

namespace RevitMCPCommandSet.Services
{
    public class ModifyElementEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private UIDocument uiDoc => uiApp.ActiveUIDocument;
        private Document doc => uiDoc.Document;
        private Autodesk.Revit.ApplicationServices.Application app => uiApp.Application;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);
        public ModifyElementSetting ModifyData { get; private set; }
        public AIResult<string> Result { get; private set; }

        public void SetParameters(ModifyElementSetting data)
        {
            ModifyData = data;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;

            try
            {
                using (Transaction trans = new Transaction(doc, "Modify Element"))
                {
                    trans.Start();
                    Element element = doc.GetElement(new ElementId(ModifyData.ElementId));
                    if (element == null)
                    {
                        throw new Exception("Element not found.");
                    }

                    foreach (KeyValuePair<string, string> entry in ModifyData.Parameters)
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
                _resetEvent.Set();
            }
        }

        public bool WaitForCompletion(int timeoutMilliseconds = 10000)
        {
            return _resetEvent.WaitOne(timeoutMilliseconds);
        }

        public string GetName()
        {
            return "Modify Element";
        }
    }
}

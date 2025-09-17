using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Interfaces;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitMCPCommandSet.Services.Generated
{
    public class AIElementFilterEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        public object Result { get; private set; }
        public bool TaskCompleted { get; private set; }
        private readonly System.Threading.ManualResetEvent _resetEvent = new System.Threading.ManualResetEvent(false);

        private FilterSetting _filterSetting;

        public void SetParameters(JObject parameters)
        {
            _filterSetting = parameters.ToObject<FilterSetting>();
        }

        public void Execute(UIApplication uiapp)
        {
            try
            {
                var doc = uiapp.ActiveUIDocument.Document;
                var elementInfoList = new List<object>();
                if (!_filterSetting.Validate(out string errorMessage))
                    throw new Exception(errorMessage);

                var elementList = FilterUtils.GetFilteredElements(doc, _filterSetting);
                if (elementList == null || !elementList.Any())
                    throw new Exception("No elements found matching the filter criteria.");

                string message = "";
                if (_filterSetting.MaxElements > 0 && elementList.Count > _filterSetting.MaxElements)
                {
                    elementList = elementList.Take(_filterSetting.MaxElements).ToList();
                    message = $" and only the first {_filterSetting.MaxElements} are displayed.";
                }

                elementInfoList = FilterUtils.GetElementFullInfo(doc, elementList);

                Result = new AIResult<List<object>>
                {
                    Success = true,
                    Message = $"Successfully retrieved {elementInfoList.Count} elements." + message,
                    Response = elementInfoList,
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<List<object>>
                {
                    Success = false,
                    Message = $"Error retrieving element information: {ex.Message}",
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
            return "ai_element_filter";
        }
    }
}

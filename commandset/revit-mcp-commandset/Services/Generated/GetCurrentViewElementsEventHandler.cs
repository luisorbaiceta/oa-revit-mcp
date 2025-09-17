using Autodesk.Revit.DB;
﻿using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Models.Common;
using RevitMCPSDK.API.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitMCPCommandSet.Services.Generated
{
    public class GetCurrentViewElementsEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        public object Result { get; private set; }
        public bool TaskCompleted { get; private set; }
        private readonly System.Threading.ManualResetEvent _resetEvent = new System.Threading.ManualResetEvent(false);

        private List<string> _modelCategoryList;
        private List<string> _annotationCategoryList;
        private bool _includeHidden;
        private int _limit;

        public void SetParameters(JObject parameters)
        {
            _modelCategoryList = parameters["modelCategoryList"]?.ToObject<List<string>>();
            _annotationCategoryList = parameters["annotationCategoryList"]?.ToObject<List<string>>();
            _includeHidden = parameters["includeHidden"]?.Value<bool>() ?? false;
            _limit = parameters["limit"]?.Value<int>() ?? 100;
        }

        public void Execute(UIApplication app)
        {
            try
            {
                var uiDoc = app.ActiveUIDocument;
                var doc = uiDoc.Document;
                var activeView = doc.ActiveView;

                List<string> modelCategories = _modelCategoryList ?? new List<string>();
                List<string> annotationCategories = _annotationCategoryList ?? new List<string>();

                List<string> allCategories = new List<string>();
                allCategories.AddRange(modelCategories);
                allCategories.AddRange(annotationCategories);

                var collector = new FilteredElementCollector(doc, activeView.Id)
                    .WhereElementIsNotElementType();

                IList<Element> elements = collector.ToElements();

                if (allCategories.Count > 0)
                {
                    List<BuiltInCategory> builtInCategories = new List<BuiltInCategory>();
                    foreach (string categoryName in allCategories)
                    {
                        if (Enum.TryParse(categoryName, out BuiltInCategory category))
                        {
                            builtInCategories.Add(category);
                        }
                    }
                    if (builtInCategories.Count > 0)
                    {
                        ElementMulticategoryFilter categoryFilter = new ElementMulticategoryFilter(builtInCategories);
                        elements = new FilteredElementCollector(doc, activeView.Id)
                            .WhereElementIsNotElementType()
                            .WherePasses(categoryFilter)
                            .ToElements();
                    }
                }

                if (!_includeHidden)
                {
                    elements = elements.Where(e => !e.IsHidden(activeView)).ToList();
                }

                if (_limit > 0 && elements.Count > _limit)
                {
                    elements = elements.Take(_limit).ToList();
                }

                var elementInfos = elements.Select(e => new ElementInfo
                {
                    Id = e.Id.Value,
                    UniqueId = e.UniqueId,
                    Name = e.Name,
                    Category = e.Category?.Name ?? "unknown",
                    Properties = GetElementProperties(e)
                }).ToList();

                Result = new ViewElementsResult
                {
                    ViewId = activeView.Id.Value,
                    ViewName = activeView.Name,
                    TotalElementsInView = new FilteredElementCollector(doc, activeView.Id).GetElementCount(),
                    FilteredElementCount = elementInfos.Count,
                    Elements = elementInfos
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
            finally
            {
                TaskCompleted = true;
                _resetEvent.Set();
            }
        }

        private Dictionary<string, string> GetElementProperties(Element element)
        {
            var properties = new Dictionary<string, string>();
            properties.Add("ElementId", element.Id.Value.ToString());

            if (element.Location is LocationPoint locationPoint)
            {
                var point = locationPoint.Point;
                properties.Add("LocationX", point.X.ToString("F2"));
                properties.Add("LocationY", point.Y.ToString("F2"));
                properties.Add("LocationZ", point.Z.ToString("F2"));
            }
            else if (element.Location is LocationCurve locationCurve)
            {
                var curve = locationCurve.Curve;
                properties.Add("Start", $"{curve.GetEndPoint(0).X:F2}, {curve.GetEndPoint(0).Y:F2}, {curve.GetEndPoint(0).Z:F2}");
                properties.Add("End", $"{curve.GetEndPoint(1).X:F2}, {curve.GetEndPoint(1).Y:F2}, {curve.GetEndPoint(1).Z:F2}");
                properties.Add("Length", curve.Length.ToString("F2"));
            }

            var commonParams = new[] { "Comments", "Mark", "Level", "Family", "Type" };
            foreach (var paramName in commonParams)
            {
                Parameter param = element.LookupParameter(paramName);
                if (param != null && !param.IsReadOnly)
                {
                    if (param.StorageType == StorageType.String)
                        properties.Add(paramName, param.AsString() ?? "");
                    else if (param.StorageType == StorageType.Double)
                        properties.Add(paramName, param.AsDouble().ToString("F2"));
                    else if (param.StorageType == StorageType.Integer)
                        properties.Add(paramName, param.AsInteger().ToString());
                    else if (param.StorageType == StorageType.ElementId)
                        properties.Add(paramName, param.AsElementId().Value.ToString());
                }
            }

            return properties;
        }

        public string GetName()
        {
            return "get_current_view_elements";
        }

        public bool WaitForCompletion(int timeoutMilliseconds = 15000)
        {
            return _resetEvent.WaitOne(timeoutMilliseconds);
        }
    }
}

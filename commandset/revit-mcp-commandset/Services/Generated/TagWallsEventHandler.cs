using Autodesk.Revit.DB;
﻿using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitMCPCommandSet.Services.Generated
{
    public class TagWallsEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        public object Result { get; private set; }
        public bool TaskCompleted { get; private set; }
        private readonly System.Threading.ManualResetEvent _resetEvent = new System.Threading.ManualResetEvent(false);

        private bool _useLeader;
        private string _tagTypeId;

        public void SetParameters(JObject parameters)
        {
            _useLeader = parameters["useLeader"]?.Value<bool>() ?? true;
            _tagTypeId = parameters["tagTypeId"]?.Value<string>();
        }

        public void Execute(UIApplication uiapp)
        {
            try
            {
                var doc = uiapp.ActiveUIDocument.Document;
                View activeView = doc.ActiveView;

                FilteredElementCollector wallCollector = new FilteredElementCollector(doc, activeView.Id);
                ICollection<Element> walls = wallCollector.OfCategory(BuiltInCategory.OST_Walls)
                                                         .WhereElementIsNotElementType()
                                                         .ToElements();

                List<object> createdTags = new List<object>();
                List<string> errors = new List<string>();

                using (Transaction tran = new Transaction(doc, "Tag Walls"))
                {
                    tran.Start();

                    FamilySymbol wallTagType = FindWallTagType(doc);

                    if (wallTagType == null)
                    {
                        Result = new { success = false, message = "Wall tag family type not found." };
                        tran.RollBack();
                        return;
                    }

                    if (!wallTagType.IsActive)
                    {
                        wallTagType.Activate();
                        doc.Regenerate();
                    }

                    foreach (Element wall in walls)
                    {
                        try
                        {
                            if (wall.Location is LocationCurve locationCurve)
                            {
                                Curve curve = locationCurve.Curve;
                                XYZ midpoint = curve.Evaluate(0.5, true);

                                IndependentTag tag = IndependentTag.Create(
                                    doc,
                                    wallTagType.Id,
                                    activeView.Id,
                                    new Reference(wall),
                                    _useLeader,
                                    TagOrientation.Horizontal,
                                    midpoint);

                                if (tag != null)
                                {
                                    createdTags.Add(new
                                    {
                                        id = tag.Id.Value.ToString(),
                                        wallId = wall.Id.Value.ToString(),
                                        wallName = wall.Name,
                                        location = new { x = midpoint.X, y = midpoint.Y, z = midpoint.Z }
                                    });
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"Error tagging wall {wall.Id.Value}: {ex.Message}");
                        }
                    }

                    tran.Commit();

                    Result = new
                    {
                        success = true,
                        totalWalls = walls.Count,
                        taggedWalls = createdTags.Count,
                        tags = createdTags,
                        errors = errors.Count > 0 ? errors : null
                    };
                }
            }
            catch (Exception ex)
            {
                Result = new { success = false, message = $"An error occurred: {ex.Message}" };
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
            return "tag_all_walls";
        }

        private FamilySymbol FindWallTagType(Document doc)
        {
            if (!string.IsNullOrEmpty(_tagTypeId) && long.TryParse(_tagTypeId, out long id))
            {
                ElementId elementId = new ElementId(id);
                Element element = doc.GetElement(elementId);
                if (element is FamilySymbol symbol &&
                    (symbol.Category.Id.Value == (long)BuiltInCategory.OST_WallTags ||
                     symbol.Category.Id.Value == (long)BuiltInCategory.OST_MultiCategoryTags))
                {
                    return symbol;
                }
            }

            return new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .WhereElementIsElementType()
                .Where(e => e.Category != null && e.Category.Id.Value == (long)BuiltInCategory.OST_WallTags)
                .Cast<FamilySymbol>()
                .FirstOrDefault() ??
                new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .WhereElementIsElementType()
                .Where(e => e.Category != null && e.Category.Id.Value == (long)BuiltInCategory.OST_MultiCategoryTags)
                .Cast<FamilySymbol>()
                .FirstOrDefault();
        }
    }
}
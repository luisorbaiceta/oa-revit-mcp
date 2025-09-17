using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPCommandSet.Models.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitMCPCommandSet.Utils
{
    public static class OperateElementUtils
    {
        public static bool ExecuteElementOperation(UIDocument uidoc, OperationSetting setting)
        {
            if (uidoc == null || uidoc.Document == null || setting == null || setting.ElementIds == null || (setting.ElementIds.Count == 0 && setting.Action.ToLower() != "resetisolate"))
                throw new Exception("Invalid parameters: document is null or no elements specified for operation.");

            Document doc = uidoc.Document;
            ICollection<ElementId> elementIds = setting.ElementIds.Select(id => new ElementId(id)).ToList();

            if (!Enum.TryParse(setting.Action, true, out ElementOperationType action))
            {
                throw new Exception($"Unsupported action type: {setting.Action}");
            }

            switch (action)
            {
                case ElementOperationType.Select:
                    uidoc.Selection.SetElementIds(elementIds);
                    return true;

                case ElementOperationType.SelectionBox:
                    View3D targetView = doc.ActiveView as View3D;
                    if (targetView == null)
                    {
                        targetView = new FilteredElementCollector(doc).OfClass(typeof(View3D)).Cast<View3D>().FirstOrDefault(v => !v.IsTemplate && !v.IsLocked && (v.Name.Contains("{3D}") || v.Name.Contains("Default 3D")));
                        if (targetView == null) throw new Exception("Could not find a suitable 3D view for section box.");
                        uidoc.ActiveView = targetView;
                    }
                    BoundingBoxXYZ boundingBox = null;
                    foreach (ElementId id in elementIds)
                    {
                        Element elem = doc.GetElement(id);
                        if (elem.get_BoundingBox(null) is BoundingBoxXYZ elemBox)
                        {
                            if (boundingBox == null)
                            {
                                boundingBox = new BoundingBoxXYZ { Min = new XYZ(elemBox.Min.X, elemBox.Min.Y, elemBox.Min.Z), Max = new XYZ(elemBox.Max.X, elemBox.Max.Y, elemBox.Max.Z) };
                            }
                            else
                            {
                                boundingBox.Min = new XYZ(Math.Min(boundingBox.Min.X, elemBox.Min.X), Math.Min(boundingBox.Min.Y, elemBox.Min.Y), Math.Min(boundingBox.Min.Z, elemBox.Min.Z));
                                boundingBox.Max = new XYZ(Math.Max(boundingBox.Max.X, elemBox.Max.X), Math.Max(boundingBox.Max.Y, elemBox.Max.Y), Math.Max(boundingBox.Max.Z, elemBox.Max.Z));
                            }
                        }
                    }
                    if (boundingBox == null) throw new Exception("Could not create bounding box for the selected elements.");
                    double offset = 1.0;
                    boundingBox.Min = new XYZ(boundingBox.Min.X - offset, boundingBox.Min.Y - offset, boundingBox.Min.Z - offset);
                    boundingBox.Max = new XYZ(boundingBox.Max.X + offset, boundingBox.Max.Y + offset, boundingBox.Max.Z + offset);
                    using (Transaction trans = new Transaction(doc, "Create Section Box"))
                    {
                        trans.Start();
                        targetView.IsSectionBoxActive = true;
                        targetView.SetSectionBox(boundingBox);
                        trans.Commit();
                    }
                    uidoc.ShowElements(elementIds);
                    return true;

                case ElementOperationType.SetColor:
                    using (Transaction trans = new Transaction(doc, "Set Element Color"))
                    {
                        trans.Start();
                        SetElementsColor(doc, elementIds, setting.ColorValue);
                        trans.Commit();
                    }
                    uidoc.ShowElements(elementIds);
                    return true;

                case ElementOperationType.SetTransparency:
                    using (Transaction trans = new Transaction(doc, "Set Element Transparency"))
                    {
                        trans.Start();
                        OverrideGraphicSettings overrideSettings = new OverrideGraphicSettings();
                        int transparencyValue = Math.Max(0, Math.Min(100, setting.TransparencyValue));
                        overrideSettings.SetSurfaceTransparency(transparencyValue);
                        foreach (ElementId id in elementIds)
                        {
                            doc.ActiveView.SetElementOverrides(id, overrideSettings);
                        }
                        trans.Commit();
                    }
                    return true;

                case ElementOperationType.Delete:
                    using (Transaction trans = new Transaction(doc, "Delete Elements"))
                    {
                        trans.Start();
                        doc.Delete(elementIds);
                        trans.Commit();
                    }
                    return true;

                case ElementOperationType.Hide:
                    using (Transaction trans = new Transaction(doc, "Hide Elements"))
                    {
                        trans.Start();
                        doc.ActiveView.HideElements(elementIds);
                        trans.Commit();
                    }
                    return true;

                case ElementOperationType.TempHide:
                    using (Transaction trans = new Transaction(doc, "Temporarily Hide Elements"))
                    {
                        trans.Start();
                        doc.ActiveView.HideElementsTemporary(elementIds);
                        trans.Commit();
                    }
                    return true;

                case ElementOperationType.Isolate:
                    using (Transaction trans = new Transaction(doc, "Isolate Elements"))
                    {
                        trans.Start();
                        doc.ActiveView.IsolateElementsTemporary(elementIds);
                        trans.Commit();
                    }
                    return true;

                case ElementOperationType.Unhide:
                    using (Transaction trans = new Transaction(doc, "Unhide Elements"))
                    {
                        trans.Start();
                        doc.ActiveView.UnhideElements(elementIds);
                        trans.Commit();
                    }
                    return true;

                case ElementOperationType.ResetIsolate:
                    using (Transaction trans = new Transaction(doc, "Reset Isolate/Hide"))
                    {
                        trans.Start();
                        doc.ActiveView.DisableTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate);
                        trans.Commit();
                    }
                    return true;

                default:
                    throw new Exception($"Unsupported action type: {setting.Action}");
            }
        }

        private static void SetElementsColor(Document doc, ICollection<ElementId> elementIds, int[] elementColor)
        {
            if (elementColor == null || elementColor.Length < 3)
            {
                elementColor = new int[] { 255, 0, 0 }; // Default to red
            }
            int r = Math.Max(0, Math.Min(255, elementColor[0]));
            int g = Math.Max(0, Math.Min(255, elementColor[1]));
            int b = Math.Max(0, Math.Min(255, elementColor[2]));
            Color color = new Color((byte)r, (byte)g, (byte)b);
            OverrideGraphicSettings overrideSettings = new OverrideGraphicSettings();
            overrideSettings.SetProjectionLineColor(color);
            overrideSettings.SetCutLineColor(color);
            overrideSettings.SetSurfaceForegroundPatternColor(color);
            overrideSettings.SetSurfaceBackgroundPatternColor(color);
            try
            {
                FillPatternElement solidPattern = new FilteredElementCollector(doc).OfClass(typeof(FillPatternElement)).Cast<FillPatternElement>().FirstOrDefault(p => p.GetFillPattern().IsSolidFill);
                if (solidPattern != null)
                {
                    overrideSettings.SetSurfaceForegroundPatternId(solidPattern.Id);
                    overrideSettings.SetSurfaceForegroundPatternVisible(true);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set fill pattern: {ex.Message}");
            }
            foreach (ElementId id in elementIds)
            {
                doc.ActiveView.SetElementOverrides(id, overrideSettings);
            }
        }
    }
}

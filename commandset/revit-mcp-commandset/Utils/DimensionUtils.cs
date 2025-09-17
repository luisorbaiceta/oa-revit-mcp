using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPCommandSet.Models.Annotation;
using System;
using System.Collections.Generic;

namespace RevitMCPCommandSet.Utils
{
    public static class DimensionUtils
    {
        private const double MILLIMETERS_TO_FEET = 1.0 / 304.8;

        public static List<Reference> GetReferences(Element element, View view)
        {
            var references = new List<Reference>();
            if (element is Wall wall)
            {
                var options = new Options { View = view, ComputeReferences = true };
                var geometry = wall.get_Geometry(options);
                if (geometry != null)
                {
                    foreach (var obj in geometry)
                    {
                        if (obj is Solid solid)
                        {
                            foreach (Face face in solid.Faces)
                            {
                                references.Add(face.Reference);
                                break;
                            }
                            if (references.Count == 0)
                            {
                                foreach (Edge edge in solid.Edges)
                                {
                                    references.Add(edge.Reference);
                                    break;
                                }
                            }
                        }
                    }
                }
                if (references.Count == 0 && wall.Location is LocationCurve)
                {
                    references.Add(new Reference(wall));
                }
            }
            else if (element is FamilyInstance familyInstance)
            {
                try
                {
                    Reference centerRef = familyInstance.GetReferenceByName("Center");
                    if (centerRef != null)
                    {
                        references.Add(centerRef);
                    }
                    else
                    {
                        references.Add(new Reference(familyInstance));
                    }
                }
                catch
                {
                    references.Add(new Reference(familyInstance));
                }
            }
            else
            {
                references.Add(new Reference(element));
            }
            return references;
        }

        public static Reference FindReferenceAtPoint(Document doc, View view, XYZ point)
        {
            try
            {
                var collector = new FilteredElementCollector(doc, view.Id);
                var elements = collector.WhereElementIsNotElementType().ToElements();
                Element closestElement = null;
                double minDistance = double.MaxValue;
                foreach (var element in elements)
                {
                    if (element.Location == null) continue;
                    XYZ elementPoint = null;
                    if (element.Location is LocationPoint locationPoint)
                    {
                        elementPoint = locationPoint.Point;
                    }
                    else if (element.Location is LocationCurve locationCurve)
                    {
                        elementPoint = locationCurve.Curve.Project(point).XYZPoint;
                    }
                    else
                    {
                        continue;
                    }
                    double distance = point.DistanceTo(elementPoint);
                    if (distance < minDistance)
                    {
                        closestElement = element;
                        minDistance = distance;
                    }
                }
                if (closestElement != null && minDistance < 5.0)
                {
                    return new Reference(closestElement);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error finding reference at point: {ex.Message}");
            }
            return null;
        }

        public static void ApplyDimensionParameters(Dimension dimension, DimensionCreationInfo dimensionInfo)
        {
            if (dimensionInfo.Options == null) return;
            foreach (var option in dimensionInfo.Options)
            {
                var param = dimension.LookupParameter(option.Key);
                if (param == null) continue;
                if (option.Value is double doubleValue && param.StorageType == StorageType.Double)
                {
                    param.Set(doubleValue * MILLIMETERS_TO_FEET);
                }
                else if (option.Value is int intValue && param.StorageType == StorageType.Integer)
                {
                    param.Set(intValue);
                }
                else if (option.Value is string stringValue && param.StorageType == StorageType.String)
                {
                    param.Set(stringValue);
                }
            }
        }

        public static XYZ ConvertToInternalCoordinates(double x, double y, double z)
        {
            return new XYZ(x * MILLIMETERS_TO_FEET, y * MILLIMETERS_TO_FEET, z * MILLIMETERS_TO_FEET);
        }
    }
}

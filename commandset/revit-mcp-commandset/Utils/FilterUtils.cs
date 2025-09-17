using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using RevitMCPCommandSet.Models.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitMCPCommandSet.Utils
{
    public static class FilterUtils
    {
        public static IList<Element> GetFilteredElements(Document doc, FilterSetting settings)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            if (!settings.Validate(out string errorMessage))
            {
                System.Diagnostics.Trace.WriteLine($"Invalid filter settings: {errorMessage}");
                return new List<Element>();
            }
            List<string> appliedFilters = new List<string>();
            List<Element> result = new List<Element>();
            if (settings.IncludeTypes && settings.IncludeInstances)
            {
                result.AddRange(GetElementsByKind(doc, settings, true, appliedFilters));
                result.AddRange(GetElementsByKind(doc, settings, false, appliedFilters));
            }
            else if (settings.IncludeInstances)
            {
                result = GetElementsByKind(doc, settings, false, appliedFilters);
            }
            else if (settings.IncludeTypes)
            {
                result = GetElementsByKind(doc, settings, true, appliedFilters);
            }

            if (appliedFilters.Count > 0)
            {
                System.Diagnostics.Trace.WriteLine($"Applied {appliedFilters.Count} filters: {string.Join(", ", appliedFilters)}");
                System.Diagnostics.Trace.WriteLine($"Final result: Found {result.Count} elements");
            }
            return result;
        }

        private static List<Element> GetElementsByKind(Document doc, FilterSetting settings, bool isElementType, List<string> appliedFilters)
        {
            FilteredElementCollector collector;
            if (!isElementType && settings.FilterVisibleInCurrentView && doc.ActiveView != null)
            {
                collector = new FilteredElementCollector(doc, doc.ActiveView.Id);
                appliedFilters.Add("Visible in current view");
            }
            else
            {
                collector = new FilteredElementCollector(doc);
            }
            if (isElementType)
            {
                collector = collector.WhereElementIsElementType();
                appliedFilters.Add("Element Types Only");
            }
            else
            {
                collector = collector.WhereElementIsNotElementType();
                appliedFilters.Add("Element Instances Only");
            }
            List<ElementFilter> filters = new List<ElementFilter>();
            if (!string.IsNullOrWhiteSpace(settings.FilterCategory))
            {
                if (!Enum.TryParse(settings.FilterCategory, true, out BuiltInCategory category))
                {
                    throw new ArgumentException($"Could not convert '{settings.FilterCategory}' to a valid Revit category.");
                }
                filters.Add(new ElementCategoryFilter(category));
                appliedFilters.Add($"Category: {settings.FilterCategory}");
            }
            if (!string.IsNullOrWhiteSpace(settings.FilterElementType))
            {
                Type elementType = Type.GetType(settings.FilterElementType, false, true);
                if (elementType != null)
                {
                    filters.Add(new ElementClassFilter(elementType));
                    appliedFilters.Add($"Element Type: {elementType.Name}");
                }
                else
                {
                    throw new Exception($"Warning: Could not find type '{settings.FilterElementType}'");
                }
            }
            if (!isElementType && settings.FilterFamilySymbolId > 0)
            {
                ElementId symbolId = new ElementId(settings.FilterFamilySymbolId);
                Element symbolElement = doc.GetElement(symbolId);
                if (symbolElement is FamilySymbol)
                {
                    filters.Add(new FamilyInstanceFilter(doc, symbolId));
                    FamilySymbol symbol = symbolElement as FamilySymbol;
                    string familyName = symbol.Family?.Name ?? "Unknown Family";
                    string symbolName = symbol.Name ?? "Unknown Type";
                    appliedFilters.Add($"Family Type: {familyName} - {symbolName} (ID: {settings.FilterFamilySymbolId})");
                }
                else
                {
                    string elementType = symbolElement != null ? symbolElement.GetType().Name : "non-existent";
                    System.Diagnostics.Trace.WriteLine($"Warning: Element with ID {settings.FilterFamilySymbolId} is not a valid FamilySymbol (actual type: {elementType})");
                }
            }
            if (settings.BoundingBoxMin != null && settings.BoundingBoxMax != null)
            {
                XYZ minXYZ = JZPoint.ToXYZ(settings.BoundingBoxMin);
                XYZ maxXYZ = JZPoint.ToXYZ(settings.BoundingBoxMax);
                Outline outline = new Outline(minXYZ, maxXYZ);
                filters.Add(new BoundingBoxIntersectsFilter(outline));
                appliedFilters.Add($"Bounding Box Filter: Min({settings.BoundingBoxMin.X:F2}, {settings.BoundingBoxMin.Y:F2}, {settings.BoundingBoxMin.Z:F2}), Max({settings.BoundingBoxMax.X:F2}, {settings.BoundingBoxMax.Y:F2}, {settings.BoundingBoxMax.Z:F2}) mm");
            }
            if (filters.Count > 0)
            {
                ElementFilter combinedFilter = filters.Count == 1 ? filters[0] : new LogicalAndFilter(filters);
                collector = collector.WherePasses(combinedFilter);
                if (filters.Count > 1)
                {
                    System.Diagnostics.Trace.WriteLine($"Applied a combined filter with {filters.Count} conditions (AND logic)");
                }
            }
            return collector.ToElements().ToList();
        }

        public static List<object> GetElementFullInfo(Document doc, IList<Element> elementCollector)
        {
            List<object> infoList = new List<object>();
            foreach (var element in elementCollector)
            {
                if (element?.Category?.HasMaterialQuantities ?? false)
                {
                    var info = CreateElementFullInfo(doc, element);
                    if (info != null) infoList.Add(info);
                }
                else if (element is ElementType elementType)
                {
                    var info = CreateTypeFullInfo(doc, elementType);
                    if (info != null) infoList.Add(info);
                }
                else if (element is Level || element is Grid)
                {
                    var info = CreatePositioningElementInfo(doc, element);
                    if (info != null) infoList.Add(info);
                }
                else if (element is SpatialElement)
                {
                    var info = CreateSpatialElementInfo(doc, element);
                    if (info != null) infoList.Add(info);
                }
                else if (element is View)
                {
                    var info = CreateViewInfo(doc, element);
                    if (info != null) infoList.Add(info);
                }
                else if (element is TextNote || element is Dimension || element is IndependentTag || element is AnnotationSymbol || element is SpotDimension)
                {
                    var info = CreateAnnotationInfo(doc, element);
                    if (info != null) infoList.Add(info);
                }
                else if (element is Group || element is RevitLinkInstance)
                {
                    var info = CreateGroupOrLinkInfo(doc, element);
                    if (info != null) infoList.Add(info);
                }
                else
                {
                    var info = CreateElementBasicInfo(doc, element);
                    if (info != null) infoList.Add(info);
                }
            }
            return infoList;
        }

        public static ElementInstanceInfo CreateElementFullInfo(Document doc, Element element)
        {
            try
            {
                if (element?.Category == null) return null;
                ElementInstanceInfo elementInfo = new ElementInstanceInfo
                {
                    Id = (int)element.Id.Value,
                    UniqueId = element.UniqueId,
                    Name = element.Name,
                    FamilyName = element.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM)?.AsValueString(),
                    Category = element.Category.Name,
                    BuiltInCategory = Enum.GetName(typeof(BuiltInCategory), (int)element.Category.Id.Value),
                    TypeId = (int)element.GetTypeId().Value
                };
                if (element is FamilyInstance instance)
                    elementInfo.RoomId = instance.Room?.Id.Value ?? -1;
                elementInfo.Level = GetElementLevel(doc, element);
                elementInfo.BoundingBox = GetBoundingBoxInfo(element);
                ParameterInfo thicknessParam = GetThicknessInfo(element);
                if (thicknessParam != null)
                {
                    elementInfo.Parameters.Add(thicknessParam);
                }
                ParameterInfo heightParam = GetBoundingBoxHeight(elementInfo.BoundingBox);
                if (heightParam != null)
                {
                    elementInfo.Parameters.Add(heightParam);
                }
                return elementInfo;
            }
            catch { return null; }
        }

        public static ElementTypeInfo CreateTypeFullInfo(Document doc, ElementType elementType)
        {
            ElementTypeInfo typeInfo = new ElementTypeInfo
            {
                Id = (int)elementType.Id.Value,
                UniqueId = elementType.UniqueId,
                Name = elementType.Name,
                FamilyName = elementType.FamilyName,
                Category = elementType.Category.Name,
                BuiltInCategory = Enum.GetName(typeof(BuiltInCategory), (int)elementType.Category.Id.Value),
                Parameters = GetDimensionParameters(elementType)
            };
            ParameterInfo thicknessParam = GetThicknessInfo(elementType);
            if (thicknessParam != null)
            {
                typeInfo.Parameters.Add(thicknessParam);
            }
            return typeInfo;
        }

        public static PositioningElementInfo CreatePositioningElementInfo(Document doc, Element element)
        {
            try
            {
                if (element == null) return null;
                PositioningElementInfo info = new PositioningElementInfo
                {
                    Id = (int)element.Id.Value,
                    UniqueId = element.UniqueId,
                    Name = element.Name,
                    FamilyName = element.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM)?.AsValueString(),
                    Category = element.Category?.Name,
                    BuiltInCategory = element.Category != null ? Enum.GetName(typeof(BuiltInCategory), (int)element.Category.Id.Value) : null,
                    ElementClass = element.GetType().Name,
                    BoundingBox = GetBoundingBoxInfo(element)
                };
                if (element is Level level)
                {
                    info.Elevation = level.Elevation * 304.8;
                }
                else if (element is Grid grid)
                {
                    Curve curve = grid.Curve;
                    if (curve != null)
                    {
                        XYZ start = curve.GetEndPoint(0);
                        XYZ end = curve.GetEndPoint(1);
                        info.GridLine = new JZLine(start.X * 304.8, start.Y * 304.8, start.Z * 304.8, end.X * 304.8, end.Y * 304.8, end.Z * 304.8);
                    }
                }
                info.Level = GetElementLevel(doc, element);
                return info;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Error creating positioning element info: {ex.Message}");
                return null;
            }
        }

        public static SpatialElementInfo CreateSpatialElementInfo(Document doc, Element element)
        {
            try
            {
                if (!(element is SpatialElement spatialElement)) return null;
                SpatialElementInfo info = new SpatialElementInfo
                {
                    Id = (int)element.Id.Value,
                    UniqueId = element.UniqueId,
                    Name = element.Name,
                    FamilyName = element.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM)?.AsValueString(),
                    Category = element.Category?.Name,
                    BuiltInCategory = element.Category != null ? Enum.GetName(typeof(BuiltInCategory), (int)element.Category.Id.Value) : null,
                    ElementClass = element.GetType().Name,
                    BoundingBox = GetBoundingBoxInfo(element)
                };
                if (element is Room room)
                {
                    info.Number = room.Number;
                    info.Volume = room.Volume * Math.Pow(304.8, 3);
                }
                else if (element is Area area)
                {
                    info.Number = area.Number;
                }
                Parameter areaParam = element.get_Parameter(BuiltInParameter.ROOM_AREA);
                if (areaParam != null && areaParam.HasValue)
                {
                    info.Area = areaParam.AsDouble() * Math.Pow(304.8, 2);
                }
                Parameter perimeterParam = element.get_Parameter(BuiltInParameter.ROOM_PERIMETER);
                if (perimeterParam != null && perimeterParam.HasValue)
                {
                    info.Perimeter = perimeterParam.AsDouble() * 304.8;
                }
                info.Level = GetElementLevel(doc, element);
                return info;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Error creating spatial element info: {ex.Message}");
                return null;
            }
        }

        public static ViewInfo CreateViewInfo(Document doc, Element element)
        {
            try
            {
                if (!(element is View view)) return null;
                ViewInfo info = new ViewInfo
                {
                    Id = (int)element.Id.Value,
                    UniqueId = element.UniqueId,
                    Name = element.Name,
                    FamilyName = element.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM)?.AsValueString(),
                    Category = element.Category?.Name,
                    BuiltInCategory = element.Category != null ? Enum.GetName(typeof(BuiltInCategory), (int)element.Category.Id.Value) : null,
                    ElementClass = element.GetType().Name,
                    ViewType = view.ViewType.ToString(),
                    Scale = view.Scale,
                    IsTemplate = view.IsTemplate,
                    DetailLevel = view.DetailLevel.ToString(),
                    BoundingBox = GetBoundingBoxInfo(element)
                };
                if (view is ViewPlan viewPlan && viewPlan.GenLevel != null)
                {
                    Level level = viewPlan.GenLevel;
                    info.AssociatedLevel = new LevelInfo
                    {
                        Id = (int)level.Id.Value,
                        Name = level.Name,
                        Height = level.Elevation * 304.8
                    };
                }
                UIDocument uidoc = new UIDocument(doc);
                IList<UIView> openViews = uidoc.GetOpenUIViews();
                foreach (UIView uiView in openViews)
                {
                    if (uiView.ViewId.Value == view.Id.Value)
                    {
                        info.IsOpen = true;
                        if (uidoc.ActiveView.Id.Value == view.Id.Value)
                        {
                            info.IsActive = true;
                        }
                        break;
                    }
                }
                return info;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Error creating view info: {ex.Message}");
                return null;
            }
        }

        public static AnnotationInfo CreateAnnotationInfo(Document doc, Element element)
        {
            try
            {
                if (element == null) return null;
                AnnotationInfo info = new AnnotationInfo
                {
                    Id = (int)element.Id.Value,
                    UniqueId = element.UniqueId,
                    Name = element.Name,
                    FamilyName = element.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM)?.AsValueString(),
                    Category = element.Category?.Name,
                    BuiltInCategory = element.Category != null ? Enum.GetName(typeof(BuiltInCategory), (int)element.Category.Id.Value) : null,
                    ElementClass = element.GetType().Name,
                    BoundingBox = GetBoundingBoxInfo(element)
                };
                Parameter viewParam = element.get_Parameter(BuiltInParameter.VIEW_NAME);
                if (viewParam != null && viewParam.HasValue)
                {
                    info.OwnerView = viewParam.AsString();
                }
                else if (element.OwnerViewId != ElementId.InvalidElementId)
                {
                    View ownerView = doc.GetElement(element.OwnerViewId) as View;
                    info.OwnerView = ownerView?.Name;
                }
                if (element is TextNote textNote)
                {
                    info.TextContent = textNote.Text;
                    XYZ position = textNote.Coord;
                    info.Position = new JZPoint(position.X * 304.8, position.Y * 304.8, position.Z * 304.8);
                }
                else if (element is Dimension dimension)
                {
                    info.DimensionValue = dimension.Value.ToString();
                    XYZ origin = dimension.Origin;
                    info.Position = new JZPoint(origin.X * 304.8, origin.Y * 304.8, origin.Z * 304.8);
                }
                else if (element is AnnotationSymbol annotationSymbol)
                {
                    if (annotationSymbol.Location is LocationPoint locationPoint)
                    {
                        XYZ position = locationPoint.Point;
                        info.Position = new JZPoint(position.X * 304.8, position.Y * 304.8, position.Z * 304.8);
                    }
                }
                return info;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Error creating annotation info: {ex.Message}");
                return null;
            }
        }

        public static GroupOrLinkInfo CreateGroupOrLinkInfo(Document doc, Element element)
        {
            try
            {
                if (element == null) return null;
                GroupOrLinkInfo info = new GroupOrLinkInfo
                {
                    Id = (int)element.Id.Value,
                    UniqueId = element.UniqueId,
                    Name = element.Name,
                    FamilyName = element.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM)?.AsValueString(),
                    Category = element.Category?.Name,
                    BuiltInCategory = element.Category != null ? Enum.GetName(typeof(BuiltInCategory), (int)element.Category.Id.Value) : null,
                    ElementClass = element.GetType().Name,
                    BoundingBox = GetBoundingBoxInfo(element)
                };
                if (element is Group group)
                {
                    ICollection<ElementId> memberIds = group.GetMemberIds();
                    info.MemberCount = memberIds?.Count;
                    info.GroupType = group.GroupType?.Name;
                }
                else if (element is RevitLinkInstance linkInstance)
                {
                    if (doc.GetElement(linkInstance.GetTypeId()) is RevitLinkType linkType)
                    {
                        ExternalFileReference extFileRef = linkType.GetExternalFileReference();
                        string absPath = ModelPathUtils.ConvertModelPathToUserVisiblePath(extFileRef.GetAbsolutePath());
                        info.LinkPath = absPath;
                        LinkedFileStatus linkStatus = linkType.GetLinkedFileStatus();
                        info.LinkStatus = linkStatus.ToString();
                    }
                    else
                    {
                        info.LinkStatus = LinkedFileStatus.Invalid.ToString();
                    }
                    if (linkInstance.Location is LocationPoint location)
                    {
                        XYZ point = location.Point;
                        info.Position = new JZPoint(point.X * 304.8, point.Y * 304.8, point.Z * 304.8);
                    }
                }
                return info;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Error creating group or link info: {ex.Message}");
                return null;
            }
        }

        public static ElementBasicInfo CreateElementBasicInfo(Document doc, Element element)
        {
            try
            {
                if (element == null) return null;
                return new ElementBasicInfo
                {
                    Id = (int)element.Id.Value,
                    UniqueId = element.UniqueId,
                    Name = element.Name,
                    FamilyName = element.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM)?.AsValueString(),
                    Category = element.Category?.Name,
                    BuiltInCategory = element.Category != null ? Enum.GetName(typeof(BuiltInCategory), (int)element.Category.Id.Value) : null,
                    BoundingBox = GetBoundingBoxInfo(element)
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Error creating basic element info: {ex.Message}");
                return null;
            }
        }

        public static ParameterInfo GetThicknessInfo(Element element)
        {
            if (!(element.Document.GetElement(element.GetTypeId()) is ElementType elementType)) return null;
            Parameter thicknessParam = null;
            if (elementType is WallType)
            {
                thicknessParam = elementType.get_Parameter(BuiltInParameter.WALL_ATTR_WIDTH_PARAM);
            }
            else if (elementType is FloorType)
            {
                thicknessParam = elementType.get_Parameter(BuiltInParameter.FLOOR_ATTR_THICKNESS_PARAM);
            }
            else if (elementType is FamilySymbol familySymbol)
            {
                switch ((BuiltInCategory)elementType.Category.Id.Value)
                {
                    case BuiltInCategory.OST_Doors:
                    case BuiltInCategory.OST_Windows:
                        thicknessParam = elementType.get_Parameter(BuiltInParameter.FAMILY_THICKNESS_PARAM);
                        break;
                }
            }
            else if (elementType is CeilingType)
            {
                thicknessParam = elementType.get_Parameter(BuiltInParameter.CEILING_THICKNESS);
            }
            if (thicknessParam != null && thicknessParam.HasValue)
            {
                return new ParameterInfo
                {
                    Name = "Thickness",
                    Value = $"{thicknessParam.AsDouble() * 304.8}"
                };
            }
            return null;
        }

        public static LevelInfo GetElementLevel(Document doc, Element element)
        {
            try
            {
                Level level = null;
                if (element is Wall wall)
                {
                    level = doc.GetElement(wall.LevelId) as Level;
                }
                else if (element is Floor floor)
                {
                    if (floor.get_Parameter(BuiltInParameter.LEVEL_PARAM) is Parameter levelParam && levelParam.HasValue)
                    {
                        level = doc.GetElement(levelParam.AsElementId()) as Level;
                    }
                }
                else if (element is FamilyInstance familyInstance)
                {
                    if (familyInstance.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM) is Parameter levelParam && levelParam.HasValue)
                    {
                        level = doc.GetElement(levelParam.AsElementId()) as Level;
                    }
                    if (level == null)
                    {
                        if (familyInstance.get_Parameter(BuiltInParameter.SCHEDULE_LEVEL_PARAM) is Parameter scheduleLevelParam && scheduleLevelParam.HasValue)
                        {
                            level = doc.GetElement(scheduleLevelParam.AsElementId()) as Level;
                        }
                    }
                }
                else
                {
                    if (element.get_Parameter(BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM) is Parameter levelParam && levelParam.HasValue)
                    {
                        level = doc.GetElement(levelParam.AsElementId()) as Level;
                    }
                }
                if (level != null)
                {
                    return new LevelInfo
                    {
                        Id = (int)level.Id.Value,
                        Name = level.Name,
                        Height = level.Elevation * 304.8
                    };
                }
                return null;
            }
            catch { return null; }
        }

        public static BoundingBoxInfo GetBoundingBoxInfo(Element element)
        {
            try
            {
                if (element.get_BoundingBox(null) is BoundingBoxXYZ bbox)
                {
                    return new BoundingBoxInfo
                    {
                        Min = new JZPoint(bbox.Min.X * 304.8, bbox.Min.Y * 304.8, bbox.Min.Z * 304.8),
                        Max = new JZPoint(bbox.Max.X * 304.8, bbox.Max.Y * 304.8, bbox.Max.Z * 304.8)
                    };
                }
                return null;
            }
            catch { return null; }
        }

        public static ParameterInfo GetBoundingBoxHeight(BoundingBoxInfo boundingBoxInfo)
        {
            try
            {
                if (boundingBoxInfo?.Min == null || boundingBoxInfo?.Max == null) return null;
                double height = Math.Abs(boundingBoxInfo.Max.Z - boundingBoxInfo.Min.Z);
                return new ParameterInfo { Name = "Height", Value = $"{height}" };
            }
            catch { return null; }
        }

        public static List<ParameterInfo> GetDimensionParameters(Element element)
        {
            if (element == null) return new List<ParameterInfo>();
            var parameters = new List<ParameterInfo>();
            foreach (Parameter param in element.Parameters)
            {
                try
                {
                    if (!param.HasValue || param.IsReadOnly) continue;
                    if (IsDimensionParameter(param))
                    {
                        string value = param.AsValueString();
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            parameters.Add(new ParameterInfo { Name = param.Definition.Name, Value = value });
                        }
                    }
                }
                catch { continue; }
            }
            return parameters.OrderBy(p => p.Name).ToList();
        }

        public static bool IsDimensionParameter(Parameter param)
        {
            ForgeTypeId paramTypeId = param.Definition.GetDataType();
            return paramTypeId.Equals(SpecTypeId.Length) ||
                   paramTypeId.Equals(SpecTypeId.Angle) ||
                   paramTypeId.Equals(SpecTypeId.Area) ||
                   paramTypeId.Equals(SpecTypeId.Volume);
        }
    }
}

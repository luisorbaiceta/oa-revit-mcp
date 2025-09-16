using Autodesk.Revit.DB;
using RevitMCPCommandSet.Models.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitMCPCommandSet.Utils
{
    public static class ParameterUtils
    {
        public static List<ParameterInfo> GetAllParameters(Element element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            var parameterInfos = new List<ParameterInfo>();
            foreach (Parameter parameter in element.Parameters)
            {
                parameterInfos.Add(CreateParameterInfo(parameter));
            }
            return parameterInfos;
        }

        public static ParameterInfo GetParameter(Element element, string parameterName)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }
            if (string.IsNullOrEmpty(parameterName))
            {
                throw new ArgumentNullException(nameof(parameterName));
            }

            var parameter = element.LookupParameter(parameterName);
            if (parameter == null)
            {
                throw new ArgumentException($"Parameter '{parameterName}' not found on the element.", nameof(parameterName));
            }
            return CreateParameterInfo(parameter);
        }

        public static void SetParameterValue(Element element, string parameterName, object value)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }
            if (string.IsNullOrEmpty(parameterName))
            {
                throw new ArgumentNullException(nameof(parameterName));
            }

            var parameter = element.LookupParameter(parameterName);
            if (parameter == null)
            {
                throw new ArgumentException($"Parameter '{parameterName}' not found on the element.", nameof(parameterName));
            }
            if (parameter.IsReadOnly)
            {
                throw new InvalidOperationException($"Parameter '{parameterName}' is read-only.");
            }

            switch (parameter.StorageType)
            {
                case StorageType.Double:
                    if (value is double d)
                        parameter.Set(d);
                    else if (value is int i)
                        parameter.Set(Convert.ToDouble(i));
                    else if (double.TryParse(value.ToString(), out double doubleValue))
                        parameter.Set(doubleValue);
                    else
                        throw new ArgumentException("Invalid value type for a double parameter.");
                    break;
                case StorageType.Integer:
                    if (value is int i2)
                        parameter.Set(i2);
                    else if (int.TryParse(value.ToString(), out int intValue))
                        parameter.Set(intValue);
                    else
                        throw new ArgumentException("Invalid value type for an integer parameter.");
                    break;
                case StorageType.String:
                    parameter.Set(value.ToString());
                    break;
                case StorageType.ElementId:
                    if (value is ElementId id)
                        parameter.Set(id);
                    else if (value is int intId)
                        parameter.Set(new ElementId(intId));
                    else
                        throw new ArgumentException("Invalid value type for an ElementId parameter.");
                    break;
                case StorageType.None:
                     throw new InvalidOperationException("Cannot set value for a parameter with no storage type.");
                default:
                    throw new InvalidOperationException($"Unsupported storage type: {parameter.StorageType}");
            }
        }

        public static List<ParameterInfo> GetAllProjectParameters(Document doc)
        {
            return GetParameters(doc, false);
        }

        public static List<ParameterInfo> GetAllSharedParameters(Document doc)
        {
            return GetParameters(doc, true);
        }

        private static List<ParameterInfo> GetParameters(Document doc, bool isShared)
        {
            if (doc == null)
            {
                throw new ArgumentNullException(nameof(doc));
            }

            var parameters = new List<ParameterInfo>();
            var bindingMap = doc.ParameterBindings;
            var it = bindingMap.ForwardIterator();
            it.Reset();
            while (it.MoveNext())
            {
                var definition = it.Key as InternalDefinition;
                var binding = it.Current as ElementBinding;

                bool isSharedParam = definition is ExternalDefinition;

                if ((isShared && isSharedParam) || (!isShared && !isSharedParam))
                {
                    parameters.Add(new ParameterInfo
                    {
                        Name = definition.Name,
                        Group = LabelUtils.GetLabelFor(definition.GetGroupTypeId()),
                        Unit = LabelUtils.GetLabelFor(definition.GetDataType()),
                        IsReadOnly = false
                    });
                }
            }
            return parameters;
        }

        private static ParameterInfo CreateParameterInfo(Parameter parameter)
        {
            var parameterInfo = new ParameterInfo
            {
                Name = parameter.Definition.Name,
                IsReadOnly = parameter.IsReadOnly,
                Group = LabelUtils.GetLabelFor(parameter.Definition.GetGroupTypeId()),
                Unit = LabelUtils.GetLabelFor(parameter.Definition.GetDataType())
            };

            switch (parameter.StorageType)
            {
                case StorageType.Double:
                    parameterInfo.Value = parameter.AsDouble();
                    break;
                case StorageType.Integer:
                    parameterInfo.Value = parameter.AsInteger();
                    break;
                case StorageType.String:
                    parameterInfo.Value = parameter.AsString();
                    break;
                case StorageType.ElementId:
                    parameterInfo.Value = parameter.AsElementId()?.IntegerValue;
                    break;
                case StorageType.None:
                    parameterInfo.Value = parameter.AsValueString();
                    break;
            }
            return parameterInfo;
        }

        public static ElementId CreateGlobalParameter(Document doc, string name, ForgeTypeId spec, bool isReporting)
        {
            if (!GlobalParametersManager.AreGlobalParametersAllowed(doc))
                throw new InvalidOperationException("Global parameters are not permitted in the given document.");

            if (!GlobalParametersManager.IsUniqueName(doc, name))
                throw new ArgumentException("A global parameter with that name already exists.", nameof(name));

            var gpDefinition = GlobalParameter.Create(doc, name, spec);
            if(gpDefinition == null)
                throw new InvalidOperationException("Failed to create global parameter.");

            var gp = doc.GetElement(gpDefinition.Id) as GlobalParameter;
            if(gp != null)
            {
                gp.IsReporting = isReporting;
            }

            return gpDefinition.Id;
        }

        public static List<GlobalParameterInfo> GetAllGlobalParameters(Document doc)
        {
            var gpIds = GlobalParametersManager.GetAllGlobalParameters(doc);
            var gpList = new List<GlobalParameterInfo>();

            foreach (var gpId in gpIds)
            {
                var gp = doc.GetElement(gpId) as GlobalParameter;
                if (gp == null) continue;

                var gpValue = gp.GetValue();
                object value = null;
                if (gpValue is DoubleParameterValue dpv) value = dpv.Value;
                else if (gpValue is IntegerParameterValue ipv) value = ipv.Value;
                else if (gpValue is StringParameterValue spv) value = spv.Value;
                else if (gpValue is ElementIdParameterValue epv) value = epv.Value.IntegerValue;

                var gpInfo = new GlobalParameterInfo
                {
                    Id = gp.Id.IntegerValue,
                    Name = gp.Name,
                    IsReporting = gp.IsReporting,
                    Value = value,
                    Type = LabelUtils.GetLabelFor(gp.GetDefinition().GetDataType()),
                    Spec = LabelUtils.GetLabelFor(gp.GetDefinition().GetSpecTypeId())
                };
                gpList.Add(gpInfo);
            }

            return gpList;
        }

        public static void SetGlobalParameterValue(Document doc, ElementId id, object value)
        {
            var gp = doc.GetElement(id) as GlobalParameter;
            if (gp == null)
                throw new ArgumentException("Global parameter not found.", nameof(id));

            ParameterValue gpValue = null;

            if (value is double d) gpValue = new DoubleParameterValue(d);
            else if (value is int i) gpValue = new IntegerParameterValue(i);
            else if (value is string s) gpValue = new StringParameterValue(s);
            else if (value is ElementId eid) gpValue = new ElementIdParameterValue(eid);
            else if (value is long l && l <= int.MaxValue && l >= int.MinValue) gpValue = new IntegerParameterValue((int)l);
            else
            {
                throw new ArgumentException("Unsupported value type for global parameter.");
            }

            gp.SetValue(gpValue);
        }

        public static void AssignGlobalParameterToElement(Document doc, ElementId elementId, string parameterName, ElementId globalParameterId)
        {
            var element = doc.GetElement(elementId);
            if (element == null)
                throw new ArgumentException("Element not found.", nameof(elementId));

            var parameter = element.LookupParameter(parameterName);
            if (parameter == null)
                throw new ArgumentException($"Parameter '{parameterName}' not found on element.", nameof(parameterName));

            if (!parameter.CanBeAssociatedWithGlobalParameter(globalParameterId))
                throw new InvalidOperationException("This parameter cannot be associated with the specified global parameter.");

            parameter.AssociateWithGlobalParameter(globalParameterId);
        }
    }
}

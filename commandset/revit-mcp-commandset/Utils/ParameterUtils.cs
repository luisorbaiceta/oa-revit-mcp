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
            var parameterInfos = new List<ParameterInfo>();
            foreach (Parameter parameter in element.Parameters)
            {
                parameterInfos.Add(CreateParameterInfo(parameter));
            }
            return parameterInfos;
        }

        public static ParameterInfo GetParameter(Element element, string parameterName)
        {
            var parameter = element.LookupParameter(parameterName);
            if (parameter == null)
            {
                return null;
            }
            return CreateParameterInfo(parameter);
        }

        public static void SetParameterValue(Element element, string parameterName, object value)
        {
            var parameter = element.LookupParameter(parameterName);
            if (parameter == null || parameter.IsReadOnly)
            {
                return;
            }
            // You might need to handle different data types here
            parameter.Set(value.ToString());
        }

        public static List<ParameterInfo> GetAllProjectParameters(Document doc)
        {
            var projectParameters = new List<ParameterInfo>();
            var bindingMap = doc.ParameterBindings;
            var it = bindingMap.ForwardIterator();
            it.Reset();
            while (it.MoveNext())
            {
                var definition = it.Key as InternalDefinition;
                if (definition == null) continue;
                projectParameters.Add(new ParameterInfo
                {
                    Name = definition.Name,
                    Group = definition.get_ParameterGroup(doc.Application.ActiveUIDocument.Document.DisplayUnitSystem).ToString(),
                    Unit = definition.GetUnitTypeId().TypeId,
                    IsReadOnly = false // Project parameters are generally not read-only by definition
                });
            }
            return projectParameters;
        }

        public static List<ParameterInfo> GetAllSharedParameters(Document doc)
        {
            var sharedParameters = new List<ParameterInfo>();
            var bindingMap = doc.ParameterBindings;
            var it = bindingMap.ForwardIterator();
            it.Reset();
            while (it.MoveNext())
            {
                if (it.Key is ExternalDefinition definition)
                {
                    sharedParameters.Add(new ParameterInfo
                    {
                        Name = definition.Name,
                        Group = definition.get_ParameterGroup(doc.Application.ActiveUIDocument.Document.DisplayUnitSystem).ToString(),
                        Unit = definition.GetUnitTypeId().TypeId,
                        IsReadOnly = false
                    });
                }
            }
            return sharedParameters;
        }

        private static ParameterInfo CreateParameterInfo(Parameter parameter)
        {
            var parameterInfo = new ParameterInfo
            {
                Name = parameter.Definition.Name,
                IsReadOnly = parameter.IsReadOnly,
                Group = parameter.Definition.get_ParameterGroup(parameter.Element.Document.Application.ActiveUIDocument.Document.DisplayUnitSystem).ToString()
            };

            switch (parameter.StorageType)
            {
                case StorageType.Double:
                    parameterInfo.Value = parameter.AsDouble();
                    parameterInfo.Unit = parameter.GetUnitTypeId()?.TypeId;
                    break;
                case StorageType.Integer:
                    parameterInfo.Value = parameter.AsInteger();
                    break;
                case StorageType.String:
                    parameterInfo.Value = parameter.AsString();
                    break;
                case StorageType.ElementId:
                    parameterInfo.Value = parameter.AsElementId().IntegerValue;
                    break;
                case StorageType.None:
                    parameterInfo.Value = parameter.AsValueString();
                    break;
            }
            return parameterInfo;
        }

        public static ElementId CreateGlobalParameter(Document doc, string name, ParameterType type, ForgeTypeId spec, bool isReporting)
        {
            if (!GlobalParametersManager.AreGlobalParametersAllowed(doc))
                throw new InvalidOperationException("Global parameters are not permitted in the given document.");

            if (!GlobalParametersManager.IsUniqueName(doc, name))
                throw new ArgumentException("A global parameter with that name already exists.", nameof(name));

            var gpDefinition = GlobalParameter.Create(doc, name, type, spec);
            var gp = doc.GetElement(gpDefinition) as GlobalParameter;
            gp.IsReporting = isReporting;

            return gpDefinition;
        }

        public static List<GlobalParameterInfo> GetAllGlobalParameters(Document doc)
        {
            var gpIds = GlobalParametersManager.GetAllGlobalParameters(doc);
            var gpList = new List<GlobalParameterInfo>();

            foreach (var gpId in gpIds)
            {
                var gp = doc.GetElement(gpId) as GlobalParameter;
                if (gp == null) continue;

                var gpInfo = new GlobalParameterInfo
                {
                    Id = gp.Id.IntegerValue,
                    Name = gp.Name,
                    IsReporting = gp.IsReporting,
                    Value = gp.GetValue()?.Value,
                    Type = gp.GetDefinition().GetDataType().TypeId
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

            var gpValue = new DoubleParameterValue();
            if (value is double d)
            {
                gpValue.Value = d;
            }
            else if (value is int i)
            {
                gpValue.Value = i;
            }
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
                throw new ArgumentException("Parameter not found on element.", nameof(parameterName));

            if (!parameter.CanBeAssociatedWithGlobalParameter())
                throw new InvalidOperationException("This parameter cannot be associated with a global parameter.");

            parameter.AssociateWithGlobalParameter(globalParameterId);
        }
    }
}

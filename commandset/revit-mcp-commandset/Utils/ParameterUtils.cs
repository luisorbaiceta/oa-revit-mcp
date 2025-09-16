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
    }
}

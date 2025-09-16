using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Services;
using System;
using Autodesk.Revit.DB;

namespace RevitMCPCommandSet.Commands
{
    public class SetGlobalParameterValueCommand : ExternalEventCommandBase
    {
        public override string CommandName => "set_global_parameter_value";

        public SetGlobalParameterValueCommand(UIApplication uiApp)
            : base(new SetGlobalParameterValueEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                var id = new ElementId(parameters["id"].ToObject<int>());
                var value = parameters["value"].ToObject<object>();

                var handler = (SetGlobalParameterValueEventHandler)Handler;
                handler.Id = id;
                handler.Value = value;

                if (RaiseAndWaitForCompletion(10000))
                {
                    return handler.Result;
                }
                else
                {
                    throw new TimeoutException("Timeout setting global parameter value.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set global parameter value: {ex.Message}");
            }
        }
    }
}

using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Interfaces;
using System;
using System.Collections.Generic;

namespace RevitMCPCommandSet.Commands
{
    public class GenericCommand : IRevitCommand, IRevitCommandInitializable
    {
        private static readonly Dictionary<string, Type> _eventHandlerRegistry = new Dictionary<string, Type>();
        private IExternalEventHandler _handler;
        private UIApplication _uiApp;

        public string CommandName { get; private set; }

        public GenericCommand() { }

        public GenericCommand(string commandName)
        {
            CommandName = commandName;
        }

        public void Initialize(UIApplication uiApp)
        {
            _uiApp = uiApp;
            if (_eventHandlerRegistry.TryGetValue(CommandName, out Type handlerType))
            {
                _handler = (IExternalEventHandler)Activator.CreateInstance(handlerType);
            }
        }

        public static void RegisterEventHandler(string commandName, Type eventHandlerType)
        {
            _eventHandlerRegistry[commandName] = eventHandlerType;
        }

        public object Execute(JObject parameters, string requestId)
        {
            if (_handler == null)
            {
                throw new InvalidOperationException($"No event handler registered for command '{CommandName}'.");
            }

            // This part is tricky. The existing commands have custom logic to set parameters on the handler.
            // I will need to devise a generic way to do this. For now, I will assume the handler
            // has a method called 'SetParameters' that takes a JObject.
            var setParametersMethod = _handler.GetType().GetMethod("SetParameters");
            if (setParametersMethod != null)
            {
                setParametersMethod.Invoke(_handler, new object[] { parameters });
            }

            var externalEvent = ExternalEvent.Create(_handler);
            externalEvent.Raise();

            // The original commands have a 'RaiseAndWaitForCompletion' method.
            // I'll need to replicate that logic here. This will involve using a ManualResetEvent
            // or similar synchronization primitive in the event handlers.
            if (_handler is IWaitableExternalEventHandler waitableHandler)
            {
                if (waitableHandler.WaitForCompletion(15000))
                {
                    var resultProperty = _handler.GetType().GetProperty("Result");
                    if (resultProperty != null)
                    {
                        return resultProperty.GetValue(_handler);
                    }
                    return null;
                }
                else
                {
                    throw new TimeoutException($"Command '{CommandName}' timed out.");
                }
            }

            return null;
        }
    }
}

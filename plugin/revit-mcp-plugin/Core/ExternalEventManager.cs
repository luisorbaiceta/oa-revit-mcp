using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using System;
using System.Threading.Tasks;

namespace revit_mcp_plugin.Core
{
    /// <summary>
    /// Manages a single external event to run actions in a valid Revit API context.
    /// </summary>
    public class ExternalEventManager
    {
        private static ExternalEventManager _instance;
        private ActionEventHandler _handler;
        private ExternalEvent _externalEvent;
        private bool _isInitialized = false;
        private ILogger _logger;

        public static ExternalEventManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ExternalEventManager();
                return _instance;
            }
        }

        private ExternalEventManager() { }

        public void Initialize(UIApplication uiApp, ILogger logger)
        {
            if (_isInitialized) return;

            _logger = logger;
            _handler = new ActionEventHandler();
            _externalEvent = ExternalEvent.Create(_handler);
            _handler.SetExternalEvent(_externalEvent); // Pass the event to the handler
            _isInitialized = true;
            _logger.Info("ExternalEventManager initialized.");
        }

        /// <summary>
        /// Posts an action to be executed in the Revit API context.
        /// </summary>
        /// <param name="action">The action to execute. It takes a UIApplication and returns an object.</param>
        /// <returns>A task that completes with the result of the action.</returns>
        public Task<object> PostActionAsync(Func<UIApplication, object> action)
        {
            if (!_isInitialized)
            {
                _logger.Error("ExternalEventManager is not initialized.");
                throw new InvalidOperationException("ExternalEventManager is not initialized.");
            }

            var wrapper = new ActionWrapper
            {
                Action = action,
                Tcs = new TaskCompletionSource<object>()
            };

            _handler.EnqueueAction(wrapper);
            _externalEvent.Raise();

            return wrapper.Tcs.Task;
        }

        public void Dispose()
        {
            if (!_isInitialized) return;

            _externalEvent?.Dispose();
            _handler = null;
            _externalEvent = null;
            _isInitialized = false;
            _logger.Info("ExternalEventManager disposed.");
        }
    }
}
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace revit_mcp_plugin.Core
{
    /// <summary>
    /// Manages an external event to run actions in a valid Revit API context using a queue.
    /// This class ensures that all actions are executed synchronously from the caller's perspective.
    /// </summary>
    public class ExternalEventManager
    {
        private static ExternalEventManager _instance;
        private ActionEventHandler _handler;
        private ExternalEvent _externalEvent;
        private bool _isInitialized = false;
        private ILogger _logger;
        // The queue for pending actions.
        private ConcurrentQueue<ActionWrapper> _actionQueue;

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
            _actionQueue = new ConcurrentQueue<ActionWrapper>();
            _handler = new ActionEventHandler(_actionQueue); // Pass the queue to the handler
            _externalEvent = ExternalEvent.Create(_handler);
            _isInitialized = true;
            _logger.Info("ExternalEventManager initialized.");
        }

        /// <summary>
        /// Queues an action to be executed in the Revit API context.
        /// This method is synchronous and will block until the action is completed.
        /// It is thread-safe.
        /// </summary>
        /// <param name="action">The action to execute. It takes a UIApplication and returns an object.</param>
        /// <returns>The result of the action.</returns>
        public object ExecuteAction(Func<UIApplication, object> action)
        {
            // The lock is removed to prevent deadlocks and improve performance.
            // Concurrency is now handled by the ConcurrentQueue.
            if (!_isInitialized)
            {
                _logger.Error("ExternalEventManager is not initialized.");
                throw new InvalidOperationException("ExternalEventManager is not initialized.");
            }

            var wrapper = new ActionWrapper
            {
                Action = action,
                Tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously)
            };

            _actionQueue.Enqueue(wrapper);
            _externalEvent.Raise();

            // This will block the current thread until the action is executed
            // and the TaskCompletionSource is set.
            return wrapper.Tcs.Task.Result;
        }

        public void Dispose()
        {
            if (!_isInitialized) return;

            _externalEvent?.Dispose();
            _handler = null;
            _externalEvent = null;
            // Clear the queue on dispose
            while (_actionQueue.TryDequeue(out var wrapper))
            {
                wrapper.Tcs.TrySetCanceled();
            }
            _isInitialized = false;
            _logger.Info("ExternalEventManager disposed.");
        }
    }
}
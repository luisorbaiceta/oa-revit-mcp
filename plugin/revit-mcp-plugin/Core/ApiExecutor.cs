using Autodesk.Revit.UI;
using System;
using System.Threading.Tasks;

namespace revit_mcp_plugin.Core
{
    /// <summary>
    /// A wrapper to hold the action to be executed and its completion source.
    /// This is kept in this file to reduce file count and simplify.
    /// </summary>
    public class ActionWrapper
    {
        public Func<UIApplication, object> Action { get; set; }
        public TaskCompletionSource<object> Tcs { get; set; }
    }

    /// <summary>
    /// A singleton class to execute actions in the Revit UI context.
    /// It must be initialized at startup in a valid Revit API context.
    /// </summary>
    public class ApiExecutor : IExternalEventHandler
    {
        private static ApiExecutor _instance;
        private ExternalEvent _externalEvent;
        private ActionWrapper _action;
        private readonly object _lock = new object();

        // Private constructor to ensure singleton pattern.
        private ApiExecutor() { }

        /// <summary>
        /// Gets the singleton instance of the ApiExecutor.
        /// </summary>
        public static ApiExecutor Instance => _instance ?? (_instance = new ApiExecutor());

        /// <summary>
        /// Initializes the ApiExecutor and creates the ExternalEvent.
        /// This MUST be called from a valid Revit API context, such as OnStartup.
        /// </summary>
        public void Initialize()
        {
            if (_externalEvent == null)
            {
                // 'this' is the handler because this class implements IExternalEventHandler.
                _externalEvent = ExternalEvent.Create(this);
            }
        }

        /// <summary>
        /// Schedules an action to be executed in the Revit UI context and waits for it to complete.
        /// </summary>
        public object ExecuteAction(Func<UIApplication, object> actionToExecute)
        {
            var wrapper = new ActionWrapper
            {
                Action = actionToExecute,
                Tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously)
            };

            lock (_lock)
            {
                if (_action != null)
                {
                    throw new InvalidOperationException("Cannot execute action: Another action is already pending.");
                }
                _action = wrapper;
                _externalEvent.Raise();
            }

            return wrapper.Tcs.Task.Result;
        }

        /// <summary>
        /// The method that Revit calls in the UI context when the ExternalEvent is raised.
        /// </summary>
        public void Execute(UIApplication app)
        {
            ActionWrapper currentAction;
            lock (_lock)
            {
                currentAction = _action;
                _action = null;
            }

            if (currentAction != null)
            {
                try
                {
                    var result = currentAction.Action(app);
                    currentAction.Tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    currentAction.Tcs.SetException(ex.InnerException ?? ex);
                }
            }
        }

        public string GetName() => "ApiExecutor";

        /// <summary>
        /// Disposes the external event and the singleton instance.
        /// </summary>
        public void Dispose()
        {
            _externalEvent?.Dispose();
            _instance = null;
        }
    }
}

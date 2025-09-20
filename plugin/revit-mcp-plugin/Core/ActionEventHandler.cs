using Autodesk.Revit.UI;
using System;
using System.Threading.Tasks;

namespace revit_mcp_plugin.Core
{
    /// <summary>
    /// A wrapper to hold the action to be executed and its completion source.
    /// </summary>
    public class ActionWrapper
    {
        public Func<UIApplication, object> Action { get; set; }
        public TaskCompletionSource<object> Tcs { get; set; }
    }

    /// <summary>
    /// An external event handler that executes a single action.
    /// Concurrency and thread safety are handled by the caller.
    /// </summary>
    public class ActionEventHandler : IExternalEventHandler
    {
        private ActionWrapper _action;

        public void SetAction(ActionWrapper action)
        {
            _action = action;
        }

        public void Execute(UIApplication app)
        {
            if (_action == null) return;

            var currentAction = _action;
            _action = null;

            try
            {
                var result = currentAction.Action(app);
                currentAction.Tcs.SetResult(result);
            }
            catch (Exception ex)
            {
                // The exception might be from the Task framework if the TCS was faulted.
                // We should check for inner exceptions.
                var actualEx = ex.InnerException ?? ex;
                currentAction.Tcs.SetException(actualEx);
            }
        }

        public string GetName()
        {
            return "ActionEventHandler";
        }
    }
}

using Autodesk.Revit.UI;
using System;
using System.Threading.Tasks;

namespace revit_mcp_plugin.Core
{
    public class ActionWrapper
    {
        public Func<UIApplication, object> Action { get; set; }
        public TaskCompletionSource<object> Tcs { get; set; }
    }

    public class ApiExecutor : IExternalEventHandler
    {
        private static ApiExecutor _instance;
        private ExternalEvent _externalEvent;
        private ActionWrapper _action;
        private readonly object _lock = new object();

        private ApiExecutor() { }

        public static ApiExecutor Instance => _instance ?? (_instance = new ApiExecutor());

        public void Initialize()
        {
            if (_externalEvent == null)
            {
                _externalEvent = ExternalEvent.Create(this);
            }
        }

        public async Task<object> ExecuteActionAsync(Func<UIApplication, object> actionToExecute)
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

            return await wrapper.Tcs.Task;
        }

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

        public void Dispose()
        {
            _externalEvent?.Dispose();
            _instance = null;
        }
    }
}

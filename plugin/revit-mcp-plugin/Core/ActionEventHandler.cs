using Autodesk.Revit.UI;
using System;
using System.Collections.Concurrent;
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
    /// An external event handler that executes actions from a queue.
    /// </summary>
    public class ActionEventHandler : IExternalEventHandler
    {
        private readonly ConcurrentQueue<ActionWrapper> _queue;

        public ActionEventHandler(ConcurrentQueue<ActionWrapper> queue)
        {
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        }

        public void Execute(UIApplication app)
        {
            if (_queue.TryDequeue(out ActionWrapper wrapper))
            {
                try
                {
                    var result = wrapper.Action(app);
                    wrapper.Tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    wrapper.Tcs.SetException(ex);
                }
            }
        }

        public string GetName()
        {
            return "ActionEventHandler";
        }
    }
}

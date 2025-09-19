using Autodesk.Revit.UI;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace revit_mcp_plugin.Core
{
    /// <summary>
    /// A wrapper to hold the action to be executed and its completion source.
    /// </summary>
    internal class ActionWrapper
    {
        public Func<UIApplication, object> Action { get; set; }
        public TaskCompletionSource<object> Tcs { get; set; }
    }

    /// <summary>
    /// An external event handler that can execute arbitrary actions
    /// queued from any thread.
    /// </summary>
    public class ActionEventHandler : IExternalEventHandler
    {
        private readonly ConcurrentQueue<ActionWrapper> _queue = new ConcurrentQueue<ActionWrapper>();

        public void EnqueueAction(ActionWrapper wrapper)
        {
            _queue.Enqueue(wrapper);
        }

        public void Execute(UIApplication app)
        {
            while (_queue.TryDequeue(out var wrapper))
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

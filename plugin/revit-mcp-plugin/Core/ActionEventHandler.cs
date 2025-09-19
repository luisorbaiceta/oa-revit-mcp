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
    /// queued from any thread. This handler implements a self-raising
    /// mechanism to ensure all queued actions are processed.
    /// </summary>
    public class ActionEventHandler : IExternalEventHandler
    {
        private readonly ConcurrentQueue<ActionWrapper> _queue = new ConcurrentQueue<ActionWrapper>();
        private ExternalEvent _externalEvent;

        /// <summary>
        /// Sets the external event that this handler is associated with.
        /// This is used to re-raise the event if the queue is not empty.
        /// </summary>
        public void SetExternalEvent(ExternalEvent exEvent)
        {
            _externalEvent = exEvent;
        }

        public void EnqueueAction(ActionWrapper wrapper)
        {
            _queue.Enqueue(wrapper);
        }

        public void Execute(UIApplication app)
        {
            if (_queue.TryDequeue(out var wrapper))
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


            // If there are more items in the queue, raise the event again
            // to process the next item in the queue. This creates a chain
            // of events that continues until the queue is empty.
            if (!_queue.IsEmpty && _externalEvent != null)
            {
                _externalEvent.Raise();
            }
        }

        public string GetName()
        {
            return "ActionEventHandler";
        }
    }
}

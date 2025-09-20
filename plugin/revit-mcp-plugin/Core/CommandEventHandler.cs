using Autodesk.Revit.UI;
using System;
using System.Threading.Tasks;

namespace revit_mcp_plugin.Core
{
    /// <summary>
    /// An IExternalEventHandler implementation designed for a single, one-time execution.
    /// It is created for a specific action and signals its completion via a TaskCompletionSource.
    /// This follows the transient handler pattern suggested by Autodesk's API documentation.
    /// </summary>
    public class CommandEventHandler : IExternalEventHandler
    {
        private readonly Func<UIApplication, object> _action;
        private readonly TaskCompletionSource<object> _tcs;

        public CommandEventHandler(Func<UIApplication, object> action, TaskCompletionSource<object> tcs)
        {
            _action = action;
            _tcs = tcs;
        }

        /// <summary>
        /// This method is executed by Revit in the main UI context.
        /// </summary>
        public void Execute(UIApplication app)
        {
            try
            {
                var result = _action(app);
                _tcs.TrySetResult(result);
            }
            catch (Exception ex)
            {
                _tcs.TrySetException(ex);
            }
        }

        public string GetName() => "One-Time Command Event Handler";
    }
}

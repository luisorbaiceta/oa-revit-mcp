using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitMCPCommandSet.Services.Generated
{
    public class DeleteElementEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        public object Result { get; private set; }
        public bool TaskCompleted { get; private set; }
        private readonly System.Threading.ManualResetEvent _resetEvent = new System.Threading.ManualResetEvent(false);

        private string[] _elementIds;

        public void SetParameters(JObject parameters)
        {
            _elementIds = parameters["elementIds"]?.ToObject<string[]>();
        }

        public void Execute(UIApplication app)
        {
            try
            {
                var doc = app.ActiveUIDocument.Document;
                int deletedCount = 0;
                if (_elementIds == null || _elementIds.Length == 0)
                {
                    Result = new { IsSuccess = false, DeletedCount = 0, Message = "No element ids provided." };
                    return;
                }

                List<ElementId> elementIdsToDelete = new List<ElementId>();
                List<string> invalidIds = new List<string>();
                foreach (var idStr in _elementIds)
                {
                    if (long.TryParse(idStr, out long elementIdValue))
                    {
                        var elementId = new ElementId(elementIdValue);
                        if (doc.GetElement(elementId) != null)
                        {
                            elementIdsToDelete.Add(elementId);
                        }
                    }
                    else
                    {
                        invalidIds.Add(idStr);
                    }
                }

                if (invalidIds.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Invalid or non-existent element ids: {string.Join(", ", invalidIds)}");
                }

                if (elementIdsToDelete.Count > 0)
                {
                    using (var transaction = new Transaction(doc, "Delete Elements"))
                    {
                        transaction.Start();
                        ICollection<ElementId> deletedIds = doc.Delete(elementIdsToDelete);
                        deletedCount = deletedIds.Count;
                        transaction.Commit();
                    }
                    Result = new { IsSuccess = true, DeletedCount = deletedCount };
                }
                else
                {
                    Result = new { IsSuccess = false, DeletedCount = 0, Message = "No valid elements to delete." };
                }
            }
            catch (Exception ex)
            {
                Result = new { IsSuccess = false, DeletedCount = 0, Message = ex.Message };
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
            finally
            {
                TaskCompleted = true;
                _resetEvent.Set();
            }
        }

        public string GetName()
        {
            return "delete_element";
        }

        public bool WaitForCompletion(int timeoutMilliseconds = 15000)
        {
            return _resetEvent.WaitOne(timeoutMilliseconds);
        }
    }
}

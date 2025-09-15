//
//                       RevitAPI-Solutions
// Copyright (c) Duong Tran Quang (DTDucas) (baymax.contact@gmail.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPCommandSet.Models.Views;
using RevitMCPSDK.API.Interfaces;
using System.Threading;

namespace RevitMCPCommandSet.Services;

public class CreateDraftingViewEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
{
    private readonly ManualResetEvent _resetEvent = new(false);
    public ViewDraftingCreationInfo CreationInfo { get; set; }
    public AIResult<ViewDraftingInfo> Result { get; private set; }

    public void Execute(UIApplication app)
    {
        var doc = app.ActiveUIDocument.Document;
        try
        {
            using var transaction = new Transaction(doc, "Create Drafting View");
            transaction.Start();

            var viewFamilyTypeId = new ElementId(CreationInfo.ViewFamilyTypeId);
            var viewDrafting = ViewDrafting.Create(doc, viewFamilyTypeId);

            transaction.Commit();

            Result = new AIResult<ViewDraftingInfo>
            {
                Success = true,
                Message = "Drafting view created successfully.",
                Response = new ViewDraftingInfo
                {
                    Id = viewDrafting.Id.IntegerValue,
                    UniqueId = viewDrafting.UniqueId,
                    Name = viewDrafting.Name,
                    ViewType = viewDrafting.ViewType.ToString()
                }
            };
        }
        catch (System.Exception e)
        {
            Result = new AIResult<ViewDraftingInfo>
            {
                Success = false,
                Message = e.Message
            };
        }
        finally
        {
            _resetEvent.Set();
        }
    }

    public string GetName()
    {
        return "CreateDraftingViewEventHandler";
    }

    public bool WaitForCompletion(int timeoutMilliseconds = 10000)
    {
        return _resetEvent.WaitOne(timeoutMilliseconds);
    }
}

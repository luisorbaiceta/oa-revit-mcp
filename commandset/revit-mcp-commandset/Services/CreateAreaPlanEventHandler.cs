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
using RevitMCPCommandSet.Models.Common;
using RevitMCPSDK.API.Interfaces;
using System.Threading;

namespace RevitMCPCommandSet.Services;

public class CreateAreaPlanEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
{
    private readonly ManualResetEvent _resetEvent = new(false);
    public AreaPlanCreationInfo CreationInfo { get; set; }
    public AIResult<ViewPlanInfo> Result { get; private set; }

    public void Execute(UIApplication app)
    {
        var doc = app.ActiveUIDocument.Document;
        try
        {
            using var transaction = new Transaction(doc, "Create Area Plan");
            transaction.Start();

            var areaSchemeId = new ElementId(CreationInfo.AreaSchemeId);
            var levelId = new ElementId(CreationInfo.LevelId);
            var viewPlan = ViewPlan.CreateAreaPlan(doc, areaSchemeId, levelId);

            transaction.Commit();

            Result = new AIResult<ViewPlanInfo>
            {
                Success = true,
                Message = "Area plan created successfully.",
                Response = new ViewPlanInfo
                {
                    Id = viewPlan.Id.IntegerValue,
                    UniqueId = viewPlan.UniqueId,
                    Name = viewPlan.Name,
                    ViewType = viewPlan.ViewType.ToString()
                }
            };
        }
        catch (System.Exception e)
        {
            Result = new AIResult<ViewPlanInfo>
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
        return "CreateAreaPlanEventHandler";
    }

    public bool WaitForCompletion(int timeoutMilliseconds = 10000)
    {
        return _resetEvent.WaitOne(timeoutMilliseconds);
    }
}

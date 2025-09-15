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
using RevitMCPCommandSet.Models.Common;

namespace RevitMCPCommandSet.Services;

public class CreateSectionViewEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
{
    private readonly ManualResetEvent _resetEvent = new(false);
    public SectionCreationInfo CreationInfo { get; set; }
    public AIResult<ViewSectionInfo> Result { get; private set; }

    public void Execute(UIApplication app)
    {
        var doc = app.ActiveUIDocument.Document;
        try
        {
            using var transaction = new Transaction(doc, "Create Section View");
            transaction.Start();

            var viewFamilyTypeId = new ElementId(CreationInfo.ViewFamilyTypeId);
            var sectionBox = new BoundingBoxXYZ
            {
                Min = new XYZ(CreationInfo.SectionBox.Min.X, CreationInfo.SectionBox.Min.Y, CreationInfo.SectionBox.Min.Z),
                Max = new XYZ(CreationInfo.SectionBox.Max.X, CreationInfo.SectionBox.Max.Y, CreationInfo.SectionBox.Max.Z),
                Transform = new Transform(Transform.Identity)
                {
                    Origin = new XYZ(CreationInfo.SectionBox.Transform.Origin.X, CreationInfo.SectionBox.Transform.Origin.Y, CreationInfo.SectionBox.Transform.Origin.Z),
                    BasisX = new XYZ(CreationInfo.SectionBox.Transform.BasisX.X, CreationInfo.SectionBox.Transform.BasisX.Y, CreationInfo.SectionBox.Transform.BasisX.Z),
                    BasisY = new XYZ(CreationInfo.SectionBox.Transform.BasisY.X, CreationInfo.SectionBox.Transform.BasisY.Y, CreationInfo.SectionBox.Transform.BasisY.Z),
                    BasisZ = new XYZ(CreationInfo.SectionBox.Transform.BasisZ.X, CreationInfo.SectionBox.Transform.BasisZ.Y, CreationInfo.SectionBox.Transform.BasisZ.Z)
                }
            };

            var viewSection = ViewSection.CreateSection(doc, viewFamilyTypeId, sectionBox);

            transaction.Commit();

            Result = new AIResult<ViewSectionInfo>
            {
                Success = true,
                Message = "Section view created successfully.",
                Response = new ViewSectionInfo
                {
                    Id = viewSection.Id.IntegerValue,
                    UniqueId = viewSection.UniqueId,
                    Name = viewSection.Name,
                    ViewType = viewSection.ViewType.ToString()
                }
            };
        }
        catch (System.Exception e)
        {
            Result = new AIResult<ViewSectionInfo>
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
        return "CreateSectionViewEventHandler";
    }

    public bool WaitForCompletion(int timeoutMilliseconds = 10000)
    {
        return _resetEvent.WaitOne(timeoutMilliseconds);
    }
}

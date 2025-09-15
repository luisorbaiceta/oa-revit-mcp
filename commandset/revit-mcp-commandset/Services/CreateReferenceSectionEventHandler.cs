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

public class CreateReferenceSectionEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
{
    private readonly ManualResetEvent _resetEvent = new(false);
    public ReferenceSectionCreationInfo CreationInfo { get; set; }
    public AIResult<ViewSectionInfo> Result { get; private set; }

    public void Execute(UIApplication app)
    {
        var doc = app.ActiveUIDocument.Document;
        try
        {
            using var transaction = new Transaction(doc, "Create Reference Section");
            transaction.Start();

            var parentViewId = new ElementId(CreationInfo.ParentViewId);
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

            // Define the headPoint and tailPoint for the section
            var headPoint = new XYZ(CreationInfo.HeadPoint.X, CreationInfo.HeadPoint.Y, CreationInfo.HeadPoint.Z);
            var tailPoint = new XYZ(CreationInfo.TailPoint.X, CreationInfo.TailPoint.Y, CreationInfo.TailPoint.Z);

            // Call the correct overload of CreateReferenceSection
            ViewSection.CreateReferenceSection(doc, parentViewId, viewFamilyTypeId, headPoint, tailPoint);

            transaction.Commit();

            Result = new AIResult<ViewSectionInfo>
            {
                Success = true,
                Message = "Reference section created successfully.",
                Response = new ViewSectionInfo
                {
                    Id = parentViewId.IntegerValue,
                    UniqueId = parentViewId.ToString(),
                    Name = "Reference Section",
                    ViewType = "Section"
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
        return "CreateReferenceSectionEventHandler";
    }

    public bool WaitForCompletion(int timeoutMilliseconds = 10000)
    {
        return _resetEvent.WaitOne(timeoutMilliseconds);
    }
}
public class ReferenceSectionCreationInfo
{
    public long ParentViewId { get; set; }
    public long ViewFamilyTypeId { get; set; }
    public BoundingBoxXYZInfo SectionBox { get; set; }

    // Add the missing properties
    public XYZ HeadPoint { get; set; } // Represents the head point of the section
    public XYZ TailPoint { get; set; } // Represents the tail point of the section
}

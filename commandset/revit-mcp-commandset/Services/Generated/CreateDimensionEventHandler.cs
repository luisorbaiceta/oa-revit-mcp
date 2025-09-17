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

using Autodesk.Revit.UI;
using RevitMCPCommandSet.Models.Annotation;
using RevitMCPCommandSet.Models.Common;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Generated;

using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Utils;
using System.Threading.Tasks;

public class CreateDimensionEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
{
    public object Result { get; private set; }
    public bool TaskCompleted { get; private set; }
    private readonly System.Threading.ManualResetEvent _resetEvent = new System.Threading.ManualResetEvent(false);

    private List<DimensionCreationInfo> _dimensionsToCreate;

    public void SetParameters(JObject parameters)
    {
        _dimensionsToCreate = parameters["dimensions"]?.ToObject<List<DimensionCreationInfo>>();
    }

    public void Execute(UIApplication app)
    {
        try
        {
            var doc = app.ActiveUIDocument.Document;
            var createdDimensionIds = new List<long>();

            foreach (var dimInfo in _dimensionsToCreate)
            {
                View view = dimInfo.ViewId > 0 ? doc.GetElement(new ElementId(dimInfo.ViewId)) as View : doc.ActiveView;
                if (view == null) continue;

                using (var transaction = new Transaction(doc, "Create Dimension"))
                {
                    transaction.Start();
                    try
                    {
                        var startPoint = DimensionUtils.ConvertToInternalCoordinates(dimInfo.StartPoint.X, dimInfo.StartPoint.Y, dimInfo.StartPoint.Z);
                        var endPoint = DimensionUtils.ConvertToInternalCoordinates(dimInfo.EndPoint.X, dimInfo.EndPoint.Y, dimInfo.EndPoint.Z);
                        var linePoint = dimInfo.LinePoint != null ? DimensionUtils.ConvertToInternalCoordinates(dimInfo.LinePoint.X, dimInfo.LinePoint.Y, dimInfo.LinePoint.Z) : new XYZ((startPoint.X + endPoint.X) / 2, (startPoint.Y + endPoint.Y) / 2 + 1.0, (startPoint.Z + endPoint.Z) / 2);

                        Dimension dimension = null;
                        if (dimInfo.ElementIds != null && dimInfo.ElementIds.Count > 0)
                        {
                            var references = new ReferenceArray();
                            foreach (var elementId in dimInfo.ElementIds)
                            {
                                var element = doc.GetElement(new ElementId(elementId));
                                if (element != null)
                                {
                                    foreach (var reference in DimensionUtils.GetReferences(element, view))
                                    {
                                        references.Append(reference);
                                    }
                                }
                            }
                            if (references.Size >= 2)
                            {
                                var line = Line.CreateBound(startPoint, endPoint);
                                dimension = doc.Create.NewDimension(view, line, references);
                            }
                        }
                        else
                        {
                            var line = Line.CreateBound(startPoint, endPoint);
                            var refArray = new ReferenceArray();
                            var startRef = DimensionUtils.FindReferenceAtPoint(doc, view, startPoint);
                            var endRef = DimensionUtils.FindReferenceAtPoint(doc, view, endPoint);
                            if (startRef != null && endRef != null)
                            {
                                refArray.Append(startRef);
                                refArray.Append(endRef);
                                dimension = doc.Create.NewDimension(view, line, refArray);
                            }
                        }

                        if (dimension != null)
                        {
                            if (dimInfo.DimensionStyleId > 0)
                            {
                                if (doc.GetElement(new ElementId(dimInfo.DimensionStyleId)) is DimensionType dimensionType)
                                {
                                    dimension.DimensionType = dimensionType;
                                }
                            }
                            DimensionUtils.ApplyDimensionParameters(dimension, dimInfo);
                            createdDimensionIds.Add(dimension.Id.Value);
                        }
                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.RollBack();
                        throw;
                    }
                }
            }

            Result = new AIResult<List<long>>
            {
                Success = true,
                Message = $"Successfully created {createdDimensionIds.Count} dimensions.",
                Response = createdDimensionIds
            };
        }
        catch (Exception ex)
        {
            Result = new AIResult<List<long>>
            {
                Success = false,
                Message = $"Error creating dimensions: {ex.Message}",
                Response = new List<long>()
            };
        }
        finally
        {
            TaskCompleted = true;
            _resetEvent.Set();
        }
    }

    public bool WaitForCompletion(int timeoutMilliseconds = 15000)
    {
        return _resetEvent.WaitOne(timeoutMilliseconds);
    }

    public string GetName()
    {
        return "create_dimensions";
    }
}
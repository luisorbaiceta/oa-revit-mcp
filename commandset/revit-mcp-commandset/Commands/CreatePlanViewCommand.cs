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
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;
using Newtonsoft.Json.Linq;
using System;
using RevitMCPCommandSet.Models.Views;

namespace RevitMCPCommandSet.Commands;

public class CreatePlanViewCommand : ExternalEventCommandBase
{
    private CreatePlanViewEventHandler _handler => (CreatePlanViewEventHandler)Handler;

    public override string CommandName => "create_plan_view";

    public CreatePlanViewCommand(UIApplication uiApp)
        : base(new CreatePlanViewEventHandler(), uiApp)
    {
    }

    public override object Execute(JObject parameters, string requestId)
    {
        try
        {
            var creationInfo = parameters.ToObject<ViewPlanCreationInfo>();
            _handler.CreationInfo = creationInfo;

            if (RaiseAndWaitForCompletion(10000))
            {
                return _handler.Result;
            }
            else
            {
                throw new TimeoutException("Create Plan View operation timed out.");
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to create plan view: {ex.Message}");
        }
    }
}

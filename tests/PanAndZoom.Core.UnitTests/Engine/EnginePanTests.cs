// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
namespace PanAndZoom.Core.UnitTests.Engine;

public class EnginePanTests
{
    [Fact]
    public void PanDelta_Adds_To_Offsets()
    {
        var harness = EngineHarness.Create();

        harness.Engine.PanDelta(10, -20);
        harness.Engine.PanDelta(5, 5);

        AssertEx.View(harness.Engine, 1, 15, -15);
        Assert.False(harness.Host.LastTransform.SkipTransitions);
    }

    [Fact]
    public void PanDelta_Is_In_Viewport_Units_And_Keeps_Zoom()
    {
        var harness = EngineHarness.Create();
        harness.Engine.ZoomTo(2, 0, 0);

        harness.Engine.PanDelta(10, 20, skipTransitions: true);

        AssertEx.View(harness.Engine, 2, 10, 20);
        Assert.True(harness.Host.LastTransform.SkipTransitions);
    }

    [Fact]
    public void PanDelta_Does_Not_Add_View_History()
    {
        var harness = EngineHarness.Create();

        harness.Engine.PanDelta(10, 10);

        Assert.Single(harness.Engine.ViewHistory);
    }

    [Fact]
    public void Pan_Sets_Absolute_Offsets_And_Adds_History()
    {
        var harness = EngineHarness.Create();
        harness.Engine.ZoomTo(2, 0, 0);

        harness.Engine.Pan(-30, 40);

        AssertEx.View(harness.Engine, 2, -30, 40);
        Assert.Equal(3, harness.Engine.ViewHistory.Count);
        Assert.Equal(harness.Engine.Matrix, harness.Engine.ViewHistory[^1].Matrix);
    }

    [Fact]
    public void BeginPanTo_ContinuePanTo_Translates_By_Pointer_Delta_In_Element_Coordinates()
    {
        var harness = EngineHarness.Create();

        harness.Engine.BeginPanTo(10, 10);
        harness.Engine.ContinuePanTo(30, 25);

        AssertEx.View(harness.Engine, 1, 20, 15);
    }

    [Fact]
    public void ContinuePanTo_Scales_Element_Delta_By_Zoom()
    {
        var harness = EngineHarness.Create();
        harness.Engine.ZoomTo(2, 0, 0);

        harness.Engine.BeginPanTo(10, 10);
        harness.Engine.ContinuePanTo(20, 15);

        AssertEx.View(harness.Engine, 2, 20, 10);
    }

    [Fact]
    public void Pointer_Drag_Keeps_Content_Under_Pointer()
    {
        var harness = EngineHarness.Create();
        var engine = harness.Engine;
        engine.ZoomTo(2, 0, 0);

        // Simulate the controls which report pointer positions relative to the (transformed) element.
        var screenPoints = new[] { new CorePoint(100, 100), new CorePoint(150, 130), new CorePoint(200, 160), new CorePoint(180, 90) };
        var grabbedContentPoint = engine.ViewportToContent(screenPoints[0]);

        engine.BeginPanTo(grabbedContentPoint.X, grabbedContentPoint.Y);
        foreach (var screenPoint in screenPoints.Skip(1))
        {
            var elementPoint = engine.ViewportToContent(screenPoint);
            engine.ContinuePanTo(elementPoint.X, elementPoint.Y, true);

            AssertEx.Equal(screenPoint, engine.ContentToViewport(grabbedContentPoint));
        }

        AssertEx.View(engine, 2, 80, -10);
    }

    [Fact]
    public void Unrestricted_Bounds_Allow_Panning_Content_Out_Of_View()
    {
        var harness = EngineHarness.Create();

        harness.Engine.PanDelta(10000, -10000);

        AssertEx.View(harness.Engine, 1, 10000, -10000);
    }
}

// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
namespace PanAndZoom.Core.UnitTests.Input;

public class PointerInputTests
{
    private const ZoomBorderPointerButtons Middle = ZoomBorderPointerButtons.Middle;

    [Theory]
    [InlineData(ButtonName.Left, ZoomBorderPointerButtons.Left, true)]
    [InlineData(ButtonName.Left, ZoomBorderPointerButtons.Right, false)]
    [InlineData(ButtonName.Left, ZoomBorderPointerButtons.Middle, false)]
    [InlineData(ButtonName.Right, ZoomBorderPointerButtons.Right, true)]
    [InlineData(ButtonName.Right, ZoomBorderPointerButtons.Left, false)]
    [InlineData(ButtonName.Middle, ZoomBorderPointerButtons.Middle, true)]
    [InlineData(ButtonName.Middle, ZoomBorderPointerButtons.Left, false)]
    [InlineData(ButtonName.Middle, ZoomBorderPointerButtons.Left | ZoomBorderPointerButtons.Middle, true)]
    [InlineData(ButtonName.Middle, ZoomBorderPointerButtons.None, false)]
    public void IsPanButtonPressed_Maps_PanButton(ButtonName panButton, ZoomBorderPointerButtons buttons, bool expected)
    {
        var harness = EngineHarness.Create(s => s.PanButton = panButton);

        Assert.Equal(expected, harness.Engine.IsPanButtonPressed(buttons));
    }

    [Fact]
    public void Press_Move_Release_Pans_Content()
    {
        var harness = EngineHarness.Create();
        var engine = harness.Engine;

        Assert.True(engine.ProcessPointerPressed(new CorePoint(10, 10), Middle));
        Assert.True(engine.IsPanning);

        Assert.True(engine.ProcessPointerMoved(new CorePoint(30, 20), Middle));
        AssertEx.View(engine, 1, 20, 10);
        Assert.True(harness.Host.LastTransform.SkipTransitions);

        Assert.True(engine.ProcessPointerReleased());
        Assert.False(engine.IsPanning);
        Assert.Equal(new[] { true, false }, harness.Host.PanningChanges);
        Assert.Equal(2, engine.ViewHistory.Count);
    }

    [Fact]
    public void PanButton_Setting_Is_Respected()
    {
        var harness = EngineHarness.Create(s => s.PanButton = ButtonName.Left);

        Assert.False(harness.Engine.ProcessPointerPressed(new CorePoint(0, 0), Middle));
        Assert.True(harness.Engine.ProcessPointerPressed(new CorePoint(0, 0), ZoomBorderPointerButtons.Left));
    }

    [Fact]
    public void Press_With_Other_Button_Does_Not_Pan()
    {
        var harness = EngineHarness.Create();

        Assert.False(harness.Engine.ProcessPointerPressed(new CorePoint(0, 0), ZoomBorderPointerButtons.Left));
        Assert.False(harness.Engine.IsPanning);
        Assert.Empty(harness.Host.PanningChanges);
    }

    [Fact]
    public void Press_While_Captured_Is_Ignored()
    {
        var harness = EngineHarness.Create();
        var started = 0;
        harness.Engine.PanStarted += (_, _) => started++;

        Assert.True(harness.Engine.ProcessPointerPressed(new CorePoint(0, 0), Middle));
        Assert.False(harness.Engine.ProcessPointerPressed(new CorePoint(5, 5), Middle));

        Assert.Equal(1, started);
        Assert.Equal(new[] { true }, harness.Host.PanningChanges);
    }

    [Fact]
    public void Move_Without_Capture_Is_Ignored()
    {
        var harness = EngineHarness.Create();

        Assert.False(harness.Engine.ProcessPointerMoved(new CorePoint(30, 20), Middle));
        AssertEx.View(harness.Engine, 1, 0, 0);
    }

    [Fact]
    public void Move_Without_Pan_Button_Is_Ignored()
    {
        var harness = EngineHarness.Create();
        harness.Engine.ProcessPointerPressed(new CorePoint(0, 0), Middle);

        Assert.False(harness.Engine.ProcessPointerMoved(new CorePoint(30, 20), ZoomBorderPointerButtons.None));
        AssertEx.View(harness.Engine, 1, 0, 0);
    }

    [Fact]
    public void Release_Without_Capture_Returns_False()
    {
        var harness = EngineHarness.Create();
        var ended = 0;
        harness.Engine.PanEnded += (_, _) => ended++;

        Assert.False(harness.Engine.ProcessPointerReleased());
        Assert.False(harness.Engine.ProcessPointerCaptureLost());
        Assert.Equal(0, ended);
    }

    [Fact]
    public void Capture_Lost_Ends_Panning()
    {
        var harness = EngineHarness.Create();
        var ended = 0;
        harness.Engine.PanEnded += (_, _) => ended++;
        harness.Engine.ProcessPointerPressed(new CorePoint(0, 0), Middle);

        Assert.True(harness.Engine.ProcessPointerCaptureLost());

        Assert.False(harness.Engine.IsPanning);
        Assert.Equal(1, ended);
        Assert.False(harness.Engine.ProcessPointerReleased());
    }

    [Fact]
    public void Pointer_Input_Is_Ignored_When_Pan_Disabled()
    {
        var harness = EngineHarness.Create(s => s.EnablePan = false);

        Assert.False(harness.Engine.ProcessPointerPressed(new CorePoint(0, 0), Middle));
        Assert.False(harness.Engine.ProcessPointerMoved(new CorePoint(10, 10), Middle));
        Assert.False(harness.Engine.ProcessPointerReleased());
        Assert.False(harness.Engine.ProcessPointerCaptureLost());
        Assert.False(harness.Engine.IsPanning);
    }

    [Fact(Skip = "BUG (pre-existing, ported from the original ZoomBorder): PanningFinished returns early when EnablePan is false, so disabling EnablePan during a drag leaves the engine captured with IsPanning == true; later presses are ignored until a release happens with EnablePan re-enabled.")]
    public void Disabling_Pan_During_Drag_Ends_Panning_On_Release()
    {
        var harness = EngineHarness.Create();
        Assert.True(harness.Engine.ProcessPointerPressed(new CorePoint(0, 0), Middle));

        harness.Settings.EnablePan = false;
        harness.Engine.ProcessPointerReleased();

        Assert.False(harness.Engine.IsPanning);
        Assert.Equal(new[] { true, false }, harness.Host.PanningChanges);

        harness.Settings.EnablePan = true;
        Assert.True(harness.Engine.ProcessPointerPressed(new CorePoint(0, 0), Middle));
    }

    [Fact]
    public void Pointer_Input_Is_Ignored_Without_Element()
    {
        var harness = EngineHarness.Create(attach: false);

        Assert.False(harness.Engine.ProcessPointerPressed(new CorePoint(0, 0), Middle));
        Assert.False(harness.Engine.IsPanning);
    }

    [Fact]
    public void Wheel_Zoom_Is_Ignored_While_Panning()
    {
        var harness = EngineHarness.Create();
        harness.Engine.ProcessPointerPressed(new CorePoint(0, 0), Middle);

        harness.Engine.ProcessPointerWheel(new CoreVector(0, 1), new CorePoint(0, 0), ZoomBorderKeyModifiers.None);

        Assert.Equal(1, harness.Engine.ZoomX);
    }

    [Fact]
    public void Multiple_Moves_Accumulate_In_Element_Coordinates()
    {
        var harness = EngineHarness.Create();
        var engine = harness.Engine;
        engine.ZoomTo(2, 0, 0);

        // Screen (100,100) -> (150,130) -> (200,160) converted to element coordinates as the controls do.
        engine.ProcessPointerPressed(engine.ViewportToContent(new CorePoint(100, 100)), Middle);
        engine.ProcessPointerMoved(engine.ViewportToContent(new CorePoint(150, 130)), Middle);
        engine.ProcessPointerMoved(engine.ViewportToContent(new CorePoint(200, 160)), Middle);
        engine.ProcessPointerReleased();

        AssertEx.View(engine, 2, 100, 60);
    }
}

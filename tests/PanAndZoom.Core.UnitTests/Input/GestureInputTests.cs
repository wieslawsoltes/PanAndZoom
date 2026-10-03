// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using System.Threading;

namespace PanAndZoom.Core.UnitTests.Input;

public class PinchGestureTests
{
    private static readonly CorePoint s_origin = new(0, 0);

    [Fact]
    public void Pinch_Converts_Cumulative_Scale_To_Incremental_Zoom()
    {
        var harness = EngineHarness.Create();
        var engine = harness.Engine;

        Assert.True(engine.ProcessPinch(1.5, 0, s_origin));
        Assert.Equal(1.5, engine.ZoomX, 12);

        Assert.True(engine.ProcessPinch(2.0, 0, s_origin));
        Assert.Equal(2.0, engine.ZoomX, 12);

        Assert.True(engine.ProcessPinch(1.0, 0, s_origin));
        Assert.Equal(1.0, engine.ZoomX, 12);
    }

    [Fact]
    public void Pinch_Zooms_Around_Origin()
    {
        var harness = EngineHarness.Create();

        harness.Engine.ProcessPinch(2, 0, new CorePoint(100, 50));

        AssertEx.View(harness.Engine, 2, -100, -50);
        Assert.False(harness.Host.LastTransform.SkipTransitions);
    }

    [Fact]
    public void Pinch_Ended_Resets_Cumulative_Scale()
    {
        var harness = EngineHarness.Create();
        var engine = harness.Engine;

        engine.ProcessPinch(1.5, 0, s_origin);
        Assert.True(engine.ProcessPinchEnded());

        engine.ProcessPinch(1.5, 0, s_origin);

        Assert.Equal(2.25, engine.ZoomX, 12);
    }

    [Fact]
    public void Detach_From_Visual_Tree_Resets_Gesture_State()
    {
        var harness = EngineHarness.Create();
        var engine = harness.Engine;

        engine.ProcessPinch(1.5, 0, s_origin);
        engine.OnDetachedFromVisualTree();
        engine.ProcessPinch(1.5, 0, s_origin);

        Assert.Equal(2.25, engine.ZoomX, 12);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    public void Pinch_Ignores_Invalid_Scale(double scale)
    {
        var harness = EngineHarness.Create();

        Assert.True(harness.Engine.ProcessPinch(scale, 0, s_origin));

        Assert.Equal(1, harness.Engine.ZoomX);
    }

    [Fact]
    public void Pinch_Applies_Rotation()
    {
        var harness = EngineHarness.Create();

        harness.Engine.ProcessPinch(1, 10, s_origin);
        harness.Engine.ProcessPinch(1, 5, s_origin);

        Assert.Equal(15, harness.Settings.Rotation);
        Assert.True(harness.Host.LastTransform.SkipTransitions);
    }

    [Fact]
    public void Pinch_Ignores_Tiny_Rotation()
    {
        var harness = EngineHarness.Create();

        harness.Engine.ProcessPinch(1.2, 0.0005, s_origin);

        Assert.Equal(0, harness.Settings.Rotation);
    }

    [Fact]
    public void Pinch_Without_Gesture_Zoom_Only_Rotates()
    {
        var harness = EngineHarness.Create(s => s.EnableGestureZoom = false);

        Assert.True(harness.Engine.ProcessPinch(2, 10, s_origin));

        Assert.Equal(1, harness.Engine.ZoomX);
        Assert.Equal(10, harness.Settings.Rotation);
    }

    [Fact]
    public void Pinch_Without_Gesture_Rotation_Only_Zooms()
    {
        var harness = EngineHarness.Create(s => s.EnableGestureRotation = false);

        Assert.True(harness.Engine.ProcessPinch(2, 10, s_origin));

        Assert.Equal(2, harness.Engine.ZoomX);
        Assert.Equal(0, harness.Settings.Rotation);
    }

    [Fact]
    public void Pinch_Without_Zoom_And_Rotation_Is_Not_Handled()
    {
        var harness = EngineHarness.Create(s =>
        {
            s.EnableGestureZoom = false;
            s.EnableGestureRotation = false;
        });

        Assert.False(harness.Engine.ProcessPinch(2, 10, s_origin));
    }

    [Fact]
    public void Pinch_With_Gestures_Disabled_Is_Not_Handled()
    {
        var harness = EngineHarness.Create(s => s.EnableGestures = false);
        var ended = 0;
        harness.Engine.GestureEnded += (_, _) => ended++;

        Assert.False(harness.Engine.ProcessPinch(2, 0, s_origin));
        Assert.False(harness.Engine.ProcessPinchEnded());
        Assert.Equal(1, harness.Engine.ZoomX);
        Assert.Equal(0, ended);
        Assert.Single(harness.Engine.ViewHistory);
    }

    [Fact]
    public void Pinch_Without_Element_Is_Not_Handled()
    {
        var harness = EngineHarness.Create(attach: false);

        Assert.False(harness.Engine.ProcessPinch(2, 0, s_origin));
    }

    [Theory]
    [InlineData(3, 4, false)]
    [InlineData(1, 1, false)]
    [InlineData(2, 2, true)]
    [InlineData(1, 10, true)]
    public void Pinch_Respects_Touch_Point_Limits(int minimum, int maximum, bool expected)
    {
        var harness = EngineHarness.Create(s =>
        {
            s.MinimumTouchPoints = minimum;
            s.MaximumTouchPoints = maximum;
        });

        Assert.Equal(expected, harness.Engine.ProcessPinch(2, 0, s_origin));
        Assert.Equal(expected ? 2 : 1, harness.Engine.ZoomX);
    }

    [Fact]
    public void Pinch_Waits_For_Recognition_Delay()
    {
        var harness = EngineHarness.Create(s => s.GestureRecognitionDelay = TimeSpan.FromHours(1));

        Assert.False(harness.Engine.ProcessPinch(2, 0, s_origin));
        Assert.False(harness.Engine.ProcessPinch(2, 0, s_origin));

        Assert.Equal(1, harness.Engine.ZoomX);
    }

    [Fact]
    public void Pinch_Is_Recognized_After_Delay_Elapses()
    {
        var harness = EngineHarness.Create(s => s.GestureRecognitionDelay = TimeSpan.FromMilliseconds(20));

        Assert.False(harness.Engine.ProcessPinch(2, 0, s_origin));

        Thread.Sleep(200);

        Assert.True(harness.Engine.ProcessPinch(2, 0, s_origin));
        Assert.True(harness.Engine.ProcessPinch(3, 0, s_origin));
        Assert.Equal(3, harness.Engine.ZoomX, 12);
    }

    [Fact]
    public void ResetGestureState_Restarts_Recognition_Delay()
    {
        var harness = EngineHarness.Create(s => s.GestureRecognitionDelay = TimeSpan.FromMilliseconds(20));
        harness.Engine.ProcessPinch(2, 0, s_origin);
        Thread.Sleep(200);
        Assert.True(harness.Engine.ProcessPinch(2, 0, s_origin));

        harness.Engine.ResetGestureState();

        Assert.False(harness.Engine.ProcessPinch(2, 0, s_origin));
    }

    [Fact]
    public void Pinch_While_Panning_Requires_Simultaneous_Pan_Zoom()
    {
        var harness = EngineHarness.Create(s => s.EnableSimultaneousPanZoom = false);
        harness.Engine.ProcessPointerPressed(new CorePoint(0, 0), ZoomBorderPointerButtons.Middle);

        Assert.False(harness.Engine.ProcessPinch(2, 0, s_origin));
        Assert.Equal(1, harness.Engine.ZoomX);

        harness.Engine.ProcessPointerReleased();

        Assert.True(harness.Engine.ProcessPinch(2, 0, s_origin));
    }

    [Fact]
    public void Pinch_While_Panning_Allowed_With_Simultaneous_Pan_Zoom()
    {
        var harness = EngineHarness.Create();
        harness.Engine.ProcessPointerPressed(new CorePoint(0, 0), ZoomBorderPointerButtons.Middle);

        Assert.True(harness.Engine.ProcessPinch(2, 0, s_origin));

        Assert.Equal(2, harness.Engine.ZoomX);
    }

    [Fact]
    public void Pinch_Adds_History_On_Each_Update_And_On_End()
    {
        var harness = EngineHarness.Create();

        harness.Engine.ProcessPinch(1.5, 0, s_origin);
        harness.Engine.ProcessPinch(2, 0, s_origin);
        harness.Engine.ProcessPinchEnded();

        Assert.Equal(4, harness.Engine.ViewHistory.Count);
    }
}

public class ScrollGestureTests
{
    [Fact]
    public void Scroll_Gesture_Pans_Opposite_To_Delta()
    {
        var harness = EngineHarness.Create();

        Assert.True(harness.Engine.ProcessScrollGesture(new CoreVector(10, 20)));

        AssertEx.View(harness.Engine, 1, -10, -20);
        Assert.False(harness.Host.LastTransform.SkipTransitions);
    }

    [Fact]
    public void Scroll_Gesture_Is_Not_Handled_When_Translation_Disabled()
    {
        var harness = EngineHarness.Create(s => s.EnableGestureTranslation = false);

        Assert.False(harness.Engine.ProcessScrollGesture(new CoreVector(10, 20)));
        AssertEx.View(harness.Engine, 1, 0, 0);
    }

    [Fact]
    public void Scroll_Gesture_Without_Element_Is_Not_Handled()
    {
        var harness = EngineHarness.Create(attach: false);

        Assert.False(harness.Engine.ProcessScrollGesture(new CoreVector(10, 20)));
    }

    [Theory]
    [InlineData(3, 4, false)]
    [InlineData(1, 1, false)]
    [InlineData(1, 2, true)]
    public void Scroll_Gesture_Respects_Touch_Point_Limits(int minimum, int maximum, bool expected)
    {
        var harness = EngineHarness.Create(s =>
        {
            s.MinimumTouchPoints = minimum;
            s.MaximumTouchPoints = maximum;
        });

        Assert.Equal(expected, harness.Engine.ProcessScrollGesture(new CoreVector(10, 0)));
    }

    [Fact]
    public void Scroll_Gesture_Blocked_During_Simultaneous_Gesture_When_Simultaneous_Pan_Zoom_Disabled()
    {
        var harness = EngineHarness.Create();
        harness.Engine.ProcessPointerPressed(new CorePoint(0, 0), ZoomBorderPointerButtons.Middle);
        harness.Engine.ProcessPinch(1.5, 0, new CorePoint(0, 0));

        harness.Settings.EnableSimultaneousPanZoom = false;

        Assert.False(harness.Engine.ProcessScrollGesture(new CoreVector(10, 0)));

        harness.Engine.ProcessPinchEnded();

        Assert.True(harness.Engine.ProcessScrollGesture(new CoreVector(10, 0)));
    }

    [Fact]
    public void Scroll_Gesture_Ended_Raises_Event_And_Adds_History()
    {
        var harness = EngineHarness.Create();
        CoreGestureEventArgs? ended = null;
        harness.Engine.GestureEnded += (_, e) => ended = e;
        harness.Engine.ProcessScrollGesture(new CoreVector(10, 20));

        Assert.True(harness.Engine.ProcessScrollGestureEnded());

        Assert.NotNull(ended);
        Assert.Equal("Scroll", ended!.GestureType);
        Assert.Equal(-10, ended.OffsetX);
        Assert.Equal(-20, ended.OffsetY);
        Assert.Equal(0, ended.Delta);
        Assert.Equal(2, harness.Engine.ViewHistory.Count);
    }
}

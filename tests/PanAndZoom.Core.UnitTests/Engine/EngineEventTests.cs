// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
namespace PanAndZoom.Core.UnitTests.Engine;

public class EngineEventTests
{
    /// <summary>
    /// Subscribes to every engine event and logs it into the host call log so that host calls
    /// and engine events can be asserted in a single ordered sequence.
    /// </summary>
    private static EngineHarness CreateLogged(Action<PanAndZoomSettings>? configure = null, CoreSize? viewport = null, CoreSize? element = null)
    {
        var harness = EngineHarness.Create(configure, viewport, element);
        var engine = harness.Engine;
        var host = harness.Host;

        engine.ZoomChanged += (_, _) => host.Log(nameof(PanAndZoomEngine.ZoomChanged));
        engine.ViewHistoryChanged += (_, _) => host.Log(nameof(PanAndZoomEngine.ViewHistoryChanged));
        engine.PanStarted += (_, _) => host.Log(nameof(PanAndZoomEngine.PanStarted));
        engine.PanContinued += (_, _) => host.Log(nameof(PanAndZoomEngine.PanContinued));
        engine.PanEnded += (_, _) => host.Log(nameof(PanAndZoomEngine.PanEnded));
        engine.ZoomStarted += (_, _) => host.Log(nameof(PanAndZoomEngine.ZoomStarted));
        engine.ZoomEnded += (_, _) => host.Log(nameof(PanAndZoomEngine.ZoomEnded));
        engine.ZoomDeltaChanged += (_, _) => host.Log(nameof(PanAndZoomEngine.ZoomDeltaChanged));
        engine.MatrixChanged += (_, _) => host.Log(nameof(PanAndZoomEngine.MatrixChanged));
        engine.MatrixReset += (_, _) => host.Log(nameof(PanAndZoomEngine.MatrixReset));
        engine.StretchModeChanged += (_, _) => host.Log(nameof(PanAndZoomEngine.StretchModeChanged));
        engine.AutoFitApplied += (_, _) => host.Log(nameof(PanAndZoomEngine.AutoFitApplied));
        engine.GestureStarted += (_, _) => host.Log(nameof(PanAndZoomEngine.GestureStarted));
        engine.GestureEnded += (_, _) => host.Log(nameof(PanAndZoomEngine.GestureEnded));
        engine.ScrollInvalidated += (_, _) => host.Log(nameof(PanAndZoomEngine.ScrollInvalidated));

        return harness;
    }

    private static readonly string[] s_invalidate =
    {
        nameof(TestHost.UpdateViewProperties),
        nameof(PanAndZoomEngine.ScrollInvalidated),
        nameof(TestHost.ApplyTransform),
        nameof(PanAndZoomEngine.ZoomChanged)
    };

    private static string[] Sequence(params string[] tail) => s_invalidate.Concat(tail).ToArray();

    [Fact]
    public void ZoomTo_Event_Order()
    {
        var harness = CreateLogged();

        harness.Engine.ZoomTo(2, 0, 0);

        Assert.Equal(Sequence(nameof(PanAndZoomEngine.ZoomDeltaChanged), nameof(PanAndZoomEngine.ViewHistoryChanged)), harness.Host.CallLog);
    }

    [Fact]
    public void ZoomIn_Event_Order()
    {
        var harness = CreateLogged();

        harness.Engine.ZoomIn();

        Assert.Equal(
            Sequence(nameof(PanAndZoomEngine.ZoomDeltaChanged), nameof(PanAndZoomEngine.ViewHistoryChanged), nameof(PanAndZoomEngine.ZoomEnded)),
            harness.Host.CallLog);
    }

    [Fact]
    public void Zoom_Event_Order()
    {
        var harness = CreateLogged();

        harness.Engine.Zoom(2, 0, 0);

        Assert.Equal(Sequence(nameof(PanAndZoomEngine.ZoomStarted)), harness.Host.CallLog);
    }

    [Fact]
    public void PanDelta_Event_Order()
    {
        var harness = CreateLogged();

        harness.Engine.PanDelta(1, 2);

        Assert.Equal(Sequence(nameof(PanAndZoomEngine.PanContinued)), harness.Host.CallLog);
    }

    [Fact]
    public void Pan_Event_Order()
    {
        var harness = CreateLogged();

        harness.Engine.Pan(1, 2);

        Assert.Equal(Sequence(nameof(PanAndZoomEngine.PanContinued), nameof(PanAndZoomEngine.ViewHistoryChanged)), harness.Host.CallLog);
    }

    [Fact]
    public void ResetMatrix_Event_Order()
    {
        var harness = CreateLogged();

        harness.Engine.ResetMatrix();

        Assert.Equal(
            Sequence(nameof(PanAndZoomEngine.MatrixChanged), nameof(PanAndZoomEngine.MatrixReset), nameof(PanAndZoomEngine.ViewHistoryChanged)),
            harness.Host.CallLog);
    }

    [Fact]
    public void Stretch_Event_Order()
    {
        var harness = CreateLogged();

        harness.Engine.Uniform();

        Assert.Equal(Sequence(nameof(PanAndZoomEngine.StretchModeChanged), nameof(PanAndZoomEngine.ViewHistoryChanged)), harness.Host.CallLog);
    }

    [Fact]
    public void AutoFit_Event_Order()
    {
        var harness = CreateLogged();

        harness.Engine.AutoFit();

        Assert.Equal(Sequence(nameof(PanAndZoomEngine.AutoFitApplied), nameof(PanAndZoomEngine.ViewHistoryChanged)), harness.Host.CallLog);
    }

    [Fact]
    public void Pointer_Pan_Event_Order()
    {
        var harness = CreateLogged();
        var engine = harness.Engine;

        engine.ProcessPointerPressed(new CorePoint(0, 0), ZoomBorderPointerButtons.Middle);
        engine.ProcessPointerMoved(new CorePoint(5, 5), ZoomBorderPointerButtons.Middle);
        engine.ProcessPointerReleased();

        var expected = new List<string> { nameof(PanAndZoomEngine.PanStarted), nameof(TestHost.OnIsPanningChanged) };
        expected.AddRange(Sequence(nameof(PanAndZoomEngine.PanContinued)));
        expected.AddRange(new[] { nameof(PanAndZoomEngine.PanEnded), nameof(PanAndZoomEngine.ViewHistoryChanged), nameof(TestHost.OnIsPanningChanged) });
        Assert.Equal(expected, harness.Host.CallLog);
    }

    [Fact]
    public void Pinch_Event_Order()
    {
        var harness = CreateLogged();

        harness.Engine.ProcessPinch(2, 0, new CorePoint(0, 0));
        harness.Engine.ProcessPinchEnded();

        var expected = new List<string> { nameof(PanAndZoomEngine.GestureStarted) };
        expected.AddRange(Sequence(nameof(PanAndZoomEngine.ZoomDeltaChanged), nameof(PanAndZoomEngine.ViewHistoryChanged)));
        expected.AddRange(new[] { nameof(PanAndZoomEngine.GestureEnded), nameof(PanAndZoomEngine.ViewHistoryChanged) });
        Assert.Equal(expected, harness.Host.CallLog);
    }

    [Fact]
    public void Scroll_Gesture_Event_Order()
    {
        var harness = CreateLogged();

        harness.Engine.ProcessScrollGesture(new CoreVector(3, 4));
        harness.Engine.ProcessScrollGestureEnded();

        var expected = new List<string> { nameof(PanAndZoomEngine.GestureStarted) };
        expected.AddRange(Sequence(nameof(PanAndZoomEngine.PanContinued)));
        expected.AddRange(new[] { nameof(PanAndZoomEngine.GestureEnded), nameof(PanAndZoomEngine.ViewHistoryChanged) });
        Assert.Equal(expected, harness.Host.CallLog);
    }

    [Fact]
    public void ZoomChanged_Reports_Current_Values_And_Engine_Sender()
    {
        var harness = EngineHarness.Create();
        object? sender = null;
        ZoomChangedEventArgs? args = null;
        harness.Engine.ZoomChanged += (s, e) =>
        {
            sender = s;
            args = e;
        };

        harness.Engine.ZoomTo(2, 100, 50);

        Assert.Same(harness.Engine, sender);
        Assert.NotNull(args);
        Assert.Equal(2, args!.ZoomX);
        Assert.Equal(2, args.ZoomY);
        Assert.Equal(-100, args.OffsetX);
        Assert.Equal(-50, args.OffsetY);
    }

    [Fact]
    public void ZoomDeltaChanged_Reports_Values()
    {
        var harness = EngineHarness.Create();
        CoreZoomEventArgs? args = null;
        harness.Engine.ZoomDeltaChanged += (_, e) => args = e;

        harness.Engine.ZoomTo(2, 100, 50);

        Assert.Equal(
            new CoreZoomEventArgs(2, 2, 1, 1, 2, 100, 50, -100, -50, new CoreMatrix(2, 0, 0, 2, -100, -50), CoreMatrix.Identity),
            args);
    }

    [Fact]
    public void ZoomDeltaChanged_Reports_Requested_Ratio_When_Clamped()
    {
        var harness = EngineHarness.Create(s => s.MaxZoomX = s.MaxZoomY = 2);
        CoreZoomEventArgs? args = null;
        harness.Engine.ZoomDeltaChanged += (_, e) => args = e;

        harness.Engine.ZoomTo(5, 0, 0);

        Assert.Equal(5, args!.ZoomDelta);
        Assert.Equal(2, args.ZoomX);
    }

    [Fact]
    public void ZoomEnded_Reports_Step_Values()
    {
        var harness = EngineHarness.Create();
        CoreZoomEventArgs? args = null;
        harness.Engine.ZoomEnded += (_, e) => args = e;

        harness.Engine.ZoomIn();

        Assert.NotNull(args);
        Assert.Equal(1.2, args!.ZoomX, 12);
        Assert.Equal(1, args.PreviousZoomX);
        Assert.Equal(1.2, args.ZoomDelta);
        Assert.Equal(200, args.CenterX);
        Assert.Equal(200, args.CenterY);
        Assert.Equal(CoreMatrix.Identity, args.PreviousMatrix);
        Assert.Equal(harness.Engine.Matrix, args.Matrix);
    }

    [Fact]
    public void ZoomStarted_Reports_Ratio_To_Previous_Zoom()
    {
        var harness = EngineHarness.Create();
        harness.Engine.ZoomTo(2, 0, 0);
        CoreZoomEventArgs? args = null;
        harness.Engine.ZoomStarted += (_, e) => args = e;

        harness.Engine.Zoom(3, 10, 20);

        Assert.NotNull(args);
        Assert.Equal(3, args!.ZoomX);
        Assert.Equal(2, args.PreviousZoomX);
        Assert.Equal(1.5, args.ZoomDelta);
        Assert.Equal(10, args.CenterX);
        Assert.Equal(20, args.CenterY);
        Assert.Equal(new CoreMatrix(2, 0, 0, 2, 0, 0), args.PreviousMatrix);
    }

    [Fact]
    public void PanContinued_Reports_Delta_And_Previous_Offsets()
    {
        var harness = EngineHarness.Create();
        harness.Engine.PanDelta(10, 20);
        CorePanEventArgs? args = null;
        harness.Engine.PanContinued += (_, e) => args = e;

        harness.Engine.PanDelta(5, -5);

        Assert.Equal(
            new CorePanEventArgs(1, 1, 15, 15, 10, 20, 5, -5, MatrixMath.Translate(15, 15), MatrixMath.Translate(10, 20)),
            args);
    }

    [Fact]
    public void Pan_Reports_Absolute_Delta()
    {
        var harness = EngineHarness.Create();
        harness.Engine.PanDelta(10, 20);
        CorePanEventArgs? args = null;
        harness.Engine.PanContinued += (_, e) => args = e;

        harness.Engine.Pan(30, 30);

        Assert.Equal(20, args!.DeltaX);
        Assert.Equal(10, args.DeltaY);
        Assert.Equal(10, args.PreviousOffsetX);
        Assert.Equal(30, args.OffsetX);
    }

    [Fact]
    public void Pointer_Pan_Events_Report_Values()
    {
        var harness = EngineHarness.Create();
        var engine = harness.Engine;
        CorePanEventArgs? started = null;
        CorePanEventArgs? continued = null;
        CorePanEventArgs? ended = null;
        engine.PanStarted += (_, e) => started = e;
        engine.PanContinued += (_, e) => continued = e;
        engine.PanEnded += (_, e) => ended = e;

        engine.ProcessPointerPressed(new CorePoint(10, 10), ZoomBorderPointerButtons.Middle);
        engine.ProcessPointerMoved(new CorePoint(15, 18), ZoomBorderPointerButtons.Middle);
        engine.ProcessPointerReleased();

        Assert.Equal(new CorePanEventArgs(1, 1, 0, 0, 0, 0, 0, 0, CoreMatrix.Identity, CoreMatrix.Identity), started);
        Assert.Equal(new CorePanEventArgs(1, 1, 5, 8, 0, 0, 5, 8, MatrixMath.Translate(5, 8), CoreMatrix.Identity), continued);
        Assert.Equal(new CorePanEventArgs(1, 1, 5, 8, 5, 8, 0, 0, MatrixMath.Translate(5, 8), MatrixMath.Translate(5, 8)), ended);
    }

    [Fact]
    public void MatrixChanged_And_MatrixReset_Report_Values()
    {
        var harness = EngineHarness.Create();
        harness.SetMatrix(2, 10, 20);
        CoreMatrixChangedEventArgs? changed = null;
        CoreMatrixChangedEventArgs? reset = null;
        harness.Engine.MatrixChanged += (_, e) => changed = e;
        harness.Engine.MatrixReset += (_, e) => reset = e;

        harness.Engine.ResetMatrix();

        var previous = MatrixMath.ScaleAndTranslate(2, 2, 10, 20);
        Assert.Equal(new CoreMatrixChangedEventArgs(CoreMatrix.Identity, previous, 1, 1, 0, 0, 2, 2, 10, 20, "SetMatrix"), changed);
        Assert.Equal(new CoreMatrixChangedEventArgs(CoreMatrix.Identity, previous, 1, 1, 0, 0, 2, 2, 10, 20, "ResetMatrix"), reset);
    }

    [Fact]
    public void MatrixChanged_Reports_Constrained_Matrix()
    {
        var harness = EngineHarness.Create(s => s.MaxZoomX = s.MaxZoomY = 2);
        CoreMatrixChangedEventArgs? changed = null;
        harness.Engine.MatrixChanged += (_, e) => changed = e;

        harness.Engine.SetMatrix(MatrixMath.Scale(5, 5));

        Assert.Equal(MatrixMath.Scale(2, 2), changed!.Matrix);
        Assert.Equal(2, changed.ZoomX);
        Assert.Equal("SetMatrix", changed.Operation);
    }

    [Fact]
    public void StretchModeChanged_Reports_Values()
    {
        var harness = EngineHarness.Create(s => s.Stretch = StretchMode.None, new CoreSize(400, 300), new CoreSize(200, 100));
        CoreStretchModeChangedEventArgs? args = null;
        harness.Engine.StretchModeChanged += (_, e) => args = e;

        harness.Engine.Fill();

        Assert.Equal(
            new CoreStretchModeChangedEventArgs(
                StretchMode.Fill,
                StretchMode.None,
                new CoreMatrix(2, 0, 0, 3, -100, -100),
                CoreMatrix.Identity,
                2,
                3,
                -100,
                -100,
                400,
                300,
                200,
                100),
            args);
    }

    [Fact]
    public void AutoFitApplied_Reports_Values()
    {
        var harness = EngineHarness.Create(s => s.Stretch = StretchMode.None, new CoreSize(400, 300), new CoreSize(200, 100));
        harness.Settings.Stretch = StretchMode.Uniform;
        CoreStretchModeChangedEventArgs? args = null;
        harness.Engine.AutoFitApplied += (_, e) => args = e;

        harness.Engine.AutoFit();

        Assert.Equal(
            new CoreStretchModeChangedEventArgs(
                StretchMode.Uniform,
                StretchMode.Uniform,
                new CoreMatrix(2, 0, 0, 2, -100, -50),
                CoreMatrix.Identity,
                2,
                2,
                -100,
                -50,
                400,
                300,
                200,
                100),
            args);
    }

    [Fact]
    public void Pinch_GestureStarted_Reports_Values_Before_Zoom()
    {
        var harness = EngineHarness.Create();
        CoreGestureEventArgs? started = null;
        CoreGestureEventArgs? ended = null;
        harness.Engine.GestureStarted += (_, e) => started = e;
        harness.Engine.GestureEnded += (_, e) => ended = e;

        harness.Engine.ProcessPinch(1.5, 0, new CorePoint(10, 20));
        var matrix = harness.Engine.Matrix;
        harness.Engine.ProcessPinchEnded();

        Assert.Equal(new CoreGestureEventArgs("Pinch", 1, 1, 0, 0, 10, 20, 0.5, CoreMatrix.Identity, CoreMatrix.Identity), started);
        Assert.Equal(new CoreGestureEventArgs("Pinch", 1.5, 1.5, matrix.M31, matrix.M32, 0, 0, 0, matrix, matrix), ended);
    }

    [Fact]
    public void Scroll_GestureStarted_Reports_Delta_Length()
    {
        var harness = EngineHarness.Create();
        CoreGestureEventArgs? started = null;
        harness.Engine.GestureStarted += (_, e) => started = e;

        harness.Engine.ProcessScrollGesture(new CoreVector(3, 4));

        Assert.Equal(new CoreGestureEventArgs("Scroll", 1, 1, 0, 0, 0, 0, 5, CoreMatrix.Identity, CoreMatrix.Identity), started);
    }

    [Fact]
    public void Ignored_Operations_Raise_No_Events()
    {
        var harness = CreateLogged(s => s.MaxZoomX = s.MaxZoomY = 1);

        harness.Engine.ZoomTo(2, 0, 0);
        harness.Engine.ProcessPointerReleased();
        harness.Engine.NavigateBack();
        harness.Engine.HideZoomIndicator();

        Assert.Empty(harness.Host.CallLog);
    }
}

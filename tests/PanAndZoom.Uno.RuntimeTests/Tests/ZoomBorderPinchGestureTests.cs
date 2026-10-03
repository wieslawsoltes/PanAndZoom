// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

namespace PanAndZoom.Uno.RuntimeTests.Tests;

/// <summary>
/// Pinch gesture tests ported from the Avalonia <c>ZoomBorderPinchGestureTests</c>.
/// </summary>
/// <remarks>
/// The Avalonia tests raise <c>PinchEventArgs</c> with exact cumulative scales directly; the Uno equivalent drives the
/// internal manipulation handlers (<see cref="ZoomBorder.HandleManipulationDelta"/>) with the same values. The Avalonia
/// <c>e.Handled</c> flag is mapped to the <see cref="ZoomBorder.GestureStarted"/>/<see cref="ZoomBorder.GestureEnded"/>
/// events, which are raised exactly when the engine handles the pinch (end).
/// </remarks>
[TestClass]
[RunsOnUIThread]
public class ZoomBorderPinchGestureTests
{
    [TestCleanup]
    public async Task Cleanup()
    {
        GestureTestHelpers.ResetTouch();
        InputHelper.Reset();
        await ZoomBorderTestHelper.UnloadAsync();
    }

    private static async Task<(ZoomBorder ZoomBorder, GestureRecorder Recorder)> CreateAsync(Action<ZoomBorder>? configure = null)
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150, zb =>
        {
            zb.EnableGestureZoom = true;
            zb.EnableGestures = true;
            configure?.Invoke(zb);
        });

        return (zoomBorder, new GestureRecorder(zoomBorder));
    }

    [TestMethod]
    public async Task PinchGesture_ZoomIn_IncreasesZoom()
    {
        var (zoomBorder, recorder) = await CreateAsync();
        var initialZoomX = zoomBorder.ZoomX;
        var initialZoomY = zoomBorder.ZoomY;

        GestureTestHelpers.Pinch(zoomBorder, 1.5, new Point(0.5, 0.5));

        Assert.IsTrue(zoomBorder.ZoomX > initialZoomX, "ZoomX should increase after pinch zoom in");
        Assert.IsTrue(zoomBorder.ZoomY > initialZoomY, "ZoomY should increase after pinch zoom in");
        // Uno adaptation: "handled" = the engine processed the pinch (GestureStarted "Pinch").
        Assert.IsTrue(recorder.Pinches.Count > 0, "Pinch should be handled");
    }

    [TestMethod]
    public async Task PinchGesture_ZoomOut_DecreasesZoom()
    {
        var (zoomBorder, recorder) = await CreateAsync();

        // First zoom in to have something to zoom out from
        zoomBorder.ZoomTo(2.0, 100, 75);

        var initialZoomX = zoomBorder.ZoomX;
        var initialZoomY = zoomBorder.ZoomY;

        GestureTestHelpers.Pinch(zoomBorder, 0.8, new Point(0.5, 0.5));

        Assert.IsTrue(zoomBorder.ZoomX < initialZoomX, "ZoomX should decrease after pinch zoom out");
        Assert.IsTrue(zoomBorder.ZoomY < initialZoomY, "ZoomY should decrease after pinch zoom out");
        // Uno adaptation: "handled" = the engine processed the pinch (GestureStarted "Pinch").
        Assert.IsTrue(recorder.Pinches.Count > 0, "Pinch should be handled");
    }

    [TestMethod]
    public async Task PinchGesture_DifferentScaleOrigins_ZoomsAtCorrectLocation()
    {
        var (zoomBorder, recorder) = await CreateAsync();
        var initialZoom = zoomBorder.ZoomX;

        // Pinch at the top-left corner
        GestureTestHelpers.Pinch(zoomBorder, 1.5, new Point(0.0, 0.0));

        Assert.IsTrue(zoomBorder.ZoomX > initialZoom, "Zoom should increase");
        // Uno adaptation: "handled" = the engine processed the pinch (GestureStarted "Pinch").
        Assert.IsTrue(recorder.Pinches.Count > 0, "Pinch should be handled");
    }

    [TestMethod]
    public async Task PinchGesture_GestureZoomDisabled_DoesNotZoom()
    {
        var (zoomBorder, recorder) = await CreateAsync(zb =>
        {
            zb.EnableGestureZoom = false;
            zb.EnableGestureRotation = false; // Also disable rotation to fully ignore pinch
        });
        var initialZoomX = zoomBorder.ZoomX;
        var initialZoomY = zoomBorder.ZoomY;

        GestureTestHelpers.Pinch(zoomBorder, 1.5, new Point(0.5, 0.5));

        Assert.AreEqual(initialZoomX, zoomBorder.ZoomX);
        Assert.AreEqual(initialZoomY, zoomBorder.ZoomY);
        // Uno adaptation: "not handled" = the engine did not process the pinch (no GestureStarted "Pinch").
        Assert.AreEqual(0, recorder.Pinches.Count, "Pinch should not be handled when gesture zoom and rotation are disabled");
    }

    [TestMethod]
    public async Task PinchGesture_NoChildElement_DoesNotCrash()
    {
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            EnableGestureZoom = true,
            EnableGestures = true
        };
        await ZoomBorderTestHelper.LoadAsync(zoomBorder);
        var recorder = new GestureRecorder(zoomBorder);

        // Should not throw exception
        GestureTestHelpers.Pinch(zoomBorder, 1.5, new Point(0.5, 0.5));
        zoomBorder.HandleManipulationCompleted();

        // Uno adaptation: "not handled" = the engine did not process the pinch (no GestureStarted "Pinch").
        Assert.AreEqual(0, recorder.Pinches.Count, "Pinch should not be handled when no child element");
    }

    [TestMethod]
    public async Task PinchGestureEnded_HandlesEvent()
    {
        var (zoomBorder, recorder) = await CreateAsync();

        // Uno adaptation: a pinch ends when the manipulation that started it completes (there is no standalone
        // pinch-ended event), so a pinch update precedes the completion.
        GestureTestHelpers.Pinch(zoomBorder, 1.1, new Point(200, 150));
        zoomBorder.HandleManipulationCompleted();

        // "handled" = the engine processed the pinch end (GestureEnded "Pinch").
        Assert.AreEqual(1, recorder.PinchEndedCount, "Pinch ended should be handled");
    }

    [TestMethod]
    public async Task PinchGesture_MultipleScaleChanges_AccumulatesZoom()
    {
        var (zoomBorder, _) = await CreateAsync();
        var initialZoom = zoomBorder.ZoomX;

        // Two updates of the same pinch: cumulative scale 1.2, then 1.3 (Avalonia PinchEventArgs semantics).
        GestureTestHelpers.Pinch(zoomBorder, 1.2, new Point(0.5, 0.5));
        var intermediateZoom = zoomBorder.ZoomX;

        GestureTestHelpers.PinchDelta(zoomBorder, 1.3, new Point(0.5, 0.5), incrementalScale: 1.3 / 1.2);

        Assert.IsTrue(intermediateZoom > initialZoom, "First pinch should increase zoom");
        Assert.IsTrue(zoomBorder.ZoomX > intermediateZoom, "Second pinch should further increase zoom");
    }

    [TestMethod]
    public async Task PinchGesture_ScaleOfOne_NoZoomChange()
    {
        var (zoomBorder, recorder) = await CreateAsync();
        var initialZoomX = zoomBorder.ZoomX;
        var initialZoomY = zoomBorder.ZoomY;

        GestureTestHelpers.Pinch(zoomBorder, 1.0, new Point(0.5, 0.5));

        Assert.AreEqual(initialZoomX, zoomBorder.ZoomX);
        Assert.AreEqual(initialZoomY, zoomBorder.ZoomY);
        // Uno adaptation: a manipulation delta with scale 1 and no rotation is a pure translation, so it never reaches the
        // pinch path (Avalonia reports such a pinch as handled); it must not be processed as a pinch.
        Assert.AreEqual(0, recorder.Pinches.Count, "A scale of one should not be processed as a pinch");
    }
}

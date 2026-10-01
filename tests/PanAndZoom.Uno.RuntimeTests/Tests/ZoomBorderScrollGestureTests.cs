// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

namespace PanAndZoom.Uno.RuntimeTests.Tests;

/// <summary>
/// Scroll (one finger pan) gesture tests ported from the Avalonia <c>ZoomBorderScrollGestureTests</c>.
/// </summary>
/// <remarks>
/// The Avalonia tests raise <c>ScrollGestureEventArgs</c> directly; on Uno a scroll gesture is a one finger touch
/// manipulation, so real touch drags are injected. An Avalonia scroll delta D corresponds to the finger moving by -D
/// (the content follows the finger). The Avalonia <c>e.Handled</c> flag is mapped to the
/// <see cref="ZoomBorder.GestureStarted"/>/<see cref="ZoomBorder.GestureEnded"/> "Scroll" events, which are raised
/// exactly when the engine handles the scroll (end).
/// </remarks>
[TestClass]
[RunsOnUIThread]
public class ZoomBorderScrollGestureTests
{
    private static readonly Point Start = new(200, 150);

    [TestCleanup]
    public async Task Cleanup()
    {
        GestureTestHelpers.ResetTouch();
        InputHelper.Reset();
        await ZoomBorderTestHelper.UnloadAsync();
    }

    private static async Task<(ZoomBorder ZoomBorder, GestureRecorder Recorder)> CreateAsync(bool enableGestureTranslation = true)
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150, zb =>
        {
            zb.EnableGestureTranslation = enableGestureTranslation;
            zb.EnableGestures = true;
        });

        return (zoomBorder, new GestureRecorder(zoomBorder));
    }

    private static async Task ScrollAsync(ZoomBorder zoomBorder, double deltaX, double deltaY)
    {
        new TestPointer().Drag(zoomBorder, Start, new Point(Start.X - deltaX, Start.Y - deltaY));
        await ZoomBorderTestHelper.WaitForIdleAsync();
    }

    [TestMethod]
    public async Task ScrollGesture_PanRight_ChangesOffsetX()
    {
        var (zoomBorder, recorder) = await CreateAsync();
        var initialOffsetX = zoomBorder.OffsetX;

        await ScrollAsync(zoomBorder, 50, 0);

        Assert.AreNotEqual(initialOffsetX, zoomBorder.OffsetX);
        Assert.AreEqual(initialOffsetX - 50, zoomBorder.OffsetX, 1.0);
        // Uno adaptation: "handled" = the engine processed the scroll (GestureStarted "Scroll").
        Assert.IsTrue(recorder.Scrolls.Count > 0, "Scroll gesture should be handled");
    }

    [TestMethod]
    public async Task ScrollGesture_PanLeft_ChangesOffsetX()
    {
        var (zoomBorder, recorder) = await CreateAsync();
        var initialOffsetX = zoomBorder.OffsetX;

        await ScrollAsync(zoomBorder, -30, 0);

        Assert.AreNotEqual(initialOffsetX, zoomBorder.OffsetX);
        Assert.AreEqual(initialOffsetX + 30, zoomBorder.OffsetX, 1.0);
        // Uno adaptation: "handled" = the engine processed the scroll (GestureStarted "Scroll").
        Assert.IsTrue(recorder.Scrolls.Count > 0, "Scroll gesture should be handled");
    }

    [TestMethod]
    public async Task ScrollGesture_PanUp_ChangesOffsetY()
    {
        var (zoomBorder, recorder) = await CreateAsync();
        var initialOffsetY = zoomBorder.OffsetY;

        await ScrollAsync(zoomBorder, 0, -25);

        Assert.AreNotEqual(initialOffsetY, zoomBorder.OffsetY);
        Assert.AreEqual(initialOffsetY + 25, zoomBorder.OffsetY, 1.0);
        // Uno adaptation: "handled" = the engine processed the scroll (GestureStarted "Scroll").
        Assert.IsTrue(recorder.Scrolls.Count > 0, "Scroll gesture should be handled");
    }

    [TestMethod]
    public async Task ScrollGesture_PanDown_ChangesOffsetY()
    {
        var (zoomBorder, recorder) = await CreateAsync();
        var initialOffsetY = zoomBorder.OffsetY;

        await ScrollAsync(zoomBorder, 0, 40);

        Assert.AreNotEqual(initialOffsetY, zoomBorder.OffsetY);
        Assert.AreEqual(initialOffsetY - 40, zoomBorder.OffsetY, 1.0);
        // Uno adaptation: "handled" = the engine processed the scroll (GestureStarted "Scroll").
        Assert.IsTrue(recorder.Scrolls.Count > 0, "Scroll gesture should be handled");
    }

    [TestMethod]
    public async Task ScrollGesture_DiagonalPan_ChangesBothOffsets()
    {
        var (zoomBorder, recorder) = await CreateAsync();
        var initialOffsetX = zoomBorder.OffsetX;
        var initialOffsetY = zoomBorder.OffsetY;

        await ScrollAsync(zoomBorder, 30, 40);

        Assert.AreNotEqual(initialOffsetX, zoomBorder.OffsetX);
        Assert.AreNotEqual(initialOffsetY, zoomBorder.OffsetY);
        Assert.AreEqual(initialOffsetX - 30, zoomBorder.OffsetX, 1.0);
        Assert.AreEqual(initialOffsetY - 40, zoomBorder.OffsetY, 1.0);
        // Uno adaptation: "handled" = the engine processed the scroll (GestureStarted "Scroll").
        Assert.IsTrue(recorder.Scrolls.Count > 0, "Scroll gesture should be handled");
    }

    [TestMethod]
    public async Task ScrollGesture_GesturePanDisabled_DoesNotPan()
    {
        var (zoomBorder, recorder) = await CreateAsync(enableGestureTranslation: false);
        var initialOffsetX = zoomBorder.OffsetX;
        var initialOffsetY = zoomBorder.OffsetY;

        await ScrollAsync(zoomBorder, 50, 50);

        Assert.AreEqual(initialOffsetX, zoomBorder.OffsetX);
        Assert.AreEqual(initialOffsetY, zoomBorder.OffsetY);
        // Uno adaptation: "not handled" = the engine did not process the scroll (no GestureStarted "Scroll").
        Assert.AreEqual(0, recorder.Scrolls.Count, "Scroll gesture should not be handled when gesture translation is disabled");
    }

    [TestMethod]
    public async Task ScrollGesture_NoChildElement_DoesNotCrash()
    {
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            EnableGestureTranslation = true,
            EnableGestures = true
        };
        await ZoomBorderTestHelper.LoadAsync(zoomBorder);
        var recorder = new GestureRecorder(zoomBorder);

        // Should not throw exception (driven through the manipulation handler: without a child nothing is hit-testable).
        zoomBorder.HandleManipulationStarted();
        GestureTestHelpers.ScrollDelta(zoomBorder, new Point(50, 50));
        zoomBorder.HandleManipulationCompleted();

        // Uno adaptation: "not handled" = the engine did not process the scroll (no GestureStarted "Scroll").
        Assert.AreEqual(0, recorder.Scrolls.Count, "Scroll gesture should not be handled when no child element");
    }

    [TestMethod]
    public async Task ScrollGestureEnded_HandlesEvent()
    {
        var (zoomBorder, recorder) = await CreateAsync();

        // Uno adaptation: a scroll ends when the manipulation that started it completes (finger released).
        await ScrollAsync(zoomBorder, 30, 0);

        // "handled" = the engine processed the scroll end (GestureEnded "Scroll").
        Assert.AreEqual(1, recorder.ScrollEndedCount, "Scroll gesture ended should be handled");
    }

    [TestMethod]
    public async Task ScrollGesture_MultipleDeltas_AccumulatesPan()
    {
        var (zoomBorder, _) = await CreateAsync();
        var initialOffsetX = zoomBorder.OffsetX;
        var touch = new TestPointer();

        // Two updates of the same scroll gesture: deltas (20,0) then (30,0).
        touch.Down(zoomBorder, Start);
        touch.Move(zoomBorder, new Point(Start.X - 20, Start.Y));
        await ZoomBorderTestHelper.WaitForIdleAsync();
        var intermediateOffsetX = zoomBorder.OffsetX;

        touch.Move(zoomBorder, new Point(Start.X - 50, Start.Y));
        touch.Up(zoomBorder);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.AreNotEqual(initialOffsetX, intermediateOffsetX);
        Assert.AreNotEqual(intermediateOffsetX, zoomBorder.OffsetX);
        Assert.AreEqual(initialOffsetX - 50, zoomBorder.OffsetX, 1.0);
    }

    [TestMethod]
    public async Task ScrollGesture_ZeroDelta_NoOffsetChange()
    {
        var (zoomBorder, recorder) = await CreateAsync();
        var initialOffsetX = zoomBorder.OffsetX;
        var initialOffsetY = zoomBorder.OffsetY;

        zoomBorder.HandleManipulationStarted();
        GestureTestHelpers.ScrollDelta(zoomBorder, new Point(0, 0));
        zoomBorder.HandleManipulationCompleted();

        Assert.AreEqual(initialOffsetX, zoomBorder.OffsetX);
        Assert.AreEqual(initialOffsetY, zoomBorder.OffsetY);
        // Uno adaptation: a manipulation delta without translation never reaches the scroll path (Avalonia reports such a
        // scroll as handled); it must not be processed as a scroll.
        Assert.AreEqual(0, recorder.Scrolls.Count, "A zero delta should not be processed as a scroll");
    }
}

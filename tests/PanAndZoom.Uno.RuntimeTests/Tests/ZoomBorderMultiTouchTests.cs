// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

namespace PanAndZoom.Uno.RuntimeTests.Tests;

/// <summary>
/// Tests for ZoomBorder multi-touch functionality (MinimumTouchPoints, MaximumTouchPoints,
/// EnableSimultaneousPanZoom and GestureRecognitionDelay).
/// </summary>
/// <remarks>
/// The Avalonia tests raise synthetic <c>PinchEvent</c>/<c>ScrollGestureEvent</c> events. On Uno the same
/// gestures are produced by real injected touch input (two fingers pinch, one finger scrolls) through
/// WinUI manipulations. "Event handled" assertions become assertions on the <c>GestureStarted</c> event,
/// which the control raises only when it processes the gesture.
/// </remarks>
[TestClass]
[RunsOnUIThread]
public class ZoomBorderMultiTouchTests
{
    [TestCleanup]
    public async Task Cleanup()
    {
        InputHelper.Reset();
        await ZoomBorderTestHelper.UnloadAsync();
    }

    private static Task<(ZoomBorder ZoomBorder, Border Child)> CreateAsync(Action<ZoomBorder> configure)
    {
        return ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150, configure);
    }

    private static List<string> TrackGestures(ZoomBorder zoomBorder)
    {
        var started = new List<string>();
        zoomBorder.GestureStarted += (_, e) => started.Add(e.GestureType);
        return started;
    }

    [TestMethod]
    public void EnableSimultaneousPanZoom_DefaultValue_IsTrue()
    {
        var zoomBorder = new ZoomBorder();

        Assert.IsTrue(zoomBorder.EnableSimultaneousPanZoom);
    }

    [TestMethod]
    public void MinimumTouchPoints_DefaultValue_Is1()
    {
        var zoomBorder = new ZoomBorder();

        Assert.AreEqual(1, zoomBorder.MinimumTouchPoints);
    }

    [TestMethod]
    public void MaximumTouchPoints_DefaultValue_Is2()
    {
        var zoomBorder = new ZoomBorder();

        Assert.AreEqual(2, zoomBorder.MaximumTouchPoints);
    }

    [TestMethod]
    public void GestureRecognitionDelay_DefaultValue_IsZero()
    {
        var zoomBorder = new ZoomBorder();

        // Default is zero (no delay), set positive value to enable delay
        Assert.AreEqual(TimeSpan.Zero, zoomBorder.GestureRecognitionDelay);
    }

    [TestMethod]
    public void EnableSimultaneousPanZoom_CanBeSet()
    {
        var zoomBorder = new ZoomBorder();

        zoomBorder.EnableSimultaneousPanZoom = false;

        Assert.IsFalse(zoomBorder.EnableSimultaneousPanZoom);
    }

    [TestMethod]
    public void MinimumTouchPoints_CanBeSet()
    {
        var zoomBorder = new ZoomBorder();

        zoomBorder.MinimumTouchPoints = 2;

        Assert.AreEqual(2, zoomBorder.MinimumTouchPoints);
    }

    [TestMethod]
    public void MaximumTouchPoints_CanBeSet()
    {
        var zoomBorder = new ZoomBorder();

        zoomBorder.MaximumTouchPoints = 3;

        Assert.AreEqual(3, zoomBorder.MaximumTouchPoints);
    }

    [TestMethod]
    public void GestureRecognitionDelay_CanBeSet()
    {
        var zoomBorder = new ZoomBorder();

        zoomBorder.GestureRecognitionDelay = TimeSpan.FromMilliseconds(100);

        Assert.AreEqual(TimeSpan.FromMilliseconds(100), zoomBorder.GestureRecognitionDelay);
    }

    /// <summary>
    /// Verifies that EnableSimultaneousPanZoom property controls simultaneous gesture handling.
    /// When false, pinch gestures are blocked when panning is active.
    /// </summary>
    [TestMethod]
    public async Task EnableSimultaneousPanZoom_WhenFalse_BlocksSimultaneousGestures()
    {
        var (zoomBorder, _) = await CreateAsync(zb => zb.EnableSimultaneousPanZoom = false);

        Assert.IsFalse(zoomBorder.EnableSimultaneousPanZoom);

        // Uno adaptation: the Avalonia test only documents the behavior; here a real middle button mouse pan is
        // kept active while a real two finger pinch is injected, and the pinch must be ignored.
        var initialZoom = zoomBorder.ZoomX;
        InputHelper.MouseMoveTo(ZoomBorderTestHelper.ToWindow(zoomBorder, new Point(50, 50)));
        InputHelper.MouseDown(ButtonName.Middle);
        InputHelper.MouseMoveTo(ZoomBorderTestHelper.ToWindow(zoomBorder, new Point(60, 60)), 2);
        await ZoomBorderTestHelper.WaitForIdleAsync();
        Assert.IsTrue(zoomBorder.IsPanning, "The mouse pan should be active");

        TouchTestHelpers.PinchScale(zoomBorder, 1.5, new Point(250, 150));
        await ZoomBorderTestHelper.WaitForIdleAsync();
        InputHelper.MouseUp(ButtonName.Middle);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.AreEqual(initialZoom, zoomBorder.ZoomX, 1e-9, "Pinch must be blocked while panning");
    }

    /// <summary>
    /// Verifies that touch point limits are checked in gesture handlers.
    /// MinimumTouchPoints > 2 blocks pinch (2-finger) gestures.
    /// MaximumTouchPoints &lt; 2 also blocks pinch gestures.
    /// </summary>
    [TestMethod]
    public void TouchPointLimits_EnforcedInGestureHandlers()
    {
        // Setting MinimumTouchPoints > 2 will block pinch gestures
        var zoomBorderMinTooHigh = new ZoomBorder
        {
            MinimumTouchPoints = 3 // Requires 3 fingers, but pinch uses 2
        };

        Assert.AreEqual(3, zoomBorderMinTooHigh.MinimumTouchPoints);

        // Setting MaximumTouchPoints < 2 will also block pinch gestures
        var zoomBorderMaxTooLow = new ZoomBorder
        {
            MaximumTouchPoints = 1 // Allows only 1 finger, but pinch uses 2
        };

        Assert.AreEqual(1, zoomBorderMaxTooLow.MaximumTouchPoints);

        // The implementation checks: if (MinimumTouchPoints > 2 || MaximumTouchPoints < 2) return;
        // (verified with real touch in the PinchGesture_* and ScrollGesture_* tests below)
    }

    /// <summary>
    /// Verifies that GestureRecognitionDelay property is implemented and respects the delay setting.
    /// </summary>
    [TestMethod]
    public void GestureRecognitionDelay_WhenPositive_DelaysGestureRecognition()
    {
        var zoomBorder = new ZoomBorder
        {
            GestureRecognitionDelay = TimeSpan.FromMilliseconds(100)
        };

        Assert.AreEqual(TimeSpan.FromMilliseconds(100), zoomBorder.GestureRecognitionDelay);

        // The implementation:
        // 1. Records _gestureStartTime on first gesture event
        // 2. Returns early while (DateTime.Now - _gestureStartTime) < GestureRecognitionDelay
        // 3. Sets _gestureRecognized = true and processes gesture after delay expires
    }

    #region Pinch Gesture Touch Point Limit Tests

    [TestMethod]
    public async Task PinchGesture_MinimumTouchPointsTooHigh_GestureNotProcessed()
    {
        // MinimumTouchPoints > 2 should block pinch gestures (pinch uses 2 fingers)
        var (zoomBorder, _) = await CreateAsync(zb =>
        {
            zb.EnableGestures = true;
            zb.EnableGestureZoom = true;
            zb.MinimumTouchPoints = 3;
        });
        var gestures = TrackGestures(zoomBorder);
        var initialZoomX = zoomBorder.ZoomX;

        // Uno: real two finger pinch instead of a synthetic PinchEvent
        TouchTestHelpers.PinchScale(zoomBorder, 1.5, TouchTestHelpers.Center(zoomBorder));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.AreEqual(initialZoomX, zoomBorder.ZoomX);
        // Uno: "event not handled" is verified as "gesture not processed" (no Pinch GestureStarted)
        Assert.IsFalse(gestures.Contains("Pinch"), "Pinch should NOT be processed when MinimumTouchPoints > 2");
    }

    [TestMethod]
    public async Task PinchGesture_MaximumTouchPointsTooLow_GestureNotProcessed()
    {
        // MaximumTouchPoints < 2 should block pinch gestures (pinch uses 2 fingers)
        var (zoomBorder, _) = await CreateAsync(zb =>
        {
            zb.EnableGestures = true;
            zb.EnableGestureZoom = true;
            zb.MaximumTouchPoints = 1;
        });
        var gestures = TrackGestures(zoomBorder);
        var initialZoomX = zoomBorder.ZoomX;

        // Uno: real two finger pinch instead of a synthetic PinchEvent
        TouchTestHelpers.PinchScale(zoomBorder, 1.5, TouchTestHelpers.Center(zoomBorder));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.AreEqual(initialZoomX, zoomBorder.ZoomX);
        // Uno: "event not handled" is verified as "gesture not processed" (no Pinch GestureStarted)
        Assert.IsFalse(gestures.Contains("Pinch"), "Pinch should NOT be processed when MaximumTouchPoints < 2");
    }

    [TestMethod]
    public async Task PinchGesture_TouchPointLimitsInclude2_GestureProcessed()
    {
        // When touch point limits include 2, pinch gestures should work
        var (zoomBorder, _) = await CreateAsync(zb =>
        {
            zb.EnableGestures = true;
            zb.EnableGestureZoom = true;
            zb.MinimumTouchPoints = 1;
            zb.MaximumTouchPoints = 3;
        });
        var gestures = TrackGestures(zoomBorder);
        var initialZoomX = zoomBorder.ZoomX;

        // Uno: real two finger pinch instead of a synthetic PinchEvent
        TouchTestHelpers.PinchScale(zoomBorder, 1.5, TouchTestHelpers.Center(zoomBorder));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(zoomBorder.ZoomX > initialZoomX, "Zoom should increase when touch point limits include 2");
        // Uno: "event handled" is verified as "gesture processed" (Pinch GestureStarted raised)
        Assert.IsTrue(gestures.Contains("Pinch"), "Pinch should be processed when touch point limits include 2");
    }

    #endregion

    #region Scroll Gesture Touch Point Limit Tests

    [TestMethod]
    public async Task ScrollGesture_MinimumTouchPointsTooHigh_GestureNotProcessed()
    {
        // MinimumTouchPoints > 2 should block scroll gestures
        var (zoomBorder, _) = await CreateAsync(zb =>
        {
            zb.EnableGestures = true;
            zb.EnableGestureTranslation = true;
            zb.MinimumTouchPoints = 3;
        });
        var gestures = TrackGestures(zoomBorder);
        var initialOffsetX = zoomBorder.OffsetX;

        // Uno: real one finger drag (manipulation translation) instead of a synthetic ScrollGestureEvent
        TouchTestHelpers.ScrollGesture(zoomBorder, 50, 0);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.AreEqual(initialOffsetX, zoomBorder.OffsetX);
        // Uno: "event not handled" is verified as "gesture not processed" (no Scroll GestureStarted)
        Assert.IsFalse(gestures.Contains("Scroll"), "Scroll should NOT be processed when MinimumTouchPoints > 2");
    }

    [TestMethod]
    public async Task ScrollGesture_MaximumTouchPointsTooLow_GestureNotProcessed()
    {
        // MaximumTouchPoints < 2 should block scroll gestures
        var (zoomBorder, _) = await CreateAsync(zb =>
        {
            zb.EnableGestures = true;
            zb.EnableGestureTranslation = true;
            zb.MaximumTouchPoints = 1;
        });
        var gestures = TrackGestures(zoomBorder);
        var initialOffsetX = zoomBorder.OffsetX;

        // Uno: real one finger drag (manipulation translation) instead of a synthetic ScrollGestureEvent
        TouchTestHelpers.ScrollGesture(zoomBorder, 50, 0);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.AreEqual(initialOffsetX, zoomBorder.OffsetX);
        // Uno: "event not handled" is verified as "gesture not processed" (no Scroll GestureStarted)
        Assert.IsFalse(gestures.Contains("Scroll"), "Scroll should NOT be processed when MaximumTouchPoints < 2");
    }

    [TestMethod]
    public async Task ScrollGesture_TouchPointLimitsInclude2_GestureProcessed()
    {
        // When touch point limits include 2, scroll gestures should work
        var (zoomBorder, _) = await CreateAsync(zb =>
        {
            zb.EnableGestures = true;
            zb.EnableGestureTranslation = true;
            zb.MinimumTouchPoints = 1;
            zb.MaximumTouchPoints = 3;
        });
        var gestures = TrackGestures(zoomBorder);
        var initialOffsetX = zoomBorder.OffsetX;

        // Uno: real one finger drag (manipulation translation) instead of a synthetic ScrollGestureEvent
        TouchTestHelpers.ScrollGesture(zoomBorder, 50, 0);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.AreNotEqual(initialOffsetX, zoomBorder.OffsetX);
        // Uno: "event handled" is verified as "gesture processed" (Scroll GestureStarted raised)
        Assert.IsTrue(gestures.Contains("Scroll"), "Scroll should be processed when touch point limits include 2");
    }

    #endregion

    #region GestureRecognitionDelay Tests

    [TestMethod]
    public async Task GestureRecognitionDelay_WithPositiveDelay_FirstGestureIsIgnored()
    {
        var (zoomBorder, _) = await CreateAsync(zb =>
        {
            zb.EnableGestures = true;
            zb.EnableGestureZoom = true;
            zb.GestureRecognitionDelay = TimeSpan.FromMilliseconds(500);
        });
        var gestures = TrackGestures(zoomBorder);
        var initialZoomX = zoomBorder.ZoomX;

        // Uno: a real pinch is injected in far less than 500ms, so every update arrives before the delay elapses
        TouchTestHelpers.PinchScale(zoomBorder, 1.5, TouchTestHelpers.Center(zoomBorder));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.AreEqual(initialZoomX, zoomBorder.ZoomX);
        // Uno: "event not handled" is verified as "gesture not processed" (no Pinch GestureStarted)
        Assert.IsFalse(gestures.Contains("Pinch"), "Pinch gesture should be ignored due to recognition delay");
    }

    [TestMethod]
    public async Task GestureRecognitionDelay_WhenZero_GestureProcessedImmediately()
    {
        var (zoomBorder, _) = await CreateAsync(zb =>
        {
            zb.EnableGestures = true;
            zb.EnableGestureZoom = true;
            zb.GestureRecognitionDelay = TimeSpan.Zero;
        });
        var gestures = TrackGestures(zoomBorder);
        var initialZoomX = zoomBorder.ZoomX;

        // Uno: real two finger pinch instead of a synthetic PinchEvent
        TouchTestHelpers.PinchScale(zoomBorder, 1.5, TouchTestHelpers.Center(zoomBorder));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(zoomBorder.ZoomX > initialZoomX, "Pinch gesture should be processed immediately with zero delay");
        // Uno: "event handled" is verified as "gesture processed" (Pinch GestureStarted raised)
        Assert.IsTrue(gestures.Contains("Pinch"), "Pinch should be processed with zero delay");
    }

    #endregion

    #region EnableSimultaneousPanZoom Tests

    [TestMethod]
    public async Task EnableSimultaneousPanZoom_DefaultTrue_AllowsSimultaneousGestures()
    {
        var (zoomBorder, _) = await CreateAsync(zb =>
        {
            zb.EnableGestures = true;
            zb.EnableGestureZoom = true;
            zb.EnableGestureTranslation = true;
            zb.EnableSimultaneousPanZoom = true; // default
        });

        Assert.IsTrue(zoomBorder.EnableSimultaneousPanZoom);

        // Uno adaptation: additionally verify the behavior with a real pinch while a real mouse pan is active.
        var initialZoom = zoomBorder.ZoomX;
        InputHelper.MouseMoveTo(ZoomBorderTestHelper.ToWindow(zoomBorder, new Point(50, 50)));
        InputHelper.MouseDown(ButtonName.Middle);
        InputHelper.MouseMoveTo(ZoomBorderTestHelper.ToWindow(zoomBorder, new Point(60, 60)), 2);
        await ZoomBorderTestHelper.WaitForIdleAsync();
        Assert.IsTrue(zoomBorder.IsPanning, "The mouse pan should be active");

        TouchTestHelpers.PinchScale(zoomBorder, 1.5, new Point(250, 150));
        await ZoomBorderTestHelper.WaitForIdleAsync();
        InputHelper.MouseUp(ButtonName.Middle);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(zoomBorder.ZoomX > initialZoom, "Pinch should zoom while panning when simultaneous pan/zoom is enabled");
    }

    [TestMethod]
    public void EnableSimultaneousPanZoom_WhenFalse_PropertyIsStored()
    {
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableSimultaneousPanZoom = false
        };

        Assert.IsFalse(zoomBorder.EnableSimultaneousPanZoom);
    }

    #endregion

    #region Property Validation Tests

    [TestMethod]
    public void MinimumTouchPoints_ValidRange_AcceptsValue()
    {
        var zoomBorder = new ZoomBorder();

        zoomBorder.MinimumTouchPoints = 0;
        Assert.AreEqual(0, zoomBorder.MinimumTouchPoints);

        zoomBorder.MinimumTouchPoints = 1;
        Assert.AreEqual(1, zoomBorder.MinimumTouchPoints);

        zoomBorder.MinimumTouchPoints = 5;
        Assert.AreEqual(5, zoomBorder.MinimumTouchPoints);

        zoomBorder.MinimumTouchPoints = 10;
        Assert.AreEqual(10, zoomBorder.MinimumTouchPoints);
    }

    [TestMethod]
    public void MaximumTouchPoints_ValidRange_AcceptsValue()
    {
        var zoomBorder = new ZoomBorder();

        zoomBorder.MaximumTouchPoints = 1;
        Assert.AreEqual(1, zoomBorder.MaximumTouchPoints);

        zoomBorder.MaximumTouchPoints = 2;
        Assert.AreEqual(2, zoomBorder.MaximumTouchPoints);

        zoomBorder.MaximumTouchPoints = 5;
        Assert.AreEqual(5, zoomBorder.MaximumTouchPoints);

        zoomBorder.MaximumTouchPoints = 10;
        Assert.AreEqual(10, zoomBorder.MaximumTouchPoints);
    }

    [TestMethod]
    public void GestureRecognitionDelay_NegativeValue_AcceptsValue()
    {
        var zoomBorder = new ZoomBorder();

        // Set negative value (treated as zero effectively in the implementation)
        zoomBorder.GestureRecognitionDelay = TimeSpan.FromMilliseconds(-100);

        Assert.AreEqual(TimeSpan.FromMilliseconds(-100), zoomBorder.GestureRecognitionDelay);
    }

    [TestMethod]
    public void GestureRecognitionDelay_LargeValue_AcceptsValue()
    {
        var zoomBorder = new ZoomBorder();

        zoomBorder.GestureRecognitionDelay = TimeSpan.FromSeconds(10);

        Assert.AreEqual(TimeSpan.FromSeconds(10), zoomBorder.GestureRecognitionDelay);
    }

    #endregion
}

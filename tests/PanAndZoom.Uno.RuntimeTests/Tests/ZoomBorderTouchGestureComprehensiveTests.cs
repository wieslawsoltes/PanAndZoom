// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using Windows.System;

namespace PanAndZoom.Uno.RuntimeTests.Tests;

/// <summary>
/// Comprehensive touch gesture tests for ZoomBorder: pinch-to-zoom, scroll (pan) gestures, touchpad
/// input, multi-touch scenarios and gesture events.
/// </summary>
/// <remarks>
/// The Avalonia tests use the headless <c>TouchInputSimulator</c> that raises synthetic gesture events.
/// On Uno the gestures are produced by real injected touch input: two fingers pinch (and rotate), one
/// finger scrolls. A synthetic <c>PinchGesture(scale, origin)</c> becomes a real pinch whose finger distance
/// changes by <c>scale</c>, a synthetic <c>ScrollGesture(delta)</c> becomes a one finger drag by <c>-delta</c>
/// (content follows the finger) and the "gesture ended" events are raised when the fingers are lifted.
/// </remarks>
[TestClass]
[RunsOnUIThread]
public class ZoomBorderTouchGestureComprehensiveTests
{
    [TestCleanup]
    public async Task Cleanup()
    {
        InputHelper.Reset();
        await ZoomBorderTestHelper.UnloadAsync();
    }

    #region Helper Methods

    private static async Task<ZoomBorder> CreateZoomBorderAsync(
        double width = 400,
        double height = 300,
        double childWidth = 200,
        double childHeight = 150,
        bool enableGestures = true,
        bool enableGestureZoom = true,
        bool enableGestureTranslation = true,
        bool enableGestureRotation = true,
        Action<ZoomBorder>? configure = null)
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(width, height, childWidth, childHeight, zb =>
        {
            zb.EnableGestures = enableGestures;
            zb.EnableGestureZoom = enableGestureZoom;
            zb.EnableGestureTranslation = enableGestureTranslation;
            zb.EnableGestureRotation = enableGestureRotation;
            configure?.Invoke(zb);
        });
        return zoomBorder;
    }

    private static Point Center(ZoomBorder zoomBorder) => TouchTestHelpers.Center(zoomBorder);

    private static async Task PinchAsync(ZoomBorder zoomBorder, double scale, Point? center = null, double startDistance = 100)
    {
        TouchTestHelpers.PinchScale(zoomBorder, scale, center ?? Center(zoomBorder), startDistance);
        await ZoomBorderTestHelper.WaitForIdleAsync();
    }

    private static async Task ScrollAsync(ZoomBorder zoomBorder, double deltaX, double deltaY)
    {
        TouchTestHelpers.ScrollGesture(zoomBorder, deltaX, deltaY);
        await ZoomBorderTestHelper.WaitForIdleAsync();
    }

    private static void AssertPoint(Point expected, Point actual)
    {
        Assert.AreEqual(expected.X, actual.X, 1.0, $"X expected {expected} actual {actual}");
        Assert.AreEqual(expected.Y, actual.Y, 1.0, $"Y expected {expected} actual {actual}");
    }

    #endregion

    #region Pinch Gesture - Basic Zoom Tests

    [TestMethod]
    public async Task PinchGesture_ZoomIn_IncreasesZoomFromDefaultValue()
    {
        var zoomBorder = await CreateZoomBorderAsync();
        var initialZoomX = zoomBorder.ZoomX;
        var initialZoomY = zoomBorder.ZoomY;

        await PinchAsync(zoomBorder, 2.0);

        Assert.IsTrue(zoomBorder.ZoomX > initialZoomX, $"ZoomX should increase from {initialZoomX}, was {zoomBorder.ZoomX}");
        Assert.IsTrue(zoomBorder.ZoomY > initialZoomY, $"ZoomY should increase from {initialZoomY}, was {zoomBorder.ZoomY}");
    }

    [TestMethod]
    public async Task PinchGesture_ZoomOut_DecreasesZoom()
    {
        var zoomBorder = await CreateZoomBorderAsync();
        zoomBorder.ZoomTo(2.0, 100, 75);
        var initialZoomX = zoomBorder.ZoomX;

        await PinchAsync(zoomBorder, 0.5);

        Assert.IsTrue(zoomBorder.ZoomX < initialZoomX, $"ZoomX should decrease from {initialZoomX}, was {zoomBorder.ZoomX}");
    }

    [TestMethod]
    public async Task PinchGesture_MultipleSequentialZooms_AccumulatesZoom()
    {
        var zoomBorder = await CreateZoomBorderAsync();
        var pinchCenter = new Point(200, 150);
        var zoomHistory = new List<double> { zoomBorder.ZoomX };

        // Multiple zoom in gestures (each real pinch ends when the fingers are lifted)
        for (var i = 0; i < 5; i++)
        {
            await PinchAsync(zoomBorder, 1.2, pinchCenter);
            zoomHistory.Add(zoomBorder.ZoomX);
        }

        for (var i = 1; i < zoomHistory.Count; i++)
        {
            Assert.IsTrue(zoomHistory[i] > zoomHistory[i - 1],
                $"Zoom at step {i} ({zoomHistory[i]}) should be greater than step {i - 1} ({zoomHistory[i - 1]})");
        }
    }

    [TestMethod]
    public async Task PinchGesture_ZoomAtDifferentOrigins_AffectsOffset()
    {
        var zoomBorder = await CreateZoomBorderAsync();
        var topLeft = new Point(50, 50);
        var center = new Point(200, 150);
        var bottomRight = new Point(350, 250);

        // Uno: offsets are read after each real pinch (a 40px start distance keeps the fingers inside the control)
        await PinchAsync(zoomBorder, 1.5, topLeft, 40);
        var offsetAfterTopLeft = (zoomBorder.OffsetX, zoomBorder.OffsetY);
        zoomBorder.ResetMatrix();

        await PinchAsync(zoomBorder, 1.5, center, 40);
        var offsetAfterCenter = (zoomBorder.OffsetX, zoomBorder.OffsetY);
        zoomBorder.ResetMatrix();

        await PinchAsync(zoomBorder, 1.5, bottomRight, 40);
        var offsetAfterBottomRight = (zoomBorder.OffsetX, zoomBorder.OffsetY);

        Assert.AreNotEqual(offsetAfterTopLeft, offsetAfterCenter);
        Assert.AreNotEqual(offsetAfterCenter, offsetAfterBottomRight);
    }

    [TestMethod]
    public async Task PinchGesture_RespectsMinZoom()
    {
        var zoomBorder = await CreateZoomBorderAsync(configure: zb =>
        {
            zb.MinZoomX = 0.5;
            zb.MinZoomY = 0.5;
        });

        // Try to zoom out beyond minimum (each real pinch is a complete gesture, so the zoom out accumulates)
        for (var i = 0; i < 10; i++)
        {
            await PinchAsync(zoomBorder, 0.5);
        }

        Assert.IsTrue(zoomBorder.ZoomX >= 0.5 - 1e-9, $"ZoomX should not go below 0.5, was {zoomBorder.ZoomX}");
        Assert.IsTrue(zoomBorder.ZoomY >= 0.5 - 1e-9, $"ZoomY should not go below 0.5, was {zoomBorder.ZoomY}");
    }

    [TestMethod]
    public async Task PinchGesture_RespectsMaxZoom()
    {
        var zoomBorder = await CreateZoomBorderAsync(configure: zb =>
        {
            zb.MaxZoomX = 5.0;
            zb.MaxZoomY = 5.0;
        });

        // Try to zoom in beyond maximum (each real pinch is a complete gesture, so the zoom in accumulates)
        for (var i = 0; i < 20; i++)
        {
            await PinchAsync(zoomBorder, 2.0);
        }

        Assert.IsTrue(zoomBorder.ZoomX <= 5.0 + 1e-9, $"ZoomX should not exceed 5.0, was {zoomBorder.ZoomX}");
        Assert.IsTrue(zoomBorder.ZoomY <= 5.0 + 1e-9, $"ZoomY should not exceed 5.0, was {zoomBorder.ZoomY}");
    }

    #endregion

    #region Pinch Gesture - Enable/Disable Tests

    [TestMethod]
    public async Task PinchGesture_WhenGesturesDisabled_DoesNotZoom()
    {
        var zoomBorder = await CreateZoomBorderAsync(enableGestures: false);
        var initialZoomX = zoomBorder.ZoomX;
        var initialZoomY = zoomBorder.ZoomY;

        await PinchAsync(zoomBorder, 2.0);

        Assert.AreEqual(initialZoomX, zoomBorder.ZoomX);
        Assert.AreEqual(initialZoomY, zoomBorder.ZoomY);
    }

    [TestMethod]
    public async Task PinchGesture_WhenGestureZoomDisabled_DoesNotZoom()
    {
        var zoomBorder = await CreateZoomBorderAsync(enableGestureZoom: false);
        var initialZoomX = zoomBorder.ZoomX;
        var initialZoomY = zoomBorder.ZoomY;

        await PinchAsync(zoomBorder, 2.0);

        Assert.AreEqual(initialZoomX, zoomBorder.ZoomX, 1e-9);
        Assert.AreEqual(initialZoomY, zoomBorder.ZoomY, 1e-9);
    }

    [TestMethod]
    public async Task PinchGesture_WhenReEnabled_ZoomsAgain()
    {
        var zoomBorder = await CreateZoomBorderAsync(enableGestureZoom: false);

        await PinchAsync(zoomBorder, 2.0);
        var zoomWhileDisabled = zoomBorder.ZoomX;

        zoomBorder.EnableGestureZoom = true;

        await PinchAsync(zoomBorder, 2.0);

        Assert.IsTrue(zoomBorder.ZoomX > zoomWhileDisabled, "Zoom should work after re-enabling");
    }

    [TestMethod]
    public async Task PinchGesture_WithNoChild_DoesNotCrash()
    {
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            EnableGestures = true,
            EnableGestureZoom = true
        };
        // No child set
        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        // Should not throw (real pinch on the transparent background of the control)
        var exception = TouchTestHelpers.Record(() =>
            TouchTestHelpers.PinchScale(zoomBorder, 2.0, Center(zoomBorder)));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsNull(exception);
    }

    #endregion

    #region Scroll Gesture - Pan Tests

    [TestMethod]
    public async Task ScrollGesture_PanRight_IncreasesOffsetX()
    {
        var zoomBorder = await CreateZoomBorderAsync();
        var initialOffsetX = zoomBorder.OffsetX;

        // Scroll gesture with negative X moves content right (the finger drags right)
        await ScrollAsync(zoomBorder, -50, 0);

        Assert.IsTrue(zoomBorder.OffsetX > initialOffsetX,
            $"OffsetX should increase, was {initialOffsetX} now {zoomBorder.OffsetX}");
    }

    [TestMethod]
    public async Task ScrollGesture_PanLeft_DecreasesOffsetX()
    {
        var zoomBorder = await CreateZoomBorderAsync();
        zoomBorder.Pan(100, 0); // Start with some offset
        var initialOffsetX = zoomBorder.OffsetX;

        // Scroll gesture with positive X moves content left (the finger drags left)
        await ScrollAsync(zoomBorder, 50, 0);

        Assert.IsTrue(zoomBorder.OffsetX < initialOffsetX,
            $"OffsetX should decrease from {initialOffsetX}, was {zoomBorder.OffsetX}");
    }

    [TestMethod]
    public async Task ScrollGesture_PanDown_IncreasesOffsetY()
    {
        var zoomBorder = await CreateZoomBorderAsync();
        var initialOffsetY = zoomBorder.OffsetY;

        // Scroll gesture with negative Y moves content down (the finger drags down)
        await ScrollAsync(zoomBorder, 0, -50);

        Assert.IsTrue(zoomBorder.OffsetY > initialOffsetY,
            $"OffsetY should increase, was {initialOffsetY} now {zoomBorder.OffsetY}");
    }

    [TestMethod]
    public async Task ScrollGesture_PanUp_DecreasesOffsetY()
    {
        var zoomBorder = await CreateZoomBorderAsync();
        zoomBorder.Pan(0, 100); // Start with some offset
        var initialOffsetY = zoomBorder.OffsetY;

        // Scroll gesture with positive Y moves content up (the finger drags up)
        await ScrollAsync(zoomBorder, 0, 50);

        Assert.IsTrue(zoomBorder.OffsetY < initialOffsetY,
            $"OffsetY should decrease from {initialOffsetY}, was {zoomBorder.OffsetY}");
    }

    [TestMethod]
    public async Task ScrollGesture_DiagonalPan_ChangesBothOffsets()
    {
        var zoomBorder = await CreateZoomBorderAsync();
        var initialOffsetX = zoomBorder.OffsetX;
        var initialOffsetY = zoomBorder.OffsetY;

        await ScrollAsync(zoomBorder, -30, -40);

        Assert.AreNotEqual(initialOffsetX, zoomBorder.OffsetX);
        Assert.AreNotEqual(initialOffsetY, zoomBorder.OffsetY);
    }

    [TestMethod]
    public async Task ScrollGesture_MultipleGestures_AccumulatesPan()
    {
        var zoomBorder = await CreateZoomBorderAsync();
        var offsetHistory = new List<double> { zoomBorder.OffsetX };

        // Multiple pan gestures to the right
        for (var i = 0; i < 5; i++)
        {
            await ScrollAsync(zoomBorder, -20, 0);
            offsetHistory.Add(zoomBorder.OffsetX);
        }

        for (var i = 1; i < offsetHistory.Count; i++)
        {
            Assert.IsTrue(offsetHistory[i] > offsetHistory[i - 1],
                $"OffsetX at step {i} ({offsetHistory[i]}) should be greater than step {i - 1} ({offsetHistory[i - 1]})");
        }
    }

    [TestMethod]
    public async Task ScrollGesture_WhenTranslationDisabled_DoesNotPan()
    {
        var zoomBorder = await CreateZoomBorderAsync(enableGestureTranslation: false);
        var initialOffsetX = zoomBorder.OffsetX;
        var initialOffsetY = zoomBorder.OffsetY;

        await ScrollAsync(zoomBorder, 50, 50);

        Assert.AreEqual(initialOffsetX, zoomBorder.OffsetX);
        Assert.AreEqual(initialOffsetY, zoomBorder.OffsetY);
    }

    [TestMethod]
    public async Task ScrollGesture_WithZoom_PansCorrectly()
    {
        var zoomBorder = await CreateZoomBorderAsync();
        zoomBorder.ZoomTo(2.0, 100, 75);
        var initialOffsetX = zoomBorder.OffsetX;

        await ScrollAsync(zoomBorder, -50, 0);

        Assert.AreNotEqual(initialOffsetX, zoomBorder.OffsetX);
    }

    #endregion

    #region Touch Point Limits Tests

    [TestMethod]
    public async Task PinchGesture_WhenMinTouchPointsGreaterThan2_DoesNotZoom()
    {
        var zoomBorder = await CreateZoomBorderAsync(configure: zb => zb.MinimumTouchPoints = 3);
        var initialZoom = zoomBorder.ZoomX;

        // Pinch uses 2 touch points
        await PinchAsync(zoomBorder, 2.0);

        Assert.AreEqual(initialZoom, zoomBorder.ZoomX);
    }

    [TestMethod]
    public async Task PinchGesture_WhenMaxTouchPointsLessThan2_DoesNotZoom()
    {
        var zoomBorder = await CreateZoomBorderAsync(configure: zb => zb.MaximumTouchPoints = 1);
        var initialZoom = zoomBorder.ZoomX;

        // Pinch uses 2 touch points
        await PinchAsync(zoomBorder, 2.0);

        Assert.AreEqual(initialZoom, zoomBorder.ZoomX);
    }

    [TestMethod]
    public async Task ScrollGesture_WhenMinTouchPointsGreaterThan2_DoesNotPan()
    {
        var zoomBorder = await CreateZoomBorderAsync(configure: zb => zb.MinimumTouchPoints = 3);
        var initialOffset = zoomBorder.OffsetX;

        await ScrollAsync(zoomBorder, 50, 0);

        Assert.AreEqual(initialOffset, zoomBorder.OffsetX);
    }

    [TestMethod]
    public async Task PinchGesture_WithDefaultTouchPointLimits_Zooms()
    {
        // Default: MinimumTouchPoints = 1, MaximumTouchPoints = 2
        var zoomBorder = await CreateZoomBorderAsync();
        var initialZoom = zoomBorder.ZoomX;

        await PinchAsync(zoomBorder, 1.5);

        Assert.IsTrue(zoomBorder.ZoomX > initialZoom);
    }

    #endregion

    #region Simultaneous Pan/Zoom Tests

    [TestMethod]
    public async Task SimultaneousPanZoom_WhenEnabled_AllowsBothGestures()
    {
        var zoomBorder = await CreateZoomBorderAsync(configure: zb => zb.EnableSimultaneousPanZoom = true);
        var initialZoom = zoomBorder.ZoomX;
        var initialOffset = zoomBorder.OffsetX;

        await PinchAsync(zoomBorder, 1.5);
        var zoomAfterPinch = zoomBorder.ZoomX;

        await ScrollAsync(zoomBorder, -30, 0);
        var offsetAfterScroll = zoomBorder.OffsetX;

        Assert.IsTrue(zoomAfterPinch > initialZoom, "Zoom should have increased");
        Assert.AreNotEqual(initialOffset, offsetAfterScroll);
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

    #endregion

    #region Gesture Recognition Delay Tests

    [TestMethod]
    public void GestureRecognitionDelay_DefaultValue_IsZero()
    {
        var zoomBorder = new ZoomBorder();

        Assert.AreEqual(TimeSpan.Zero, zoomBorder.GestureRecognitionDelay);
    }

    [TestMethod]
    public void GestureRecognitionDelay_CanBeSet()
    {
        var zoomBorder = new ZoomBorder();
        var delay = TimeSpan.FromMilliseconds(100);

        zoomBorder.GestureRecognitionDelay = delay;

        Assert.AreEqual(delay, zoomBorder.GestureRecognitionDelay);
    }

    #endregion

    #region Gesture Events Tests

    [TestMethod]
    public async Task PinchGesture_RaisesGestureStartedEvent()
    {
        var zoomBorder = await CreateZoomBorderAsync();
        var received = new List<GestureEventArgs>();
        zoomBorder.GestureStarted += (_, args) => received.Add(args);

        await PinchAsync(zoomBorder, 1.5);

        // Uno: a real pinch can also report a small centroid translation ("Scroll"), so check that a Pinch was raised
        Assert.IsTrue(received.Count > 0);
        Assert.IsTrue(received.Any(a => a.GestureType == "Pinch"));
    }

    [TestMethod]
    public async Task PinchGestureEnded_RaisesGestureEndedEvent()
    {
        var zoomBorder = await CreateZoomBorderAsync();
        var received = new List<GestureEventArgs>();
        zoomBorder.GestureEnded += (_, args) => received.Add(args);

        // Uno: the pinch ends when the fingers are lifted (a "Scroll" end may follow for the centroid translation)
        await PinchAsync(zoomBorder, 1.5);

        Assert.IsTrue(received.Count > 0);
        Assert.IsTrue(received.Any(a => a.GestureType == "Pinch"));
    }

    [TestMethod]
    public async Task ScrollGesture_RaisesGestureStartedEvent()
    {
        var zoomBorder = await CreateZoomBorderAsync();
        GestureEventArgs? receivedArgs = null;
        zoomBorder.GestureStarted += (_, args) => receivedArgs = args;

        await ScrollAsync(zoomBorder, 30, 20);

        Assert.IsNotNull(receivedArgs);
        Assert.AreEqual("Scroll", receivedArgs!.GestureType);
    }

    [TestMethod]
    public async Task ScrollGestureEnded_RaisesGestureEndedEvent()
    {
        var zoomBorder = await CreateZoomBorderAsync();
        GestureEventArgs? receivedArgs = null;
        zoomBorder.GestureEnded += (_, args) => receivedArgs = args;

        // Uno: the scroll ends when the finger is lifted
        await ScrollAsync(zoomBorder, 30, 20);

        Assert.IsNotNull(receivedArgs);
        Assert.AreEqual("Scroll", receivedArgs!.GestureType);
    }

    [TestMethod]
    public async Task GestureEvents_ProvideCorrectZoomValues()
    {
        var zoomBorder = await CreateZoomBorderAsync();
        GestureEventArgs? receivedArgs = null;
        zoomBorder.GestureStarted += (_, args) => receivedArgs = args;

        await PinchAsync(zoomBorder, 1.5);

        Assert.IsNotNull(receivedArgs);
        Assert.IsTrue(receivedArgs!.ZoomX > 0);
        Assert.IsTrue(receivedArgs!.ZoomY > 0);
    }

    [TestMethod]
    public async Task GestureEvents_ProvideCorrectOffsetValues()
    {
        var zoomBorder = await CreateZoomBorderAsync();
        zoomBorder.Pan(50, 30);
        var offsetX = zoomBorder.OffsetX;
        var offsetY = zoomBorder.OffsetY;
        var received = new List<GestureEventArgs>();
        zoomBorder.GestureStarted += (_, args) => received.Add(args);

        // Uno: a (10, 10) finger move stays below the WinUI touch manipulation start threshold (it is a tap), so a 20px move is used
        await ScrollAsync(zoomBorder, 20, 20);

        Assert.IsTrue(received.Count > 0);
        // The event args should contain current offset values (the first update reports the offset before panning)
        Assert.AreEqual(offsetX, received[0].OffsetX, 1e-9);
        Assert.AreEqual(offsetY, received[0].OffsetY, 1e-9);
    }

    #endregion

    #region Touchpad Magnify Tests

    // Uno adaptation: WinUI has no touchpad magnify event; a precision touchpad pinch is delivered to apps as
    // Ctrl + mouse wheel, so the touchpad magnify tests inject a real Ctrl + wheel.

    [TestMethod]
    public async Task TouchpadMagnify_ZoomIn_DoesNotCrash()
    {
        var zoomBorder = await CreateZoomBorderAsync();

        var exception = TouchTestHelpers.Record(() =>
            InputHelper.MouseWheel(ZoomBorderTestHelper.ToWindow(zoomBorder, new Point(200, 150)), 36, VirtualKeyModifiers.Control));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsNull(exception);
    }

    [TestMethod]
    public async Task TouchpadMagnify_ZoomOut_DoesNotCrash()
    {
        var zoomBorder = await CreateZoomBorderAsync();
        zoomBorder.ZoomTo(2.0, 100, 75);

        var exception = TouchTestHelpers.Record(() =>
            InputHelper.MouseWheel(ZoomBorderTestHelper.ToWindow(zoomBorder, new Point(200, 150)), -36, VirtualKeyModifiers.Control));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsNull(exception);
    }

    [TestMethod]
    public async Task TouchpadMagnify_WithKeyModifiers_DoesNotCrash()
    {
        var zoomBorder = await CreateZoomBorderAsync();
        var position = ZoomBorderTestHelper.ToWindow(zoomBorder, new Point(200, 150));

        // Should not throw with various modifiers (combined with the Ctrl of the touchpad pinch)
        var exception = TouchTestHelpers.Record(() =>
        {
            InputHelper.MouseWheel(position, 24, VirtualKeyModifiers.Control);
            InputHelper.MouseWheel(position, 24, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift);
            InputHelper.MouseWheel(position, 24, VirtualKeyModifiers.Control | VirtualKeyModifiers.Menu);
        });
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsNull(exception);
    }

    #endregion

    #region Touchpad Swipe Tests

    // Uno adaptation: WinUI has no touchpad swipe event; two finger touchpad scrolling is delivered as
    // (horizontal) mouse wheel input, so the touchpad swipe tests inject real wheel input.

    [TestMethod]
    public async Task TouchpadSwipe_Horizontal_DoesNotCrash()
    {
        var zoomBorder = await CreateZoomBorderAsync();

        var exception = TouchTestHelpers.Record(() =>
            InputHelper.MouseWheel(ZoomBorderTestHelper.ToWindow(zoomBorder, new Point(200, 150)), 50, horizontal: true));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsNull(exception);
    }

    [TestMethod]
    public async Task TouchpadSwipe_Vertical_DoesNotCrash()
    {
        var zoomBorder = await CreateZoomBorderAsync();

        var exception = TouchTestHelpers.Record(() =>
            InputHelper.MouseWheel(ZoomBorderTestHelper.ToWindow(zoomBorder, new Point(200, 150)), 50));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsNull(exception);
    }

    [TestMethod]
    public async Task TouchpadSwipe_Diagonal_DoesNotCrash()
    {
        var zoomBorder = await CreateZoomBorderAsync();
        var position = ZoomBorderTestHelper.ToWindow(zoomBorder, new Point(200, 150));

        var exception = TouchTestHelpers.Record(() =>
        {
            InputHelper.MouseWheel(position, 30, horizontal: true);
            InputHelper.MouseWheel(position, 40);
        });
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsNull(exception);
    }

    #endregion

    #region Touchpad Rotate Tests

    // Uno adaptation: WinUI has no touchpad rotate event; the closest real input is a two finger touch rotation.

    [TestMethod]
    public async Task TouchpadRotate_Clockwise_DoesNotCrash()
    {
        var zoomBorder = await CreateZoomBorderAsync();

        var exception = TouchTestHelpers.Record(() =>
            TouchTestHelpers.Rotate(zoomBorder, new Point(200, 150), 50, 0, 45));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsNull(exception);
    }

    [TestMethod]
    public async Task TouchpadRotate_CounterClockwise_DoesNotCrash()
    {
        var zoomBorder = await CreateZoomBorderAsync();

        var exception = TouchTestHelpers.Record(() =>
            TouchTestHelpers.Rotate(zoomBorder, new Point(200, 150), 50, 0, -45));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsNull(exception);
    }

    #endregion

    #region Multi-Step Simulation Tests

    [TestMethod]
    public async Task SimulatePinchZoom_ZoomIn_IncreasesZoom()
    {
        var zoomBorder = await CreateZoomBorderAsync();
        var initialZoom = zoomBorder.ZoomX;

        // Pinch outward (zoom in)
        TouchTestHelpers.Pinch(zoomBorder, new Point(200, 150), 50, 150, 10);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(zoomBorder.ZoomX > initialZoom,
            $"Zoom should increase from {initialZoom}, was {zoomBorder.ZoomX}");
    }

    [TestMethod]
    public async Task SimulatePinchZoom_ZoomOut_DecreasesZoom()
    {
        var zoomBorder = await CreateZoomBorderAsync();
        zoomBorder.ZoomTo(2.0, 100, 75);
        var initialZoom = zoomBorder.ZoomX;

        // Pinch inward (zoom out)
        TouchTestHelpers.Pinch(zoomBorder, new Point(200, 150), 150, 50, 10);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(zoomBorder.ZoomX < initialZoom,
            $"Zoom should decrease from {initialZoom}, was {zoomBorder.ZoomX}");
    }

    [TestMethod]
    public async Task SimulateTwoFingerPan_ChangesOffset()
    {
        var zoomBorder = await CreateZoomBorderAsync();
        var initialOffsetX = zoomBorder.OffsetX;
        var initialOffsetY = zoomBorder.OffsetY;

        TouchTestHelpers.TwoFingerPan(zoomBorder, new Point(100, 100), new Point(200, 150), 50, 10);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        var offsetChanged = zoomBorder.OffsetX != initialOffsetX || zoomBorder.OffsetY != initialOffsetY;
        Assert.IsTrue(offsetChanged, "Offset should change after two-finger pan simulation");
    }

    [TestMethod]
    public async Task SimulateRotation_CompletesWithoutError()
    {
        var zoomBorder = await CreateZoomBorderAsync();

        var exception = TouchTestHelpers.Record(() =>
            TouchTestHelpers.Rotate(zoomBorder, new Point(200, 150), 50, 0, 90, 10));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsNull(exception);
    }

    [TestMethod]
    public async Task SimulateDrag_RaisesPointerEvents()
    {
        var zoomBorder = await CreateZoomBorderAsync();
        using var recorder = new TouchPointerRecorder(zoomBorder);

        TouchTestHelpers.Drag(zoomBorder, new Point(100, 100), new Point(200, 150), 10);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.AreEqual(1, recorder.PressedCount);
        Assert.IsTrue(recorder.MovedCount >= 10, $"Should have at least 10 move events, had {recorder.MovedCount}");
        Assert.AreEqual(1, recorder.ReleasedCount);
    }

    #endregion

    #region Swipe Gesture Tests

    [TestMethod]
    public async Task Swipe_Left_RaisesEvents()
    {
        var zoomBorder = await CreateZoomBorderAsync();
        using var recorder = new TouchPointerRecorder(zoomBorder);

        TouchTestHelpers.Swipe(zoomBorder, new Point(300, 150), TouchSwipeDirection.Left, 100);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(recorder.PressedCount > 0);
        Assert.IsTrue(recorder.ReleasedCount > 0);
    }

    [TestMethod]
    public async Task Swipe_Right_RaisesEvents()
    {
        var zoomBorder = await CreateZoomBorderAsync();
        using var recorder = new TouchPointerRecorder(zoomBorder);

        TouchTestHelpers.Swipe(zoomBorder, new Point(100, 150), TouchSwipeDirection.Right, 100);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(recorder.PressedCount > 0);
        Assert.IsTrue(recorder.ReleasedCount > 0);
    }

    [TestMethod]
    public async Task Swipe_Up_RaisesEvents()
    {
        var zoomBorder = await CreateZoomBorderAsync();
        using var recorder = new TouchPointerRecorder(zoomBorder);

        TouchTestHelpers.Swipe(zoomBorder, new Point(200, 250), TouchSwipeDirection.Up, 100);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(recorder.PressedCount > 0);
        Assert.IsTrue(recorder.ReleasedCount > 0);
    }

    [TestMethod]
    public async Task Swipe_Down_RaisesEvents()
    {
        var zoomBorder = await CreateZoomBorderAsync();
        using var recorder = new TouchPointerRecorder(zoomBorder);

        TouchTestHelpers.Swipe(zoomBorder, new Point(200, 50), TouchSwipeDirection.Down, 100);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(recorder.PressedCount > 0);
        Assert.IsTrue(recorder.ReleasedCount > 0);
    }

    #endregion

    #region Touch Interaction Tests

    [TestMethod]
    public async Task TouchTap_RaisesPointerPressedAndReleased()
    {
        var zoomBorder = await CreateZoomBorderAsync();
        using var recorder = new TouchPointerRecorder(zoomBorder);

        TouchTestHelpers.Tap(zoomBorder, new Point(200, 150));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.AreEqual(1, recorder.PressedCount);
        Assert.AreEqual(1, recorder.ReleasedCount);
    }

    [TestMethod]
    public async Task TouchDoubleTap_RaisesTwoTapSequences()
    {
        var zoomBorder = await CreateZoomBorderAsync();
        using var recorder = new TouchPointerRecorder(zoomBorder);

        TouchTestHelpers.DoubleTap(zoomBorder, new Point(200, 150));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.AreEqual(2, recorder.PressedCount);
        Assert.AreEqual(2, recorder.ReleasedCount);
    }

    [TestMethod]
    public async Task TouchDrag_WithPanEnabled_ChangesOffset()
    {
        var zoomBorder = await CreateZoomBorderAsync(configure: zb =>
        {
            zb.EnablePan = true;
            zb.PanButton = ButtonName.Left;
        });
        using var recorder = new TouchPointerRecorder(zoomBorder);
        var initialOffsetX = zoomBorder.OffsetX;

        TouchTestHelpers.Drag(zoomBorder, new Point(100, 100), new Point(200, 150), 5);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(recorder.MovedCount >= 5);
        // Uno: a one finger touch drag pans through the manipulation (scroll gesture), so the offset changes too
        Assert.AreNotEqual(initialOffsetX, zoomBorder.OffsetX);
    }

    [TestMethod]
    public async Task MultiTouch_TwoFingerTouchDown_TracksPoints()
    {
        var zoomBorder = await CreateZoomBorderAsync();
        using var recorder = new TouchPointerRecorder(zoomBorder);
        using var session = new TouchInjectionSession(zoomBorder);

        var touch1 = session.TouchDown(new Point(100, 100));
        var touch2 = session.TouchDown(new Point(200, 200));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        // Uno: the simulator bookkeeping assertions become assertions on the pointers the control received
        Assert.AreEqual(2, recorder.ActivePointers.Count);
        Assert.IsTrue(recorder.ActivePointers.ContainsKey(touch1));
        Assert.IsTrue(recorder.ActivePointers.ContainsKey(touch2));
        Assert.AreEqual(2, session.ActiveTouchPoints.Count);
    }

    [TestMethod]
    public async Task MultiTouch_MoveAndRelease_WorksCorrectly()
    {
        var zoomBorder = await CreateZoomBorderAsync();
        using var recorder = new TouchPointerRecorder(zoomBorder);
        using var session = new TouchInjectionSession(zoomBorder);

        var touch1 = session.TouchDown(new Point(100, 100));
        var touch2 = session.TouchDown(new Point(200, 200));

        session.TouchMove(touch1, new Point(110, 110));
        session.TouchMove(touch2, new Point(210, 210));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        // Uno: positions are the ones reported to the control (whole pixel injection, so within 1px)
        AssertPoint(new Point(110, 110), recorder.ActivePointers[touch1]);
        AssertPoint(new Point(210, 210), recorder.ActivePointers[touch2]);

        session.TouchUp(touch1);
        await ZoomBorderTestHelper.WaitForIdleAsync();
        Assert.AreEqual(1, recorder.ActivePointers.Count);

        session.TouchUp(touch2);
        await ZoomBorderTestHelper.WaitForIdleAsync();
        Assert.AreEqual(0, recorder.ActivePointers.Count);
        Assert.AreEqual(0, session.ActiveTouchPoints.Count);
    }

    #endregion

    #region Edge Cases and Error Handling

    [TestMethod]
    public async Task PinchGesture_ScaleOfOne_NoChange()
    {
        var zoomBorder = await CreateZoomBorderAsync();
        var initialZoom = zoomBorder.ZoomX;

        // Scale of 1 means the finger distance does not change
        await PinchAsync(zoomBorder, 1.0);

        Assert.AreEqual(initialZoom, zoomBorder.ZoomX);
    }

    [TestMethod]
    public async Task ScrollGesture_ZeroDelta_NoChange()
    {
        var zoomBorder = await CreateZoomBorderAsync();
        var initialOffsetX = zoomBorder.OffsetX;
        var initialOffsetY = zoomBorder.OffsetY;

        await ScrollAsync(zoomBorder, 0, 0);

        Assert.AreEqual(initialOffsetX, zoomBorder.OffsetX);
        Assert.AreEqual(initialOffsetY, zoomBorder.OffsetY);
    }

    [TestMethod]
    public async Task PinchGesture_VerySmallScale_HandlesGracefully()
    {
        var zoomBorder = await CreateZoomBorderAsync();

        // Should not crash with very small scale (the fingers end on the same pixel)
        var exception = TouchTestHelpers.Record(() =>
            TouchTestHelpers.PinchScale(zoomBorder, 0.001, Center(zoomBorder), 200));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsNull(exception);
    }

    [TestMethod]
    public async Task PinchGesture_VeryLargeScale_HandlesGracefully()
    {
        var zoomBorder = await CreateZoomBorderAsync();

        // Should not crash with very large scale (3px -> 300px finger distance)
        var exception = TouchTestHelpers.Record(() =>
            TouchTestHelpers.PinchScale(zoomBorder, 100.0, Center(zoomBorder), 3));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsNull(exception);
    }

    [TestMethod]
    public async Task ScrollGesture_VeryLargeDelta_HandlesGracefully()
    {
        var zoomBorder = await CreateZoomBorderAsync();

        // Uno: a 10000px finger move cannot be injected inside the test window, so the manipulation handlers are driven directly
        var exception = TouchTestHelpers.Record(() =>
            TouchTestHelpers.ManipulationTranslate(zoomBorder, new Point(-10000, -10000)));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsNull(exception);
    }

    [TestMethod]
    public async Task RapidGestures_HandlesWithoutError()
    {
        var zoomBorder = await CreateZoomBorderAsync();

        // Rapid succession of real gestures (few steps each)
        var exception = TouchTestHelpers.Record(() =>
        {
            for (var i = 0; i < 50; i++)
            {
                TouchTestHelpers.PinchScale(zoomBorder, 1.1, Center(zoomBorder), 100, 2);
                TouchTestHelpers.ScrollGesture(zoomBorder, 5, 5, steps: 2);
            }
        });
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsNull(exception);
    }

    [TestMethod]
    public async Task GestureSequence_PinchThenScroll_WorksCorrectly()
    {
        var zoomBorder = await CreateZoomBorderAsync();
        var gestureEvents = new List<string>();
        zoomBorder.GestureStarted += (_, args) => gestureEvents.Add($"Start:{args.GestureType}");
        zoomBorder.GestureEnded += (_, args) => gestureEvents.Add($"End:{args.GestureType}");

        await PinchAsync(zoomBorder, 1.5);
        await ScrollAsync(zoomBorder, 30, 20);

        CollectionAssert.Contains(gestureEvents, "Start:Pinch");
        CollectionAssert.Contains(gestureEvents, "End:Pinch");
        CollectionAssert.Contains(gestureEvents, "Start:Scroll");
        CollectionAssert.Contains(gestureEvents, "End:Scroll");
    }

    #endregion

    #region Combined Operations Tests

    [TestMethod]
    public async Task ZoomThenPan_BothOperationsWork()
    {
        var zoomBorder = await CreateZoomBorderAsync();

        await PinchAsync(zoomBorder, 2.0);
        var zoomAfterPinch = zoomBorder.ZoomX;

        var offsetBeforePan = zoomBorder.OffsetX;
        await ScrollAsync(zoomBorder, -50, 0);
        var offsetAfterPan = zoomBorder.OffsetX;

        Assert.IsTrue(zoomAfterPinch > 1.0, "Should have zoomed in");
        Assert.AreNotEqual(offsetBeforePan, offsetAfterPan);
    }

    [TestMethod]
    public async Task ResetMatrixAfterGestures_ResetsToDefault()
    {
        var zoomBorder = await CreateZoomBorderAsync();

        await PinchAsync(zoomBorder, 2.0);
        await ScrollAsync(zoomBorder, -50, -30);

        zoomBorder.ResetMatrix();

        Assert.AreEqual(1.0, zoomBorder.ZoomX);
        Assert.AreEqual(1.0, zoomBorder.ZoomY);
        Assert.AreEqual(0.0, zoomBorder.OffsetX);
        Assert.AreEqual(0.0, zoomBorder.OffsetY);
    }

    [TestMethod]
    public async Task FillAfterGestures_FillsContent()
    {
        var zoomBorder = await CreateZoomBorderAsync();

        await PinchAsync(zoomBorder, 0.5);
        var zoomAfterPinch = zoomBorder.ZoomX;

        zoomBorder.Fill();

        // Fill should change the zoom
        Assert.AreNotEqual(zoomAfterPinch, zoomBorder.ZoomX);
    }

    #endregion
}

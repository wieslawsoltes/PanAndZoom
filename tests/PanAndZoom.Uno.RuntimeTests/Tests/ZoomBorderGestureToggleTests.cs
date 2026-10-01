// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using Microsoft.UI.Xaml.Input;

namespace PanAndZoom.Uno.RuntimeTests.Tests;

/// <summary>
/// Gesture and input toggle tests ported from the Avalonia <c>ZoomBorderGestureToggleTests</c>.
/// </summary>
/// <remarks>
/// Avalonia gesture recognizers are WinUI manipulations on Uno (<see cref="UIElement.ManipulationMode"/>), the
/// <c>:isPanning</c> pseudo class is the <see cref="ZoomBorder.IsPanning"/> property, and the Avalonia <c>e.Handled</c>
/// flag of gesture events is mapped to the <see cref="ZoomBorder.GestureStarted"/> events.
/// </remarks>
[TestClass]
[RunsOnUIThread]
public class ZoomBorderGestureToggleTests
{
    private const ManipulationModes GestureManipulations =
        ManipulationModes.TranslateX | ManipulationModes.TranslateY | ManipulationModes.Scale | ManipulationModes.Rotate;

    [TestCleanup]
    public async Task Cleanup()
    {
        GestureTestHelpers.ResetTouch();
        InputHelper.Reset();
        await ZoomBorderTestHelper.UnloadAsync();
    }

    private static async Task<ZoomBorder> LoadWithoutChildAsync(double width, double height, Action<ZoomBorder> configure)
    {
        var zoomBorder = new ZoomBorder
        {
            Width = width,
            Height = height,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
        configure(zoomBorder);
        await ZoomBorderTestHelper.LoadAsync(zoomBorder);
        return zoomBorder;
    }

    private static async Task<ZoomBorder> CreateAsync(Action<ZoomBorder> configure)
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150, configure);
        return zoomBorder;
    }

    [TestMethod]
    public async Task EnableGestures_True_AddsGestureRecognizers()
    {
        var zoomBorder = await LoadWithoutChildAsync(400, 300, zb => zb.EnableGestures = false);

        zoomBorder.EnableGestures = true;

        Assert.IsTrue(zoomBorder.EnableGestures, "EnableGestures should be true");
        // Uno adaptation: gesture recognizers are the translate/scale/rotate manipulation modes.
        Assert.AreEqual(GestureManipulations, zoomBorder.ManipulationMode);
    }

    [TestMethod]
    public async Task EnableGestures_False_RemovesGestureRecognizers()
    {
        var zoomBorder = await LoadWithoutChildAsync(400, 300, zb => zb.EnableGestures = true);

        zoomBorder.EnableGestures = false;

        Assert.IsFalse(zoomBorder.EnableGestures, "EnableGestures should be false");
        // Uno adaptation: removing the gesture recognizers turns the manipulations off (system handling only).
        Assert.AreEqual(ManipulationModes.System, zoomBorder.ManipulationMode);
    }

    [TestMethod]
    public async Task EnableGestures_ToggledOffMidPinch_NextPinchStartsFresh()
    {
        var zoomBorder = await CreateAsync(zb =>
        {
            zb.EnableGestures = true;
            zb.EnableGestureZoom = true;
        });

        // Uno adaptation: pinch updates/ends are driven through the manipulation handlers instead of raising PinchEventArgs.
        GestureTestHelpers.Pinch(zoomBorder, 2.0, new Point(200, 150));
        var zoomAfterFirstPinch = zoomBorder.ZoomX;

        zoomBorder.EnableGestures = false;
        zoomBorder.HandleManipulationCompleted();
        zoomBorder.EnableGestures = true;

        GestureTestHelpers.Pinch(zoomBorder, 1.5, new Point(200, 150));

        Assert.IsTrue(zoomBorder.ZoomX > zoomAfterFirstPinch,
            $"A fresh pinch should continue zooming in after gestures are re-enabled. Previous: {zoomAfterFirstPinch}, Current: {zoomBorder.ZoomX}");
    }

    [TestMethod]
    public async Task EnableGestureZoom_False_PinchGestureIgnored()
    {
        var zoomBorder = await CreateAsync(zb =>
        {
            zb.EnableGestures = true;
            zb.EnableGestureZoom = false;
            zb.EnableGestureRotation = false; // Also disable rotation to fully ignore pinch
        });
        var recorder = new GestureRecorder(zoomBorder);
        var initialZoom = zoomBorder.ZoomX;

        // Uno adaptation: pinch driven through the manipulation handler instead of raising PinchEventArgs.
        GestureTestHelpers.Pinch(zoomBorder, 1.5, new Point(0.5, 0.5));

        Assert.AreEqual(initialZoom, zoomBorder.ZoomX);
        // Uno adaptation: "not handled" = the engine did not process the pinch (no GestureStarted "Pinch").
        Assert.AreEqual(0, recorder.Pinches.Count, "Pinch should not be handled when gesture zoom and rotation are disabled");
    }

    [TestMethod]
    public async Task EnableGestureTranslation_False_ScrollGestureIgnored()
    {
        var zoomBorder = await CreateAsync(zb =>
        {
            zb.EnableGestures = true;
            zb.EnableGestureTranslation = false;
        });
        var recorder = new GestureRecorder(zoomBorder);
        var initialOffsetX = zoomBorder.OffsetX;
        var initialOffsetY = zoomBorder.OffsetY;

        // Uno adaptation: a real one finger drag (scroll delta 50,50 = finger moving by -50,-50) instead of ScrollGestureEventArgs.
        new TestPointer().Drag(zoomBorder, new Point(200, 150), new Point(150, 100));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.AreEqual(initialOffsetX, zoomBorder.OffsetX);
        Assert.AreEqual(initialOffsetY, zoomBorder.OffsetY);
        // Uno adaptation: "not handled" = the engine did not process the scroll (no GestureStarted "Scroll").
        Assert.AreEqual(0, recorder.Scrolls.Count, "Scroll gesture should not be handled when gesture translation is disabled");
    }

    [TestMethod]
    public async Task EnablePan_False_PointerPanIgnored()
    {
        var zoomBorder = await CreateAsync(zb =>
        {
            zb.EnablePan = false;
            zb.PanButton = ButtonName.Left;
        });

        // Real left mouse button press (instead of raising PointerPressedEventArgs).
        InputHelper.MouseMoveTo(ZoomBorderTestHelper.ToWindow(zoomBorder, new Point(200, 150)));
        InputHelper.MouseDown(ButtonName.Left);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsFalse(zoomBorder.IsPanning, "Panning should not be active when pan is disabled");

        InputHelper.MouseUp(ButtonName.Left);
        await ZoomBorderTestHelper.WaitForIdleAsync();
    }

    [TestMethod]
    public async Task EnableZoom_False_PointerWheelZoomIgnored()
    {
        var zoomBorder = await CreateAsync(zb =>
        {
            zb.EnableZoom = false;
            zb.EnablePan = false;
        });
        var initialZoom = zoomBorder.ZoomX;

        // Real wheel input (instead of raising PointerWheelEventArgs).
        InputHelper.MouseWheel(ZoomBorderTestHelper.ToWindow(zoomBorder, new Point(200, 150)), 120);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.AreEqual(initialZoom, zoomBorder.ZoomX);
        // Uno adaptation: the handled flag is the result of the control wheel handler (used by OnPointerWheelChanged).
        Assert.IsFalse(ZoomBorderTestHelper.Wheel(zoomBorder, 120, new Point(200, 150)), "Pointer wheel event should not be handled when zoom is disabled");
        Assert.AreEqual(initialZoom, zoomBorder.ZoomX);
    }

    [TestMethod]
    public async Task PanButton_Changed_AffectsPointerCapture()
    {
        var zoomBorder = await CreateAsync(zb =>
        {
            zb.EnablePan = true;
            zb.PanButton = ButtonName.Left;
        });
        var position = ZoomBorderTestHelper.ToWindow(zoomBorder, new Point(200, 150));

        // Test with left button (should work)
        InputHelper.MouseMoveTo(position);
        InputHelper.MouseDown(ButtonName.Left);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(zoomBorder.IsPanning, "Left button should start panning when PanButton is Left");
        // Uno addition: the panning pointer is captured by the control.
        Assert.IsTrue(zoomBorder.PointerCaptures?.Count > 0, "The panning pointer should be captured");

        // Change pan button to right
        zoomBorder.PanButton = ButtonName.Right;

        // Reset panning state
        InputHelper.MouseUp(ButtonName.Left);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        // Test with left button again (should not work now)
        InputHelper.MouseDown(ButtonName.Left);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsFalse(zoomBorder.IsPanning, "Left button should not start panning when PanButton is Right");

        InputHelper.MouseUp(ButtonName.Left);
        await ZoomBorderTestHelper.WaitForIdleAsync();
    }

    [TestMethod]
    public async Task ZoomSpeed_Changed_AffectsZoomAmount()
    {
        var zoomBorder = await CreateAsync(zb =>
        {
            zb.EnableZoom = true;
            zb.ZoomSpeed = 1.2; // Default speed
            zb.Stretch = StretchMode.None;
            zb.EnableConstrains = false;
        });
        var initialZoom = zoomBorder.ZoomX;

        // Real wheel input with the default zoom speed (instead of raising PointerWheelEventArgs).
        InputHelper.MouseWheel(ZoomBorderTestHelper.ToWindow(zoomBorder, new Point(200, 150)), 120);
        await ZoomBorderTestHelper.WaitForIdleAsync();
        var zoomAfterDefault = zoomBorder.ZoomX;

        // Reset zoom (a ratio of the initial zoom, as in the Avalonia test)
        zoomBorder.ZoomTo(initialZoom, 0, 0);

        // Change zoom speed
        zoomBorder.ZoomSpeed = 2.0;

        InputHelper.MouseWheel(ZoomBorderTestHelper.ToWindow(zoomBorder, new Point(50, 50)), 120);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(zoomAfterDefault > initialZoom, $"Zoom should increase with default speed. Initial: {initialZoom}, After: {zoomAfterDefault}");
        Assert.IsTrue(zoomBorder.ZoomX > zoomAfterDefault, $"Zoom should increase more with higher speed. After default: {zoomAfterDefault}, After increased: {zoomBorder.ZoomX}");
    }

    [TestMethod]
    public async Task PropertyChanges_TriggerCorrectBehavior()
    {
        var zoomBorder = await LoadWithoutChildAsync(200, 150, _ => { });

        zoomBorder.EnableGestures = true;
        Assert.IsTrue(zoomBorder.EnableGestures);

        zoomBorder.EnableGestureZoom = true;
        Assert.IsTrue(zoomBorder.EnableGestureZoom);

        zoomBorder.EnableGestureTranslation = true;
        Assert.IsTrue(zoomBorder.EnableGestureTranslation);

        zoomBorder.EnablePan = true;
        Assert.IsTrue(zoomBorder.EnablePan);

        zoomBorder.EnableZoom = true;
        Assert.IsTrue(zoomBorder.EnableZoom);

        zoomBorder.PanButton = ButtonName.Middle;
        Assert.AreEqual(ButtonName.Middle, zoomBorder.PanButton);

        zoomBorder.ZoomSpeed = 0.5;
        Assert.AreEqual(0.5, zoomBorder.ZoomSpeed);
    }
}

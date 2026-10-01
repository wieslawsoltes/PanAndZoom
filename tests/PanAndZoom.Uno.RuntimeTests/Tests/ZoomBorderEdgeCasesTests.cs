// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using Windows.System;

namespace PanAndZoom.Uno.RuntimeTests.Tests;

/// <summary>
/// Tests for edge cases and additional scenarios in ZoomBorder.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class ZoomBorderEdgeCasesTests
{
    [TestCleanup]
    public async Task Cleanup()
    {
        InputHelper.Reset();
        await ZoomBorderTestHelper.UnloadAsync();
    }

    private static async Task<(ZoomBorder ZoomBorder, Canvas Canvas)> CreateCanvasAndLoadAsync(Action<ZoomBorder>? configure = null)
    {
        var canvas = new Canvas { Width = 200, Height = 200 };
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Child = canvas
        };
        configure?.Invoke(zoomBorder);
        await ZoomBorderTestHelper.LoadAsync(zoomBorder);
        return (zoomBorder, canvas);
    }

    // ===== Keyboard Navigation Edge Cases =====

    [TestMethod]
    public async Task KeyDown_D0_WithoutControl_DoesNotReset()
    {
        var (zoomBorder, _) = await CreateCanvasAndLoadAsync(zb => zb.EnableKeyboardNavigation = true);

        // First zoom in
        zoomBorder.ZoomTo(2.0, 100, 100, skipTransitions: true);
        var zoomedValue = zoomBorder.ZoomX;

        // Press D0 without Control modifier (keys are routed through the control keyboard handler on Uno)
        ZoomBorderTestHelper.PressKey(zoomBorder, VirtualKey.Number0, ZoomBorderKeyModifiers.None);

        // Zoom should remain unchanged
        Assert.AreEqual(zoomedValue, zoomBorder.ZoomX);
    }

    [TestMethod]
    public async Task KeyDown_UnhandledKey_DoesNotAffectZoom()
    {
        var (zoomBorder, _) = await CreateCanvasAndLoadAsync(zb => zb.EnableKeyboardNavigation = true);

        var initialZoomX = zoomBorder.ZoomX;
        var initialOffsetX = zoomBorder.OffsetX;

        // Press an unhandled key (e.g., A)
        ZoomBorderTestHelper.PressKey(zoomBorder, VirtualKey.A, ZoomBorderKeyModifiers.None);

        // State should remain unchanged
        Assert.AreEqual(initialZoomX, zoomBorder.ZoomX);
        Assert.AreEqual(initialOffsetX, zoomBorder.OffsetX);
    }

    // ===== Child Element Change Tests =====

    [TestMethod]
    public async Task ChangingChild_DetachesOldAndAttachesNew()
    {
        var (zoomBorder, firstCanvas) = await CreateCanvasAndLoadAsync();
        var secondCanvas = new Canvas { Width = 300, Height = 300 };

        // Zoom the first child
        zoomBorder.ZoomTo(2.0, 100, 100, skipTransitions: true);

        // Change the child
        zoomBorder.Child = secondCanvas;
        await ZoomBorderTestHelper.WaitForIdleAsync();

        // ZoomBorder should still work with new child
        Assert.IsNotNull(zoomBorder.Child);
        Assert.AreSame(secondCanvas, zoomBorder.Child);

        // Uno: the old child's owned render transform is detached and the new child receives one.
        Assert.IsFalse(firstCanvas.RenderTransform is MatrixTransform, "Old child should no longer be transformed by the control");
        Assert.IsTrue(await ZoomBorderTestHelper.WaitForAsync(() => secondCanvas.RenderTransform is MatrixTransform), "New child should be transformed by the control");
    }

    [TestMethod]
    public async Task RemovingChild_ClearsElement()
    {
        var (zoomBorder, _) = await CreateCanvasAndLoadAsync();

        // Remove the child
        zoomBorder.Child = null;
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsNull(zoomBorder.Child);
    }

    // ===== Pointer Capture Loss Tests =====

    [TestMethod]
    public async Task PointerCaptureLost_EndsPanning()
    {
        var (zoomBorder, _) = await CreateCanvasAndLoadAsync(zb =>
        {
            zb.EnablePan = true;
            zb.PanButton = ButtonName.Left;
        });

        // Start panning with a real (injected) left button press
        InputHelper.Reset();
        InputHelper.MouseMoveTo(ZoomBorderTestHelper.ToWindow(zoomBorder, new Point(100, 75)));
        InputHelper.MouseDown(ButtonName.Left);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        // Uno: the Avalonia ':isPanning' pseudo class is the IsPanning property.
        Assert.IsTrue(zoomBorder.IsPanning, "Pressing the pan button should start panning");

        // Simulate pointer capture lost (releasing the captures raises PointerCaptureLost on WinUI)
        zoomBorder.ReleasePointerCaptures();
        await ZoomBorderTestHelper.WaitForIdleAsync();

        // Panning should end
        Assert.IsFalse(zoomBorder.IsPanning, "Losing the pointer capture should end panning");

        InputHelper.MouseUp(ButtonName.Left);
    }

    // ===== EnablePan and EnableZoom Tests =====

    [TestMethod]
    public void EnablePan_DefaultValue_IsTrue()
    {
        var zoomBorder = new ZoomBorder();

        Assert.IsTrue(zoomBorder.EnablePan);
    }

    [TestMethod]
    public void EnableZoom_DefaultValue_IsTrue()
    {
        var zoomBorder = new ZoomBorder();

        Assert.IsTrue(zoomBorder.EnableZoom);
    }

    [TestMethod]
    public async Task ZoomIn_WhenEnableZoomIsFalse_StillWorks()
    {
        // User interaction zoom disabled
        var (zoomBorder, _) = await CreateCanvasAndLoadAsync(zb => zb.EnableZoom = false);

        var initialZoom = zoomBorder.ZoomX;

        // Programmatic zoom should still work
        zoomBorder.ZoomIn();

        Assert.IsTrue(zoomBorder.ZoomX > initialZoom);
    }

    [TestMethod]
    public async Task Pan_WhenEnablePanIsFalse_StillWorks()
    {
        // User interaction pan disabled
        var (zoomBorder, _) = await CreateCanvasAndLoadAsync(zb => zb.EnablePan = false);

        var initialOffsetX = zoomBorder.OffsetX;

        // Programmatic pan should still work
        zoomBorder.Pan(50, 50);

        Assert.AreNotEqual(initialOffsetX, zoomBorder.OffsetX);
    }
}

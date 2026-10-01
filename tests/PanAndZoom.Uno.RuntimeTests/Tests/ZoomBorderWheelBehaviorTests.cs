// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using Windows.System;

namespace PanAndZoom.Uno.RuntimeTests.Tests;

/// <summary>
/// Port of the Avalonia <c>ZoomBorderWheelBehaviorTests</c>. Wheel input is injected for real, keyboard
/// modifiers are reported with the injected wheel event (<c>PointerRoutedEventArgs.KeyModifiers</c>).
/// </summary>
[TestClass]
[RunsOnUIThread]
public class ZoomBorderWheelBehaviorTests
{
    [TestCleanup]
    public async Task Cleanup()
    {
        InputHelper.Reset();
        await ZoomBorderTestHelper.UnloadAsync();
    }

    private static async Task<ZoomBorder> CreateAsync(Action<ZoomBorder> configure)
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150, configure);
        InputHelper.Reset();
        return zoomBorder;
    }

    [TestMethod]
    public void WheelBehavior_DefaultValue_IsZoom()
    {
        var zoomBorder = new ZoomBorder();

        Assert.AreEqual(WheelBehaviorMode.Zoom, zoomBorder.WheelBehavior);
    }

    [TestMethod]
    public void WheelBehavior_CanBeSetToZoom()
    {
        var zoomBorder = new ZoomBorder();

        zoomBorder.WheelBehavior = WheelBehaviorMode.Zoom;

        Assert.AreEqual(WheelBehaviorMode.Zoom, zoomBorder.WheelBehavior);
    }

    [TestMethod]
    public void WheelBehavior_CanBeSetToPanVertical()
    {
        var zoomBorder = new ZoomBorder();

        zoomBorder.WheelBehavior = WheelBehaviorMode.PanVertical;

        Assert.AreEqual(WheelBehaviorMode.PanVertical, zoomBorder.WheelBehavior);
    }

    [TestMethod]
    public void WheelBehavior_CanBeSetToPanHorizontal()
    {
        var zoomBorder = new ZoomBorder();

        zoomBorder.WheelBehavior = WheelBehaviorMode.PanHorizontal;

        Assert.AreEqual(WheelBehaviorMode.PanHorizontal, zoomBorder.WheelBehavior);
    }

    [TestMethod]
    public void WheelBehavior_CanBeSetToNone()
    {
        var zoomBorder = new ZoomBorder();

        zoomBorder.WheelBehavior = WheelBehaviorMode.None;

        Assert.AreEqual(WheelBehaviorMode.None, zoomBorder.WheelBehavior);
    }

    [TestMethod]
    public void WheelWithCtrl_DefaultValue_IsZoom()
    {
        var zoomBorder = new ZoomBorder();

        Assert.AreEqual(WheelBehaviorMode.Zoom, zoomBorder.WheelWithCtrl);
    }

    [TestMethod]
    public void WheelWithCtrl_CanBeChanged()
    {
        var zoomBorder = new ZoomBorder();

        zoomBorder.WheelWithCtrl = WheelBehaviorMode.PanVertical;

        Assert.AreEqual(WheelBehaviorMode.PanVertical, zoomBorder.WheelWithCtrl);
    }

    [TestMethod]
    public void WheelWithShift_DefaultValue_IsPanHorizontal()
    {
        var zoomBorder = new ZoomBorder();

        Assert.AreEqual(WheelBehaviorMode.PanHorizontal, zoomBorder.WheelWithShift);
    }

    [TestMethod]
    public void WheelWithShift_CanBeChanged()
    {
        var zoomBorder = new ZoomBorder();

        zoomBorder.WheelWithShift = WheelBehaviorMode.Zoom;

        Assert.AreEqual(WheelBehaviorMode.Zoom, zoomBorder.WheelWithShift);
    }

    [TestMethod]
    public void WheelZoomSensitivity_DefaultValue_IsOne()
    {
        var zoomBorder = new ZoomBorder();

        Assert.AreEqual(1.0, zoomBorder.WheelZoomSensitivity);
    }

    [TestMethod]
    public void WheelZoomSensitivity_CanBeSetToCustomValue()
    {
        var zoomBorder = new ZoomBorder();

        zoomBorder.WheelZoomSensitivity = 2.5;

        Assert.AreEqual(2.5, zoomBorder.WheelZoomSensitivity);
    }

    [TestMethod]
    public void WheelPanSensitivity_DefaultValue_IsOne()
    {
        var zoomBorder = new ZoomBorder();

        Assert.AreEqual(1.0, zoomBorder.WheelPanSensitivity);
    }

    [TestMethod]
    public void WheelPanSensitivity_CanBeSetToCustomValue()
    {
        var zoomBorder = new ZoomBorder();

        zoomBorder.WheelPanSensitivity = 1.5;

        Assert.AreEqual(1.5, zoomBorder.WheelPanSensitivity);
    }

    [TestMethod]
    public void WheelBehavior_AllPropertiesCanBeSetTogether()
    {
        var zoomBorder = new ZoomBorder
        {
            WheelBehavior = WheelBehaviorMode.PanVertical,
            WheelWithCtrl = WheelBehaviorMode.Zoom,
            WheelWithShift = WheelBehaviorMode.PanHorizontal,
            WheelZoomSensitivity = 2.0,
            WheelPanSensitivity = 1.5
        };

        Assert.AreEqual(WheelBehaviorMode.PanVertical, zoomBorder.WheelBehavior);
        Assert.AreEqual(WheelBehaviorMode.Zoom, zoomBorder.WheelWithCtrl);
        Assert.AreEqual(WheelBehaviorMode.PanHorizontal, zoomBorder.WheelWithShift);
        Assert.AreEqual(2.0, zoomBorder.WheelZoomSensitivity);
        Assert.AreEqual(1.5, zoomBorder.WheelPanSensitivity);
    }

    [TestMethod]
    public async Task WheelBehavior_Zoom_ZoomsInOnWheelScroll()
    {
        var zoomBorder = await CreateAsync(zb =>
        {
            zb.WheelBehavior = WheelBehaviorMode.Zoom;
            zb.EnableZoom = true;
        });

        var initialZoom = zoomBorder.ZoomX;

        // Scroll up to zoom in.
        PointerTestHelpers.MouseWheelAt(zoomBorder, new Point(200, 150), 120);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(zoomBorder.ZoomX > initialZoom, "Zoom should increase on wheel scroll up");
    }

    [TestMethod]
    public async Task WheelBehavior_PanVertical_PansVerticallyOnWheelScroll()
    {
        var zoomBorder = await CreateAsync(zb =>
        {
            zb.WheelBehavior = WheelBehaviorMode.PanVertical;
            zb.EnablePan = true;
        });

        var initialOffsetY = zoomBorder.OffsetY;

        PointerTestHelpers.MouseWheelAt(zoomBorder, new Point(200, 150), 120);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(zoomBorder.OffsetY != initialOffsetY, "Should pan vertically on wheel scroll");
    }

    [TestMethod]
    public async Task WheelBehavior_WithCtrlModifier_UsesCtrlBehaviorInsteadOfDefault()
    {
        var zoomBorder = await CreateAsync(zb =>
        {
            zb.WheelBehavior = WheelBehaviorMode.PanVertical;
            zb.WheelWithCtrl = WheelBehaviorMode.Zoom;
            zb.EnableZoom = true;
            zb.EnablePan = true;
        });

        var initialZoom = zoomBorder.ZoomX;

        PointerTestHelpers.MouseWheelAt(zoomBorder, new Point(200, 150), 120, VirtualKeyModifiers.Control);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        // Should zoom (WheelWithCtrl) instead of pan (WheelBehavior).
        Assert.IsTrue(zoomBorder.ZoomX > initialZoom, "Should zoom when Ctrl is held");
    }

    [TestMethod]
    public async Task WheelBehavior_WithShiftModifier_UsesShiftBehaviorInsteadOfDefault()
    {
        var zoomBorder = await CreateAsync(zb =>
        {
            zb.WheelBehavior = WheelBehaviorMode.Zoom;
            zb.WheelWithShift = WheelBehaviorMode.PanHorizontal;
            zb.EnableZoom = true;
            zb.EnablePan = true;
        });

        var initialOffsetX = zoomBorder.OffsetX;

        PointerTestHelpers.MouseWheelAt(zoomBorder, new Point(200, 150), 120, VirtualKeyModifiers.Shift);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        // Should pan horizontally (WheelWithShift) instead of zoom (WheelBehavior).
        Assert.IsTrue(zoomBorder.OffsetX != initialOffsetX, "Should pan horizontally when Shift is held");
    }

    [TestMethod]
    public async Task WheelZoomSensitivity_HigherValue_ProducesMoreZoom()
    {
        // Avalonia shows two windows; here both controls are loaded one after the other into the test window.
        var zoomBorder1 = await CreateAsync(zb =>
        {
            zb.WheelBehavior = WheelBehaviorMode.Zoom;
            zb.WheelZoomSensitivity = 1.0;
            zb.EnableZoom = true;
        });

        PointerTestHelpers.MouseWheelAt(zoomBorder1, new Point(200, 150), 120);
        await ZoomBorderTestHelper.WaitForIdleAsync();
        var zoom1 = zoomBorder1.ZoomX;

        var zoomBorder2 = await CreateAsync(zb =>
        {
            zb.WheelBehavior = WheelBehaviorMode.Zoom;
            zb.WheelZoomSensitivity = 2.0; // Double sensitivity
            zb.EnableZoom = true;
        });

        PointerTestHelpers.MouseWheelAt(zoomBorder2, new Point(200, 150), 120);
        await ZoomBorderTestHelper.WaitForIdleAsync();
        var zoom2 = zoomBorder2.ZoomX;

        Assert.IsTrue(zoom2 > zoom1, $"Higher sensitivity should zoom more ({zoom2} > {zoom1})");
    }
}

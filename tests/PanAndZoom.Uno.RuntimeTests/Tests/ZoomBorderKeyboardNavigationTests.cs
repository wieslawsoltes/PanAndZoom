// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using Windows.System;

namespace PanAndZoom.Uno.RuntimeTests.Tests;

/// <summary>
/// Port of the Avalonia <c>ZoomBorderKeyboardNavigationTests</c>.
/// </summary>
/// <remarks>
/// Uno does not implement keyboard injection on Skia, so keys are routed through
/// <see cref="ZoomBorderTestHelper.PressKey"/> (the handler used by <c>OnKeyDown</c>) instead of raising <c>KeyDown</c>.
/// Avalonia <c>Key.OemPlus</c>/<c>Key.OemMinus</c> are <c>VK_OEM_PLUS</c> (187) and <c>VK_OEM_MINUS</c> (189).
/// </remarks>
[TestClass]
[RunsOnUIThread]
public class ZoomBorderKeyboardNavigationTests
{
    private const VirtualKey OemPlus = (VirtualKey)187;
    private const VirtualKey OemMinus = (VirtualKey)189;

    [TestCleanup]
    public async Task Cleanup()
    {
        InputHelper.Reset();
        await ZoomBorderTestHelper.UnloadAsync();
    }

    private static async Task<ZoomBorder> CreateAsync(Action<ZoomBorder> configure)
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150, configure);
        return zoomBorder;
    }

    [TestMethod]
    public void EnableKeyboardNavigation_DefaultValue_IsTrue()
    {
        var zoomBorder = new ZoomBorder();

        Assert.IsTrue(zoomBorder.EnableKeyboardNavigation);
    }

    [TestMethod]
    public void EnableKeyboardNavigation_CanBeSetToFalse()
    {
        var zoomBorder = new ZoomBorder();

        zoomBorder.EnableKeyboardNavigation = false;

        Assert.IsFalse(zoomBorder.EnableKeyboardNavigation);
    }

    [TestMethod]
    public void KeyboardPanStep_DefaultValue_Is50()
    {
        var zoomBorder = new ZoomBorder();

        Assert.AreEqual(50.0, zoomBorder.KeyboardPanStep);
    }

    [TestMethod]
    public void KeyboardPanStep_CanBeSetToCustomValue()
    {
        var zoomBorder = new ZoomBorder();

        zoomBorder.KeyboardPanStep = 100.0;

        Assert.AreEqual(100.0, zoomBorder.KeyboardPanStep);
    }

    [TestMethod]
    public void KeyboardZoomStep_DefaultValue_Is1Point1()
    {
        var zoomBorder = new ZoomBorder();

        Assert.AreEqual(1.1, zoomBorder.KeyboardZoomStep);
    }

    [TestMethod]
    public void KeyboardZoomStep_CanBeSetToCustomValue()
    {
        var zoomBorder = new ZoomBorder();

        zoomBorder.KeyboardZoomStep = 1.5;

        Assert.AreEqual(1.5, zoomBorder.KeyboardZoomStep);
    }

    [TestMethod]
    public void KeyboardNavigation_AllPropertiesCanBeSetTogether()
    {
        var zoomBorder = new ZoomBorder
        {
            EnableKeyboardNavigation = false,
            KeyboardPanStep = 75.0,
            KeyboardZoomStep = 1.25
        };

        Assert.IsFalse(zoomBorder.EnableKeyboardNavigation);
        Assert.AreEqual(75.0, zoomBorder.KeyboardPanStep);
        Assert.AreEqual(1.25, zoomBorder.KeyboardZoomStep);
    }

    [TestMethod]
    public async Task KeyboardNavigation_LeftArrow_PansLeft()
    {
        var zoomBorder = await CreateAsync(zb =>
        {
            zb.EnableKeyboardNavigation = true;
            zb.EnablePan = true;
            zb.KeyboardPanStep = 50.0;
        });

        var initialOffsetX = zoomBorder.OffsetX;

        Assert.IsTrue(ZoomBorderTestHelper.PressKey(zoomBorder, VirtualKey.Left));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(zoomBorder.OffsetX < initialOffsetX, "Offset should decrease when panning left");
    }

    [TestMethod]
    public async Task KeyboardNavigation_RightArrow_PansRight()
    {
        var zoomBorder = await CreateAsync(zb =>
        {
            zb.EnableKeyboardNavigation = true;
            zb.EnablePan = true;
            zb.KeyboardPanStep = 50.0;
        });

        var initialOffsetX = zoomBorder.OffsetX;

        Assert.IsTrue(ZoomBorderTestHelper.PressKey(zoomBorder, VirtualKey.Right));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(zoomBorder.OffsetX > initialOffsetX, "Offset should increase when panning right");
    }

    [TestMethod]
    public async Task KeyboardNavigation_UpArrow_PansUp()
    {
        var zoomBorder = await CreateAsync(zb =>
        {
            zb.EnableKeyboardNavigation = true;
            zb.EnablePan = true;
            zb.KeyboardPanStep = 50.0;
        });

        var initialOffsetY = zoomBorder.OffsetY;

        Assert.IsTrue(ZoomBorderTestHelper.PressKey(zoomBorder, VirtualKey.Up));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(zoomBorder.OffsetY < initialOffsetY, "Offset should decrease when panning up");
    }

    [TestMethod]
    public async Task KeyboardNavigation_DownArrow_PansDown()
    {
        var zoomBorder = await CreateAsync(zb =>
        {
            zb.EnableKeyboardNavigation = true;
            zb.EnablePan = true;
            zb.KeyboardPanStep = 50.0;
        });

        var initialOffsetY = zoomBorder.OffsetY;

        Assert.IsTrue(ZoomBorderTestHelper.PressKey(zoomBorder, VirtualKey.Down));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(zoomBorder.OffsetY > initialOffsetY, "Offset should increase when panning down");
    }

    [TestMethod]
    public async Task KeyboardNavigation_PlusKey_ZoomsIn()
    {
        var zoomBorder = await CreateAsync(zb =>
        {
            zb.EnableKeyboardNavigation = true;
            zb.EnableZoom = true;
            zb.KeyboardZoomStep = 1.5;
        });

        var initialZoom = zoomBorder.ZoomX;

        Assert.IsTrue(ZoomBorderTestHelper.PressKey(zoomBorder, OemPlus));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(zoomBorder.ZoomX > initialZoom, "Zoom should increase with Plus key");
    }

    [TestMethod]
    public async Task KeyboardNavigation_MinusKey_ZoomsOut()
    {
        var zoomBorder = await CreateAsync(zb =>
        {
            zb.EnableKeyboardNavigation = true;
            zb.EnableZoom = true;
            zb.KeyboardZoomStep = 1.5;
        });

        // Zoom in first.
        zoomBorder.ZoomIn();
        var initialZoom = zoomBorder.ZoomX;

        Assert.IsTrue(ZoomBorderTestHelper.PressKey(zoomBorder, OemMinus));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(zoomBorder.ZoomX < initialZoom, "Zoom should decrease with Minus key");
    }

    [TestMethod]
    public async Task KeyboardNavigation_HomeKey_FitsToViewport()
    {
        var zoomBorder = await CreateAsync(zb =>
        {
            zb.EnableKeyboardNavigation = true;
            zb.Stretch = StretchMode.Uniform;
        });

        // Zoom in first.
        zoomBorder.ZoomIn();
        var zoomBeforeFit = zoomBorder.ZoomX;

        Assert.IsTrue(ZoomBorderTestHelper.PressKey(zoomBorder, VirtualKey.Home));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        // AutoFit should apply.
        Assert.IsTrue(zoomBorder.ZoomX != zoomBeforeFit, "Home key should trigger AutoFit");
    }

    [TestMethod]
    public async Task KeyboardNavigation_Ctrl0_ResetsView()
    {
        var zoomBorder = await CreateAsync(zb => zb.EnableKeyboardNavigation = true);

        // Zoom and pan.
        zoomBorder.ZoomIn();
        zoomBorder.Pan(50, 50);

        Assert.IsTrue(ZoomBorderTestHelper.PressKey(zoomBorder, VirtualKey.Number0, ZoomBorderKeyModifiers.Control));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        // Should reset to identity matrix.
        Assert.AreEqual(1.0, zoomBorder.ZoomX);
        Assert.AreEqual(1.0, zoomBorder.ZoomY);
        Assert.AreEqual(0.0, zoomBorder.OffsetX);
        Assert.AreEqual(0.0, zoomBorder.OffsetY);
    }

    [TestMethod]
    public async Task KeyboardNavigation_DisabledWhenEnableKeyboardNavigationIsFalse()
    {
        var zoomBorder = await CreateAsync(zb =>
        {
            zb.EnableKeyboardNavigation = false;
            zb.EnablePan = true;
            zb.KeyboardPanStep = 50.0;
        });

        var initialOffsetX = zoomBorder.OffsetX;

        Assert.IsFalse(ZoomBorderTestHelper.PressKey(zoomBorder, VirtualKey.Left), "Key should not be handled when keyboard navigation is disabled");
        await ZoomBorderTestHelper.WaitForIdleAsync();

        // Offset should not change when keyboard navigation is disabled.
        Assert.AreEqual(initialOffsetX, zoomBorder.OffsetX);
    }

    [TestMethod]
    public async Task CtrlLeft_NavigatesBack_WhenViewHistoryEnabled()
    {
        var zoomBorder = await CreateAsync(zb =>
        {
            zb.EnableKeyboardNavigation = true;
            zb.EnableViewHistory = true;
            zb.Stretch = StretchMode.None;
        });

        // Create history.
        zoomBorder.ZoomIn();
        var zoomAfterIn = zoomBorder.ZoomX;
        zoomBorder.ZoomIn();

        // Ctrl+Left should navigate back.
        Assert.IsTrue(ZoomBorderTestHelper.PressKey(zoomBorder, VirtualKey.Left, ZoomBorderKeyModifiers.Control));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        // The behavior depends on whether view history is populated (the Avalonia test only asserts it does not throw).
        Assert.IsNotNull(zoomBorder);
        Assert.AreEqual(zoomAfterIn, zoomBorder.ZoomX, 1e-9, "Ctrl+Left should navigate back to the previous view");
    }

    [TestMethod]
    public async Task CtrlRight_NavigatesForward_WhenViewHistoryEnabled()
    {
        var zoomBorder = await CreateAsync(zb =>
        {
            zb.EnableKeyboardNavigation = true;
            zb.EnableViewHistory = true;
            zb.Stretch = StretchMode.None;
        });

        // Create history and navigate back.
        zoomBorder.ZoomIn();
        zoomBorder.ZoomIn();
        var zoomAfterSecondIn = zoomBorder.ZoomX;
        zoomBorder.NavigateBack();

        // Ctrl+Right should navigate forward.
        Assert.IsTrue(ZoomBorderTestHelper.PressKey(zoomBorder, VirtualKey.Right, ZoomBorderKeyModifiers.Control));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsNotNull(zoomBorder);
        Assert.AreEqual(zoomAfterSecondIn, zoomBorder.ZoomX, 1e-9, "Ctrl+Right should navigate forward to the next view");
    }

    [TestMethod]
    public async Task PlusKey_ZoomsIn_WhenEnabled()
    {
        var zoomBorder = await CreateAsync(zb =>
        {
            zb.EnableKeyboardNavigation = true;
            zb.EnableZoom = true;
            zb.Stretch = StretchMode.None;
        });

        var initialZoom = zoomBorder.ZoomX;

        Assert.IsTrue(ZoomBorderTestHelper.PressKey(zoomBorder, VirtualKey.Add));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(zoomBorder.ZoomX > initialZoom);
    }

    [TestMethod]
    public async Task MinusKey_ZoomsOut_WhenEnabled()
    {
        var zoomBorder = await CreateAsync(zb =>
        {
            zb.EnableKeyboardNavigation = true;
            zb.EnableZoom = true;
            zb.Stretch = StretchMode.None;
        });

        // First zoom in.
        zoomBorder.ZoomIn();
        var initialZoom = zoomBorder.ZoomX;

        Assert.IsTrue(ZoomBorderTestHelper.PressKey(zoomBorder, VirtualKey.Subtract));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(zoomBorder.ZoomX < initialZoom);
    }

    [TestMethod]
    public async Task HomeKey_FitsToViewport()
    {
        var zoomBorder = await CreateAsync(zb =>
        {
            zb.EnableKeyboardNavigation = true;
            zb.Stretch = StretchMode.None;
        });

        // First zoom in.
        zoomBorder.ZoomIn();
        zoomBorder.ZoomIn();

        Assert.IsTrue(ZoomBorderTestHelper.PressKey(zoomBorder, VirtualKey.Home));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        // Just verify it executes without error.
        Assert.IsNotNull(zoomBorder);
    }
}

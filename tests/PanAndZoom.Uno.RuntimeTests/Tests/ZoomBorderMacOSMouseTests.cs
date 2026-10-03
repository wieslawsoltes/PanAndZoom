// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
namespace PanAndZoom.Uno.RuntimeTests.Tests;

/// <summary>
/// Regression tests for real mouse input routed through the Uno macOS host
/// (the macOS host does not report the middle button in PointerPointProperties).
/// </summary>
[TestClass]
[RunsOnUIThread]
public class ZoomBorderMacOSMouseTests
{
    [TestCleanup]
    public async Task Cleanup()
    {
        InputHelper.Reset();
        await ZoomBorderTestHelper.UnloadAsync();
    }

    [TestMethod]
    public async Task MacOSHost_MiddleButtonDrag_Pans()
    {
        if (!MacOSNativeMouse.IsAvailable)
        {
            Assert.Inconclusive("Requires the Uno macOS Skia host.");
        }

        var (zb, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 400, 400, 400, z => z.Stretch = StretchMode.None);

        MacOSNativeMouse.Drag(ZoomBorderTestHelper.ToWindow(zb, new Point(100, 100)), ZoomBorderTestHelper.ToWindow(zb, new Point(150, 130)), MacOSNativeMouse.MiddleButton);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsFalse(zb.IsPanning);
        Assert.AreEqual(50, zb.OffsetX, 1.0);
        Assert.AreEqual(30, zb.OffsetY, 1.0);
    }

    [TestMethod]
    public async Task MacOSHost_LeftButtonDrag_WithLeftPanButton_Pans()
    {
        if (!MacOSNativeMouse.IsAvailable)
        {
            Assert.Inconclusive("Requires the Uno macOS Skia host.");
        }

        var (zb, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 400, 400, 400, z => { z.Stretch = StretchMode.None; z.PanButton = ButtonName.Left; });

        MacOSNativeMouse.Drag(ZoomBorderTestHelper.ToWindow(zb, new Point(100, 100)), ZoomBorderTestHelper.ToWindow(zb, new Point(140, 120)), MacOSNativeMouse.LeftButton);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.AreEqual(40, zb.OffsetX, 1.0);
        Assert.AreEqual(20, zb.OffsetY, 1.0);
    }

    [TestMethod]
    public async Task MacOSHost_MiddleButtonDrag_WithLeftPanButton_DoesNotPan()
    {
        if (!MacOSNativeMouse.IsAvailable)
        {
            Assert.Inconclusive("Requires the Uno macOS Skia host.");
        }

        var (zb, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 400, 400, 400, z => { z.Stretch = StretchMode.None; z.PanButton = ButtonName.Left; });

        MacOSNativeMouse.Drag(ZoomBorderTestHelper.ToWindow(zb, new Point(100, 100)), ZoomBorderTestHelper.ToWindow(zb, new Point(150, 130)), MacOSNativeMouse.MiddleButton);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.AreEqual(0, zb.OffsetX, 1e-9);
        Assert.AreEqual(0, zb.OffsetY, 1e-9);
    }
}

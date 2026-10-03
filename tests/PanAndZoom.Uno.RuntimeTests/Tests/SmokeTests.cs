// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using Windows.System;

namespace PanAndZoom.Uno.RuntimeTests.Tests;

[TestClass]
[RunsOnUIThread]
public class SmokeTests
{
    [TestCleanup]
    public async Task Cleanup()
    {
        InputHelper.Reset();
        await ZoomBorderTestHelper.UnloadAsync();
    }

    [TestMethod]
    public async Task Loads_And_Fits_Content()
    {
        var (zb, child) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 400, 200, 100);
        Assert.AreEqual(2.0, zb.ZoomX, 1e-6);
        Console.WriteLine($"[Smoke] Offset {zb.OffsetX},{zb.OffsetY} layout {child.ActualOffset} render {ZoomBorderTestHelper.GetChildRenderMatrix(zb)}");
    }

    [TestMethod]
    public async Task MiddleButtonDrag_Pans()
    {
        var (zb, child) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 400, 400, 400, z => z.Stretch = StretchMode.None);
        var from = ZoomBorderTestHelper.ToWindow(zb, new Point(100, 100));
        var to = ZoomBorderTestHelper.ToWindow(zb, new Point(150, 130));
        InputHelper.MouseDrag(from, to, ButtonName.Middle);
        await ZoomBorderTestHelper.WaitForIdleAsync();
        Console.WriteLine($"[Smoke] Pan offset {zb.OffsetX},{zb.OffsetY}");
        Assert.AreEqual(50, zb.OffsetX, 1.0);
        Assert.AreEqual(30, zb.OffsetY, 1.0);
    }

    [TestMethod]
    public async Task Wheel_Zooms()
    {
        var (zb, child) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 400, 400, 400, z => z.Stretch = StretchMode.None);
        InputHelper.MouseWheel(ZoomBorderTestHelper.ToWindow(zb, new Point(200, 200)), 120);
        await ZoomBorderTestHelper.WaitForIdleAsync();
        Console.WriteLine($"[Smoke] Wheel zoom {zb.ZoomX} offset {zb.OffsetX},{zb.OffsetY}");
        Assert.AreEqual(1.2, zb.ZoomX, 1e-6);
    }

    [TestMethod]
    public async Task ShiftWheel_PansHorizontally()
    {
        var (zb, child) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 400, 400, 400, z => z.Stretch = StretchMode.None);
        InputHelper.MouseWheel(ZoomBorderTestHelper.ToWindow(zb, new Point(200, 200)), 120, VirtualKeyModifiers.Shift);
        await ZoomBorderTestHelper.WaitForIdleAsync();
        Console.WriteLine($"[Smoke] Shift+Wheel zoom {zb.ZoomX} offset {zb.OffsetX},{zb.OffsetY}");
        Assert.AreEqual(1.0, zb.ZoomX, 1e-6);
        Assert.AreEqual(10, zb.OffsetX, 1e-6);
    }

    [TestMethod]
    public async Task DoubleClick_Zooms()
    {
        var (zb, child) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 400, 400, 400, z => z.Stretch = StretchMode.None);
        InputHelper.MouseDoubleClick(ZoomBorderTestHelper.ToWindow(zb, new Point(200, 200)));
        await ZoomBorderTestHelper.WaitForIdleAsync();
        Console.WriteLine($"[Smoke] DoubleClick zoom {zb.ZoomX}");
        Assert.AreEqual(2.0, zb.ZoomX, 1e-6);
    }

    [TestMethod]
    public async Task TouchDrag_Pans()
    {
        var (zb, child) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 400, 400, 400, z => z.Stretch = StretchMode.None);
        InputHelper.TouchDrag(ZoomBorderTestHelper.ToWindow(zb, new Point(100, 100)), ZoomBorderTestHelper.ToWindow(zb, new Point(160, 140)));
        await ZoomBorderTestHelper.WaitForIdleAsync();
        Console.WriteLine($"[Smoke] TouchDrag offset {zb.OffsetX},{zb.OffsetY}");
        Assert.IsTrue(zb.OffsetX > 30);
    }

    [TestMethod]
    public async Task Pinch_Zooms()
    {
        var (zb, child) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 400, 400, 400, z => { z.Stretch = StretchMode.None; z.EnableGestureRotation = false; });
        var gestureStarted = 0;
        zb.GestureStarted += (_, e) => { if (e.GestureType == "Pinch") gestureStarted++; };
        InputHelper.Pinch(ZoomBorderTestHelper.ToWindow(zb, new Point(200, 200)), 100, 200);
        await ZoomBorderTestHelper.WaitForIdleAsync();
        Console.WriteLine($"[Smoke] Pinch zoom {zb.ZoomX} gestures {gestureStarted}");
        Assert.IsTrue(zb.ZoomX > 1.5, $"ZoomX={zb.ZoomX}");
    }

    [TestMethod]
    public async Task Keyboard_Pans()
    {
        var (zb, child) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 400, 400, 400, z => z.Stretch = StretchMode.None);
        Assert.IsTrue(ZoomBorderTestHelper.PressKey(zb, VirtualKey.Right));
        Assert.AreEqual(50, zb.OffsetX, 1e-6);
    }

    [TestMethod]
    public async Task Animation_Runs()
    {
        var (zb, child) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 400, 400, 400, z => { z.Stretch = StretchMode.None; z.EnableAnimations = true; z.AnimationDuration = TimeSpan.FromMilliseconds(150); });
        zb.ZoomTo(2.0, 0, 0);
        Assert.IsTrue(zb.IsAnimating);
        Assert.IsTrue(await ZoomBorderTestHelper.WaitForAsync(() => !zb.IsAnimating));
        Assert.AreEqual(2.0, ZoomBorderTestHelper.GetChildRenderMatrix(zb).M11, 1e-6);
    }
}

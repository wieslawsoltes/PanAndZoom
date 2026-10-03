// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using Microsoft.UI;

namespace PanAndZoom.Uno.RuntimeTests.Tests;

/// <summary>
/// Tests for resize behavior functionality.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class ZoomBorderResizeBehaviorTests
{
    [TestCleanup]
    public async Task Cleanup()
    {
        InputHelper.Reset();
        await ZoomBorderTestHelper.UnloadAsync();
    }

    private static Border CreateChild(double width, double height)
    {
        return new Border
        {
            Width = width,
            Height = height,
            Background = new SolidColorBrush(Colors.Red)
        };
    }

    private static async Task ResizeAsync(ZoomBorder zoomBorder, double width, double height)
    {
        // Uno layout is asynchronous (Avalonia calls window.UpdateLayout()): wait for the new size to be arranged.
        zoomBorder.Width = width;
        zoomBorder.Height = height;
        await ZoomBorderTestHelper.WaitForIdleAsync();
        Assert.IsTrue(await ZoomBorderTestHelper.WaitForAsync(() => zoomBorder.ActualWidth == width && zoomBorder.ActualHeight == height));
    }

    [TestMethod]
    public void ResizeBehavior_DefaultValue_IsNone()
    {
        var zoomBorder = new ZoomBorder();

        Assert.AreEqual(ResizeBehaviorMode.None, zoomBorder.ResizeBehavior);
    }

    [TestMethod]
    public void ResizeBehavior_CanBeSetToNone()
    {
        var zoomBorder = new ZoomBorder();

        zoomBorder.ResizeBehavior = ResizeBehaviorMode.None;

        Assert.AreEqual(ResizeBehaviorMode.None, zoomBorder.ResizeBehavior);
    }

    [TestMethod]
    public void ResizeBehavior_CanBeSetToMaintainCenter()
    {
        var zoomBorder = new ZoomBorder();

        zoomBorder.ResizeBehavior = ResizeBehaviorMode.MaintainCenter;

        Assert.AreEqual(ResizeBehaviorMode.MaintainCenter, zoomBorder.ResizeBehavior);
    }

    [TestMethod]
    public void ResizeBehavior_CanBeSetToMaintainTopLeft()
    {
        var zoomBorder = new ZoomBorder();

        zoomBorder.ResizeBehavior = ResizeBehaviorMode.MaintainTopLeft;

        Assert.AreEqual(ResizeBehaviorMode.MaintainTopLeft, zoomBorder.ResizeBehavior);
    }

    [TestMethod]
    public void ResizeBehavior_CanBeSetToMaintainZoom()
    {
        var zoomBorder = new ZoomBorder();

        zoomBorder.ResizeBehavior = ResizeBehaviorMode.MaintainZoom;

        Assert.AreEqual(ResizeBehaviorMode.MaintainZoom, zoomBorder.ResizeBehavior);
    }

    [TestMethod]
    public void ResizeBehavior_CanBeSetToReapplyStretch()
    {
        var zoomBorder = new ZoomBorder();

        zoomBorder.ResizeBehavior = ResizeBehaviorMode.ReapplyStretch;

        Assert.AreEqual(ResizeBehaviorMode.ReapplyStretch, zoomBorder.ResizeBehavior);
    }

    [TestMethod]
    public void ResizeBehavior_CanBeSetToCustom()
    {
        var zoomBorder = new ZoomBorder();

        zoomBorder.ResizeBehavior = ResizeBehaviorMode.Custom;

        Assert.AreEqual(ResizeBehaviorMode.Custom, zoomBorder.ResizeBehavior);
    }

    [TestMethod]
    public async Task ResizeBehavior_None_MaintainsCurrentOffsetOnResize()
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150,
            zb => zb.ResizeBehavior = ResizeBehaviorMode.None);

        var initialOffsetX = zoomBorder.OffsetX;
        var initialOffsetY = zoomBorder.OffsetY;

        await ResizeAsync(zoomBorder, 600, 450);

        // Offset should remain the same with None mode
        Assert.AreEqual(initialOffsetX, zoomBorder.OffsetX);
        Assert.AreEqual(initialOffsetY, zoomBorder.OffsetY);
    }

    [TestMethod]
    [DataRow(ResizeBehaviorMode.None)]
    [DataRow(ResizeBehaviorMode.MaintainCenter)]
    [DataRow(ResizeBehaviorMode.MaintainTopLeft)]
    [DataRow(ResizeBehaviorMode.MaintainZoom)]
    [DataRow(ResizeBehaviorMode.Custom)]
    public async Task ResizeBehavior_DoesNotReapplyStretchUnlessRequested(ResizeBehaviorMode resizeBehavior)
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150, zb =>
        {
            zb.ResizeBehavior = resizeBehavior;
            zb.Stretch = StretchMode.Uniform;
        });

        zoomBorder.ZoomTo(1.25, 100, 75, skipTransitions: true);
        var zoomBeforeResize = zoomBorder.ZoomX;

        await ResizeAsync(zoomBorder, 600, 450);

        Assert.AreEqual(2.5, zoomBeforeResize, 1e-5);
        Assert.AreEqual(zoomBeforeResize, zoomBorder.ZoomX, 1e-5);
        Assert.AreEqual(zoomBeforeResize, zoomBorder.ZoomY, 1e-5);
    }

    [TestMethod]
    public async Task ChangingStretch_ReappliesTheNewStretchMode()
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 100, zb =>
        {
            zb.ResizeBehavior = ResizeBehaviorMode.None;
            zb.Stretch = StretchMode.Uniform;
        });

        Assert.AreEqual(2.0, zoomBorder.ZoomX, 1e-5);

        zoomBorder.Stretch = StretchMode.UniformToFill;
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.AreEqual(3.0, zoomBorder.ZoomX, 1e-5);
        Assert.AreEqual(3.0, zoomBorder.ZoomY, 1e-5);
    }

    [TestMethod]
    public async Task ResizeBehavior_MaintainZoom_PreservesZoomLevelOnResize()
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150,
            zb => zb.ResizeBehavior = ResizeBehaviorMode.MaintainZoom);

        zoomBorder.Pan(50, 50);
        var initialZoomX = zoomBorder.ZoomX;

        await ResizeAsync(zoomBorder, 800, 600);

        Assert.AreEqual(initialZoomX, zoomBorder.ZoomX);
    }

    [TestMethod]
    public async Task ResizeBehavior_MaintainCenter_TriggersOnBoundsChange()
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150, zb =>
        {
            zb.ResizeBehavior = ResizeBehaviorMode.MaintainCenter;
            zb.Stretch = StretchMode.None;
        });

        zoomBorder.ZoomIn();
        zoomBorder.Pan(50, 50);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        await ResizeAsync(zoomBorder, 600, 450);

        // Just verify it didn't throw
        Assert.IsNotNull(zoomBorder);
    }

    [TestMethod]
    public async Task ResizeBehavior_ReapplyStretch_TriggersAutoFit()
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150, zb =>
        {
            zb.ResizeBehavior = ResizeBehaviorMode.ReapplyStretch;
            zb.Stretch = StretchMode.Uniform;
        });

        Assert.AreEqual(2.0, zoomBorder.ZoomX, 1e-6);

        await ResizeAsync(zoomBorder, 600, 450);

        // Should auto fit after resize (Uniform: 600 / 200 = 450 / 150 = 3)
        Assert.IsNotNull(zoomBorder);
        Assert.AreEqual(3.0, zoomBorder.ZoomX, 1e-6);
        Assert.AreEqual(3.0, zoomBorder.ZoomY, 1e-6);
    }

    [TestMethod]
    public async Task ResizeBehavior_Custom_TriggersOnResizedMethod()
    {
        var zoomBorder = new TestableZoomBorder
        {
            Width = 400,
            Height = 300,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            ResizeBehavior = ResizeBehaviorMode.Custom,
            Stretch = StretchMode.None,
            Child = CreateChild(200, 150)
        };

        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        await ResizeAsync(zoomBorder, 600, 450);

        // OnResized should have been called
        Assert.IsNotNull(zoomBorder);
        Assert.IsTrue(zoomBorder.OnResizedCalled);
        Assert.AreEqual(new Size(400, 300), zoomBorder.LastOldSize);
        Assert.AreEqual(new Size(600, 450), zoomBorder.LastNewSize);
    }
}

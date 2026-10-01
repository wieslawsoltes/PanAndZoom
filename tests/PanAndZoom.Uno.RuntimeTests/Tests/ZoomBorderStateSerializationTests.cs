// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

namespace PanAndZoom.Uno.RuntimeTests.Tests;

/// <summary>
/// Tests for ZoomBorder state serialization functionality.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class ZoomBorderStateSerializationTests
{
    [TestCleanup]
    public async Task Cleanup()
    {
        InputHelper.Reset();
        await ZoomBorderTestHelper.UnloadAsync();
    }

    [TestMethod]
    public async Task ExportState_ReturnsZoomBorderState()
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150, zb =>
        {
            zb.ZoomSpeed = 1.5;
            zb.EnablePan = true;
            zb.EnableZoom = true;
        });

        var state = zoomBorder.ExportState();

        Assert.IsNotNull(state);
        Assert.AreEqual(zoomBorder.ZoomSpeed, state.ZoomSpeed);
        Assert.AreEqual(zoomBorder.EnablePan, state.EnablePan);
        Assert.AreEqual(zoomBorder.EnableZoom, state.EnableZoom);
    }

    [TestMethod]
    public async Task ExportState_CapturesMatrix()
    {
        var (zoomBorder, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150);

        zoomBorder.Zoom(2.0, 100, 75);

        var state = zoomBorder.ExportState();

        Assert.IsNotNull(state);
        Assert.AreNotEqual(Matrix.Identity, state.Matrix);
    }

    [TestMethod]
    public void ExportState_CapturesRotation()
    {
        var zoomBorder = new ZoomBorder
        {
            Rotation = 45.0
        };

        var state = zoomBorder.ExportState();

        Assert.AreEqual(45.0, state.Rotation);
    }

    [TestMethod]
    public void ExportState_CapturesStretchMode()
    {
        var zoomBorder = new ZoomBorder
        {
            Stretch = StretchMode.Fill
        };

        var state = zoomBorder.ExportState();

        Assert.AreEqual(StretchMode.Fill, state.Stretch);
    }

    [TestMethod]
    public void ExportState_CapturesTimestamp()
    {
        var zoomBorder = new ZoomBorder();
        var beforeExport = DateTime.UtcNow;

        var state = zoomBorder.ExportState();
        var afterExport = DateTime.UtcNow;

        Assert.IsTrue(state.Timestamp >= beforeExport);
        Assert.IsTrue(state.Timestamp <= afterExport);
    }

    [TestMethod]
    public void ImportState_RestoresZoomSpeed()
    {
        var zoomBorder = new ZoomBorder();
        var state = new ZoomBorderState
        {
            ZoomSpeed = 2.0,
            Matrix = Matrix.Identity,
            Stretch = StretchMode.None,
            EnablePan = true,
            EnableZoom = true
        };

        zoomBorder.ImportState(state);

        Assert.AreEqual(2.0, zoomBorder.ZoomSpeed);
    }

    [TestMethod]
    public void ImportState_RestoresEnablePan()
    {
        var zoomBorder = new ZoomBorder();
        var state = new ZoomBorderState
        {
            EnablePan = false,
            Matrix = Matrix.Identity,
            Stretch = StretchMode.None
        };

        zoomBorder.ImportState(state);

        Assert.IsFalse(zoomBorder.EnablePan);
    }

    [TestMethod]
    public void ImportState_RestoresEnableZoom()
    {
        var zoomBorder = new ZoomBorder();
        var state = new ZoomBorderState
        {
            EnableZoom = false,
            Matrix = Matrix.Identity,
            Stretch = StretchMode.None
        };

        zoomBorder.ImportState(state);

        Assert.IsFalse(zoomBorder.EnableZoom);
    }

    [TestMethod]
    public void ImportState_RestoresRotation()
    {
        var zoomBorder = new ZoomBorder();
        var state = new ZoomBorderState
        {
            Rotation = 90.0,
            Matrix = Matrix.Identity,
            Stretch = StretchMode.None
        };

        zoomBorder.ImportState(state);

        Assert.AreEqual(90.0, zoomBorder.Rotation);
    }

    [TestMethod]
    public void ImportState_RestoresStretchMode()
    {
        var zoomBorder = new ZoomBorder();
        var state = new ZoomBorderState
        {
            Stretch = StretchMode.Uniform,
            Matrix = Matrix.Identity
        };

        zoomBorder.ImportState(state);

        Assert.AreEqual(StretchMode.Uniform, zoomBorder.Stretch);
    }

    [TestMethod]
    public void ImportState_RestoresZoomLimits()
    {
        var zoomBorder = new ZoomBorder();
        var state = new ZoomBorderState
        {
            MinZoomX = 0.5,
            MaxZoomX = 10.0,
            MinZoomY = 0.5,
            MaxZoomY = 10.0,
            Matrix = Matrix.Identity,
            Stretch = StretchMode.None
        };

        zoomBorder.ImportState(state);

        Assert.AreEqual(0.5, zoomBorder.MinZoomX);
        Assert.AreEqual(10.0, zoomBorder.MaxZoomX);
        Assert.AreEqual(0.5, zoomBorder.MinZoomY);
        Assert.AreEqual(10.0, zoomBorder.MaxZoomY);
    }

    [TestMethod]
    public async Task ExportAndImportState_RoundTrip_PreservesState()
    {
        var (zoomBorder1, _) = await ZoomBorderTestHelper.CreateAndLoadAsync(400, 300, 200, 150, zb =>
        {
            zb.ZoomSpeed = 1.5;
            zb.EnablePan = false;
            zb.EnableZoom = true;
            zb.Rotation = 45.0;
            zb.Stretch = StretchMode.Fill;
        });

        zoomBorder1.Zoom(2.0, 100, 75);

        var zoomBorder2 = new ZoomBorder();

        var state = zoomBorder1.ExportState();
        zoomBorder2.ImportState(state);

        Assert.AreEqual(zoomBorder1.ZoomSpeed, zoomBorder2.ZoomSpeed);
        Assert.AreEqual(zoomBorder1.EnablePan, zoomBorder2.EnablePan);
        Assert.AreEqual(zoomBorder1.EnableZoom, zoomBorder2.EnableZoom);
        Assert.AreEqual(zoomBorder1.Rotation, zoomBorder2.Rotation);
        Assert.AreEqual(zoomBorder1.Stretch, zoomBorder2.Stretch);
    }
}

// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using Microsoft.UI;

namespace PanAndZoom.Uno.RuntimeTests.Tests;

/// <summary>
/// Tests for ZoomBorder grid and snap functionality.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class ZoomBorderGridAndSnapTests
{
    [TestMethod]
    public void ShowGrid_DefaultValue_IsFalse()
    {
        var zoomBorder = new ZoomBorder();

        Assert.IsFalse(zoomBorder.ShowGrid);
    }

    [TestMethod]
    public void EnableSnapToGrid_DefaultValue_IsFalse()
    {
        var zoomBorder = new ZoomBorder();

        Assert.IsFalse(zoomBorder.EnableSnapToGrid);
    }

    [TestMethod]
    public void GridSize_DefaultValue_Is50()
    {
        var zoomBorder = new ZoomBorder();

        Assert.AreEqual(50.0, zoomBorder.GridSize);
    }

    [TestMethod]
    public void GridThickness_DefaultValue_Is1()
    {
        var zoomBorder = new ZoomBorder();

        Assert.AreEqual(1.0, zoomBorder.GridThickness);
    }

    [TestMethod]
    public void GridOpacity_DefaultValue_Is03()
    {
        var zoomBorder = new ZoomBorder();

        Assert.AreEqual(0.3, zoomBorder.GridOpacity);
    }

    [TestMethod]
    public void MajorGridInterval_DefaultValue_Is5()
    {
        var zoomBorder = new ZoomBorder();

        Assert.AreEqual(5, zoomBorder.MajorGridInterval);
    }

    [TestMethod]
    public void MajorGridThickness_DefaultValue_Is2()
    {
        var zoomBorder = new ZoomBorder();

        Assert.AreEqual(2.0, zoomBorder.MajorGridThickness);
    }

    [TestMethod]
    public void ShowGrid_CanBeSet()
    {
        var zoomBorder = new ZoomBorder();

        zoomBorder.ShowGrid = true;

        Assert.IsTrue(zoomBorder.ShowGrid);
    }

    [TestMethod]
    public void EnableSnapToGrid_CanBeSet()
    {
        var zoomBorder = new ZoomBorder();

        zoomBorder.EnableSnapToGrid = true;

        Assert.IsTrue(zoomBorder.EnableSnapToGrid);
    }

    [TestMethod]
    public void GridSize_CanBeSet()
    {
        var zoomBorder = new ZoomBorder();

        zoomBorder.GridSize = 100.0;

        Assert.AreEqual(100.0, zoomBorder.GridSize);
    }

    [TestMethod]
    public void GridBrush_CanBeSet()
    {
        var zoomBorder = new ZoomBorder();
        var brush = new SolidColorBrush(Colors.Blue);

        zoomBorder.GridBrush = brush;

        Assert.AreSame(brush, zoomBorder.GridBrush);
    }

    [TestMethod]
    public void SnapToGrid_DoubleValue_SnapsToNearestGridPoint()
    {
        var zoomBorder = new ZoomBorder
        {
            EnableSnapToGrid = true,
            GridSize = 50.0
        };

        var snapped1 = zoomBorder.SnapToGrid(123.0);
        var snapped2 = zoomBorder.SnapToGrid(176.0);

        Assert.AreEqual(100.0, snapped1);
        Assert.AreEqual(200.0, snapped2);
    }

    [TestMethod]
    public void SnapToGrid_DoubleValue_ReturnsUnchangedWhenDisabled()
    {
        var zoomBorder = new ZoomBorder
        {
            EnableSnapToGrid = false,
            GridSize = 50.0
        };

        var snapped = zoomBorder.SnapToGrid(123.0);

        Assert.AreEqual(123.0, snapped);
    }

    [TestMethod]
    public void SnapToGrid_Point_SnapsToNearestGridPoint()
    {
        var zoomBorder = new ZoomBorder
        {
            EnableSnapToGrid = true,
            GridSize = 50.0
        };

        var point = new Point(123.0, 176.0);

        var snapped = zoomBorder.SnapToGrid(point);

        Assert.AreEqual(100.0, snapped.X);
        Assert.AreEqual(200.0, snapped.Y);
    }

    [TestMethod]
    public void SnapToGrid_Rect_SnapsToNearestGridPoints()
    {
        var zoomBorder = new ZoomBorder
        {
            EnableSnapToGrid = true,
            GridSize = 50.0
        };

        var rect = new Rect(123.0, 176.0, 100.0, 80.0);

        var snapped = zoomBorder.SnapToGrid(rect);

        Assert.AreEqual(100.0, snapped.X);
        Assert.AreEqual(200.0, snapped.Y);
        // Width and height are derived from snapped corners
        Assert.IsTrue(snapped.Width > 0);
        Assert.IsTrue(snapped.Height > 0);
    }

    [TestMethod]
    public void SnapToGrid_WithZeroGridSize_ReturnsOriginalValue()
    {
        var zoomBorder = new ZoomBorder
        {
            EnableSnapToGrid = true,
            GridSize = 0.0
        };

        var snapped = zoomBorder.SnapToGrid(123.0);

        Assert.AreEqual(123.0, snapped);
    }

    [TestMethod]
    public void SnapToGrid_WithNegativeGridSize_ReturnsOriginalValue()
    {
        var zoomBorder = new ZoomBorder
        {
            EnableSnapToGrid = true,
            GridSize = -50.0
        };

        var snapped = zoomBorder.SnapToGrid(123.0);

        Assert.AreEqual(123.0, snapped);
    }
}

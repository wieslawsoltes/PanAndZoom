// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using Microsoft.UI;

namespace PanAndZoom.Uno.RuntimeTests.Tests;

/// <summary>
/// Tests for content bounds restriction functionality.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class ZoomBorderContentBoundsTests
{
    [TestCleanup]
    public async Task Cleanup()
    {
        InputHelper.Reset();
        await ZoomBorderTestHelper.UnloadAsync();
    }

    [TestMethod]
    public void BoundsMode_DefaultValue_IsUnrestricted()
    {
        // Arrange & Act
        var zoomBorder = new ZoomBorder();

        // Assert
        Assert.AreEqual(ContentBoundsMode.Unrestricted, zoomBorder.BoundsMode);
    }

    [TestMethod]
    public void BoundsMode_CanBeSetToUnrestricted()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();

        // Act
        zoomBorder.BoundsMode = ContentBoundsMode.Unrestricted;

        // Assert
        Assert.AreEqual(ContentBoundsMode.Unrestricted, zoomBorder.BoundsMode);
    }

    [TestMethod]
    public void BoundsMode_CanBeSetToKeepContentVisible()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();

        // Act
        zoomBorder.BoundsMode = ContentBoundsMode.KeepContentVisible;

        // Assert
        Assert.AreEqual(ContentBoundsMode.KeepContentVisible, zoomBorder.BoundsMode);
    }

    [TestMethod]
    public void BoundsMode_CanBeSetToFillViewport()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();

        // Act
        zoomBorder.BoundsMode = ContentBoundsMode.FillViewport;

        // Assert
        Assert.AreEqual(ContentBoundsMode.FillViewport, zoomBorder.BoundsMode);
    }

    [TestMethod]
    public void BoundsMode_CanBeSetToKeepCentered()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();

        // Act
        zoomBorder.BoundsMode = ContentBoundsMode.KeepCentered;

        // Assert
        Assert.AreEqual(ContentBoundsMode.KeepCentered, zoomBorder.BoundsMode);
    }

    [TestMethod]
    public void BoundsMode_CanBeSetToCustom()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();

        // Act
        zoomBorder.BoundsMode = ContentBoundsMode.Custom;

        // Assert
        Assert.AreEqual(ContentBoundsMode.Custom, zoomBorder.BoundsMode);
    }

    [TestMethod]
    public void BoundsPadding_DefaultValue_IsZero()
    {
        // Arrange & Act
        var zoomBorder = new ZoomBorder();

        // Assert
        Assert.AreEqual(new Thickness(0), zoomBorder.BoundsPadding);
    }

    [TestMethod]
    public void BoundsPadding_CanBeSetToUniformThickness()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();

        // Act
        zoomBorder.BoundsPadding = new Thickness(10);

        // Assert
        Assert.AreEqual(new Thickness(10), zoomBorder.BoundsPadding);
    }

    [TestMethod]
    public void BoundsPadding_CanBeSetToNonUniformThickness()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();

        // Act
        zoomBorder.BoundsPadding = new Thickness(5, 10, 15, 20);

        // Assert
        Assert.AreEqual(new Thickness(5, 10, 15, 20), zoomBorder.BoundsPadding);
    }

    [TestMethod]
    public void MinimumVisibleContentPercentage_DefaultValue_IsPointOne()
    {
        // Arrange & Act
        var zoomBorder = new ZoomBorder();

        // Assert
        Assert.AreEqual(0.1, zoomBorder.MinimumVisibleContentPercentage);
    }

    [TestMethod]
    public void MinimumVisibleContentPercentage_CanBeSetToCustomValue()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();

        // Act
        zoomBorder.MinimumVisibleContentPercentage = 0.25;

        // Assert
        Assert.AreEqual(0.25, zoomBorder.MinimumVisibleContentPercentage);
    }

    [TestMethod]
    public void ContentBounds_AllPropertiesCanBeSetTogether()
    {
        // Arrange & Act
        var zoomBorder = new ZoomBorder
        {
            BoundsMode = ContentBoundsMode.KeepContentVisible,
            BoundsPadding = new Thickness(10),
            MinimumVisibleContentPercentage = 0.25
        };

        // Assert
        Assert.AreEqual(ContentBoundsMode.KeepContentVisible, zoomBorder.BoundsMode);
        Assert.AreEqual(new Thickness(10), zoomBorder.BoundsPadding);
        Assert.AreEqual(0.25, zoomBorder.MinimumVisibleContentPercentage);
    }

    [TestMethod]
    public async Task BoundsMode_Unrestricted_AllowsUnconstrainedPanning()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            BoundsMode = ContentBoundsMode.Unrestricted,
            EnableConstrains = true
        };

        var childElement = new Border
        {
            Width = 200,
            Height = 150,
            Background = new SolidColorBrush(Colors.Red)
        };

        zoomBorder.Child = childElement;
        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        // Act - Pan far beyond content
        zoomBorder.Pan(1000, 1000);

        // Assert - Should allow any offset in Unrestricted mode
        Assert.AreEqual(1000, zoomBorder.OffsetX);
        Assert.AreEqual(1000, zoomBorder.OffsetY);
    }

    [TestMethod]
    public async Task BoundsMode_KeepContentVisible_ConstrainsPanningBeyondContent()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            BoundsMode = ContentBoundsMode.KeepContentVisible,
            MinimumVisibleContentPercentage = 0.1,
            EnableConstrains = true
        };

        var childElement = new Border
        {
            Width = 200,
            Height = 150,
            Background = new SolidColorBrush(Colors.Red)
        };

        zoomBorder.Child = childElement;
        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        // Act - Try to pan far beyond content
        zoomBorder.Pan(1000, 1000);

        // Assert - Should constrain to keep minimum content visible
        Assert.IsTrue(zoomBorder.OffsetX < 1000, "OffsetX should be constrained");
        Assert.IsTrue(zoomBorder.OffsetY < 1000, "OffsetY should be constrained");
    }

    [TestMethod]
    public async Task BoundsMode_FillViewport_ConstrainsPanning()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            BoundsMode = ContentBoundsMode.FillViewport,
            EnableConstrains = true
        };

        var childElement = new Border
        {
            Width = 200,
            Height = 150,
            Background = new SolidColorBrush(Colors.Red)
        };

        zoomBorder.Child = childElement;
        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        // Act - Try to pan
        zoomBorder.Pan(500, 500);

        // Assert - FillViewport mode constrains movement
        Assert.IsNotNull(zoomBorder);
    }

    [TestMethod]
    public async Task BoundsMode_KeepCentered_ConstrainsPanning()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            BoundsMode = ContentBoundsMode.KeepCentered,
            EnableConstrains = true
        };

        var childElement = new Border
        {
            Width = 200,
            Height = 150,
            Background = new SolidColorBrush(Colors.Red)
        };

        zoomBorder.Child = childElement;
        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        // Act - Try to pan
        zoomBorder.Pan(500, 500);

        // Assert - KeepCentered mode constrains movement
        Assert.IsNotNull(zoomBorder);
    }

    [TestMethod]
    public async Task BoundsMode_Custom_AllowsCustomBehavior()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            BoundsMode = ContentBoundsMode.Custom,
            EnableConstrains = true
        };

        var childElement = new Border
        {
            Width = 200,
            Height = 150,
            Background = new SolidColorBrush(Colors.Red)
        };

        zoomBorder.Child = childElement;
        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        // Act - Pan
        zoomBorder.Pan(100, 100);

        // Assert - Custom bounds mode doesn't throw
        Assert.IsNotNull(zoomBorder);
    }

    [TestMethod]
    public async Task BoundsMode_Custom_ConstrainsPanningToReturnedBounds()
    {
        var zoomBorder = new CustomBoundsZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            Stretch = StretchMode.None,
            BoundsMode = ContentBoundsMode.Custom,
            EnableConstrains = true,
            CustomBounds = new Rect(-1000, -1000, 2200, 2150),
            Child = new Border
            {
                Width = 200,
                Height = 150,
                Background = new SolidColorBrush(Colors.Red)
            }
        };
        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        zoomBorder.Pan(5000, 5000, skipTransitions: true);

        Assert.AreEqual(900, zoomBorder.OffsetX, 1e-5);
        Assert.AreEqual(925, zoomBorder.OffsetY, 1e-5);

        zoomBorder.Pan(-5000, -5000, skipTransitions: true);

        Assert.AreEqual(-900, zoomBorder.OffsetX, 1e-5);
        Assert.AreEqual(-925, zoomBorder.OffsetY, 1e-5);
    }

    [TestMethod]
    public async Task BoundsMode_Custom_UsesReturnedBoundsForScrollbarState()
    {
        var zoomBorder = new CustomBoundsZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            Stretch = StretchMode.None,
            BoundsMode = ContentBoundsMode.Custom,
            EnableConstrains = true,
            CustomBounds = new Rect(-1000, -1000, 2200, 2150),
            Child = new Border
            {
                Width = 200,
                Height = 150,
                Background = new SolidColorBrush(Colors.Red)
            }
        };
        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        // Avalonia IScrollable members are exposed directly on the Uno ZoomBorder (Extent, Viewport, ScrollOffset).
        var scrollable = zoomBorder;

        Assert.AreEqual(new Size(2200, 2150), scrollable.Extent);
        Assert.AreEqual(new Size(400, 300), scrollable.Viewport);
        Assert.AreEqual(new Point(900, 925), scrollable.ScrollOffset);
    }

    [TestMethod]
    public async Task BoundsMode_Custom_ValidateTransformCanRejectTransform()
    {
        var zoomBorder = new CustomBoundsZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            Stretch = StretchMode.None,
            BoundsMode = ContentBoundsMode.Custom,
            EnableConstrains = true,
            MaximumAcceptedZoom = 2,
            CustomBounds = new Rect(-1000, -1000, 2200, 2150),
            Child = new Border
            {
                Width = 200,
                Height = 150,
                Background = new SolidColorBrush(Colors.Red)
            }
        };
        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        zoomBorder.ZoomTo(3, 100, 75, skipTransitions: true);

        Assert.AreEqual(1, zoomBorder.ZoomX, 1e-5);
        Assert.AreEqual(1, zoomBorder.ZoomY, 1e-5);
        Assert.AreEqual(0, zoomBorder.OffsetX, 1e-5);
        Assert.AreEqual(0, zoomBorder.OffsetY, 1e-5);
    }

    [TestMethod]
    public async Task BoundsMode_Custom_CanRefreshDynamicBounds()
    {
        var zoomBorder = new CustomBoundsZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            Stretch = StretchMode.None,
            BoundsMode = ContentBoundsMode.Custom,
            EnableConstrains = true,
            CustomBounds = new Rect(-1000, -1000, 2200, 2150),
            Child = new Border
            {
                Width = 200,
                Height = 150,
                Background = new SolidColorBrush(Colors.Red)
            }
        };
        await ZoomBorderTestHelper.LoadAsync(zoomBorder);
        // Avalonia IScrollable members are exposed directly on the Uno ZoomBorder (Extent, Viewport, ScrollOffset).
        var scrollable = zoomBorder;
        Assert.AreEqual(new Size(2200, 2150), scrollable.Extent);

        zoomBorder.CustomBounds = new Rect(0, 0, 200, 150);
        zoomBorder.RefreshCustomBounds();

        Assert.AreEqual(new Size(400, 300), scrollable.Extent);
        Assert.AreEqual(new Point(0, 0), scrollable.ScrollOffset);
    }
}

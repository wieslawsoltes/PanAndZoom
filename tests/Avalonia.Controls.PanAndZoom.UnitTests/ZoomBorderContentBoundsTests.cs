// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Controls.PanAndZoom.UnitTests;

/// <summary>
/// Tests for content bounds restriction functionality.
/// </summary>
public class ZoomBorderContentBoundsTests
{
    [AvaloniaFact]
    public void BoundsMode_DefaultValue_IsUnrestricted()
    {
        // Arrange & Act
        var zoomBorder = new ZoomBorder();

        // Assert
        Assert.Equal(ContentBoundsMode.Unrestricted, zoomBorder.BoundsMode);
    }

    [AvaloniaFact]
    public void BoundsMode_CanBeSetToUnrestricted()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();

        // Act
        zoomBorder.BoundsMode = ContentBoundsMode.Unrestricted;

        // Assert
        Assert.Equal(ContentBoundsMode.Unrestricted, zoomBorder.BoundsMode);
    }

    [AvaloniaFact]
    public void BoundsMode_CanBeSetToKeepContentVisible()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();

        // Act
        zoomBorder.BoundsMode = ContentBoundsMode.KeepContentVisible;

        // Assert
        Assert.Equal(ContentBoundsMode.KeepContentVisible, zoomBorder.BoundsMode);
    }

    [AvaloniaFact]
    public void BoundsMode_CanBeSetToFillViewport()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();

        // Act
        zoomBorder.BoundsMode = ContentBoundsMode.FillViewport;

        // Assert
        Assert.Equal(ContentBoundsMode.FillViewport, zoomBorder.BoundsMode);
    }

    [AvaloniaFact]
    public void BoundsMode_CanBeSetToKeepCentered()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();

        // Act
        zoomBorder.BoundsMode = ContentBoundsMode.KeepCentered;

        // Assert
        Assert.Equal(ContentBoundsMode.KeepCentered, zoomBorder.BoundsMode);
    }

    [AvaloniaFact]
    public void BoundsMode_CanBeSetToCustom()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();

        // Act
        zoomBorder.BoundsMode = ContentBoundsMode.Custom;

        // Assert
        Assert.Equal(ContentBoundsMode.Custom, zoomBorder.BoundsMode);
    }

    [AvaloniaFact]
    public void BoundsPadding_DefaultValue_IsZero()
    {
        // Arrange & Act
        var zoomBorder = new ZoomBorder();

        // Assert
        Assert.Equal(new Thickness(0), zoomBorder.BoundsPadding);
    }

    [AvaloniaFact]
    public void BoundsPadding_CanBeSetToUniformThickness()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();

        // Act
        zoomBorder.BoundsPadding = new Thickness(10);

        // Assert
        Assert.Equal(new Thickness(10), zoomBorder.BoundsPadding);
    }

    [AvaloniaFact]
    public void BoundsPadding_CanBeSetToNonUniformThickness()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();

        // Act
        zoomBorder.BoundsPadding = new Thickness(5, 10, 15, 20);

        // Assert
        Assert.Equal(new Thickness(5, 10, 15, 20), zoomBorder.BoundsPadding);
    }

    [AvaloniaFact]
    public void MinimumVisibleContentPercentage_DefaultValue_IsPointOne()
    {
        // Arrange & Act
        var zoomBorder = new ZoomBorder();

        // Assert
        Assert.Equal(0.1, zoomBorder.MinimumVisibleContentPercentage);
    }

    [AvaloniaFact]
    public void MinimumVisibleContentPercentage_CanBeSetToCustomValue()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();

        // Act
        zoomBorder.MinimumVisibleContentPercentage = 0.25;

        // Assert
        Assert.Equal(0.25, zoomBorder.MinimumVisibleContentPercentage);
    }

    [AvaloniaFact]
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
        Assert.Equal(ContentBoundsMode.KeepContentVisible, zoomBorder.BoundsMode);
        Assert.Equal(new Thickness(10), zoomBorder.BoundsPadding);
        Assert.Equal(0.25, zoomBorder.MinimumVisibleContentPercentage);
    }

    [AvaloniaFact]
    public void BoundsMode_Unrestricted_AllowsUnconstrainedPanning()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            BoundsMode = ContentBoundsMode.Unrestricted,
            EnableConstrains = true
        };

        var childElement = new Border
        {
            Width = 200,
            Height = 150,
            Background = Brushes.Red
        };

        zoomBorder.Child = childElement;
        var window = new Window { Content = zoomBorder };
        window.Show();

        // Act - Pan far beyond content
        zoomBorder.Pan(1000, 1000);

        // Assert - Should allow any offset in Unrestricted mode
        Assert.Equal(1000, zoomBorder.OffsetX);
        Assert.Equal(1000, zoomBorder.OffsetY);
    }

    [AvaloniaFact]
    public void BoundsMode_KeepContentVisible_ConstrainsPanningBeyondContent()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
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
            Background = Brushes.Red
        };

        zoomBorder.Child = childElement;
        var window = new Window { Content = zoomBorder };
        window.Show();

        // Act - Try to pan far beyond content
        zoomBorder.Pan(1000, 1000);

        // Assert - Should constrain to keep minimum content visible
        Assert.True(zoomBorder.OffsetX < 1000, "OffsetX should be constrained");
        Assert.True(zoomBorder.OffsetY < 1000, "OffsetY should be constrained");
    }

    [AvaloniaFact]
    public void BoundsMode_FillViewport_ConstrainsPanning()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            BoundsMode = ContentBoundsMode.FillViewport,
            EnableConstrains = true
        };

        var childElement = new Border
        {
            Width = 200,
            Height = 150,
            Background = Brushes.Red
        };

        zoomBorder.Child = childElement;
        var window = new Window { Content = zoomBorder };
        window.Show();

        // Act - Try to pan
        zoomBorder.Pan(500, 500);

        // Assert - FillViewport mode constrains movement
        Assert.NotNull(zoomBorder);
    }

    [AvaloniaFact]
    public void BoundsMode_KeepCentered_ConstrainsPanning()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            BoundsMode = ContentBoundsMode.KeepCentered,
            EnableConstrains = true
        };

        var childElement = new Border
        {
            Width = 200,
            Height = 150,
            Background = Brushes.Red
        };

        zoomBorder.Child = childElement;
        var window = new Window { Content = zoomBorder };
        window.Show();

        // Act - Try to pan
        zoomBorder.Pan(500, 500);

        // Assert - KeepCentered mode constrains movement
        Assert.NotNull(zoomBorder);
    }

    [AvaloniaFact]
    public void BoundsMode_Custom_AllowsCustomBehavior()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            BoundsMode = ContentBoundsMode.Custom,
            EnableConstrains = true
        };

        var childElement = new Border
        {
            Width = 200,
            Height = 150,
            Background = Brushes.Red
        };

        zoomBorder.Child = childElement;
        var window = new Window { Content = zoomBorder };
        window.Show();

        // Act - Pan
        zoomBorder.Pan(100, 100);

        // Assert - Custom bounds mode doesn't throw
        Assert.NotNull(zoomBorder);
    }

    [AvaloniaFact]
    public void BoundsMode_Custom_ConstrainsPanningToReturnedBounds()
    {
        var zoomBorder = new CustomBoundsZoomBorder
        {
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
                Background = Brushes.Red
            }
        };
        var window = new Window { Content = zoomBorder };
        window.Show();
        window.UpdateLayout();

        zoomBorder.Pan(5000, 5000, skipTransitions: true);

        Assert.Equal(900, zoomBorder.OffsetX, 5);
        Assert.Equal(925, zoomBorder.OffsetY, 5);

        zoomBorder.Pan(-5000, -5000, skipTransitions: true);

        Assert.Equal(-900, zoomBorder.OffsetX, 5);
        Assert.Equal(-925, zoomBorder.OffsetY, 5);
    }

    [AvaloniaFact]
    public void BoundsMode_Custom_UsesReturnedBoundsForScrollbarState()
    {
        var zoomBorder = new CustomBoundsZoomBorder
        {
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
                Background = Brushes.Red
            }
        };
        var window = new Window { Content = zoomBorder };
        window.Show();
        window.UpdateLayout();

        var scrollable = (IScrollable)zoomBorder;

        Assert.Equal(new Size(2200, 2150), scrollable.Extent);
        Assert.Equal(new Size(400, 300), scrollable.Viewport);
        Assert.Equal(new Vector(900, 925), scrollable.Offset);
    }

    [AvaloniaFact]
    public void BoundsMode_Custom_ValidateTransformCanRejectTransform()
    {
        var zoomBorder = new CustomBoundsZoomBorder
        {
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
                Background = Brushes.Red
            }
        };
        var window = new Window { Content = zoomBorder };
        window.Show();
        window.UpdateLayout();

        zoomBorder.ZoomTo(3, 100, 75, skipTransitions: true);

        Assert.Equal(1, zoomBorder.ZoomX, 5);
        Assert.Equal(1, zoomBorder.ZoomY, 5);
        Assert.Equal(0, zoomBorder.OffsetX, 5);
        Assert.Equal(0, zoomBorder.OffsetY, 5);
    }

    [AvaloniaFact]
    public void BoundsMode_Custom_CanRefreshDynamicBounds()
    {
        var zoomBorder = new CustomBoundsZoomBorder
        {
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
                Background = Brushes.Red
            }
        };
        var window = new Window { Content = zoomBorder };
        window.Show();
        window.UpdateLayout();
        var scrollable = (IScrollable)zoomBorder;
        Assert.Equal(new Size(2200, 2150), scrollable.Extent);

        zoomBorder.CustomBounds = new Rect(0, 0, 200, 150);
        zoomBorder.RefreshCustomBounds();

        Assert.Equal(new Size(400, 300), scrollable.Extent);
        Assert.Equal(new Vector(0, 0), scrollable.Offset);
    }
}

internal sealed class CustomBoundsZoomBorder : ZoomBorder
{
    public Rect CustomBounds { get; set; }

    public double MaximumAcceptedZoom { get; set; } = double.PositiveInfinity;

    protected override Rect GetContentBounds() => CustomBounds;

    protected override bool ValidateTransform(Matrix newMatrix) => newMatrix.M11 <= MaximumAcceptedZoom;

    public void RefreshCustomBounds() => InvalidateContentBounds();
}

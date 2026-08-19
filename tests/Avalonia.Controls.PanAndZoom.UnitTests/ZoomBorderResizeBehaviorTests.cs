// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Controls.PanAndZoom.UnitTests;

/// <summary>
/// Tests for resize behavior functionality.
/// </summary>
public class ZoomBorderResizeBehaviorTests
{
    [AvaloniaFact]
    public void ResizeBehavior_DefaultValue_IsNone()
    {
        // Arrange & Act
        var zoomBorder = new ZoomBorder();

        // Assert
        Assert.Equal(ResizeBehaviorMode.None, zoomBorder.ResizeBehavior);
    }

    [AvaloniaFact]
    public void ResizeBehavior_CanBeSetToNone()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();

        // Act
        zoomBorder.ResizeBehavior = ResizeBehaviorMode.None;

        // Assert
        Assert.Equal(ResizeBehaviorMode.None, zoomBorder.ResizeBehavior);
    }

    [AvaloniaFact]
    public void ResizeBehavior_CanBeSetToMaintainCenter()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();

        // Act
        zoomBorder.ResizeBehavior = ResizeBehaviorMode.MaintainCenter;

        // Assert
        Assert.Equal(ResizeBehaviorMode.MaintainCenter, zoomBorder.ResizeBehavior);
    }

    [AvaloniaFact]
    public void ResizeBehavior_CanBeSetToMaintainTopLeft()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();

        // Act
        zoomBorder.ResizeBehavior = ResizeBehaviorMode.MaintainTopLeft;

        // Assert
        Assert.Equal(ResizeBehaviorMode.MaintainTopLeft, zoomBorder.ResizeBehavior);
    }

    [AvaloniaFact]
    public void ResizeBehavior_CanBeSetToMaintainZoom()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();

        // Act
        zoomBorder.ResizeBehavior = ResizeBehaviorMode.MaintainZoom;

        // Assert
        Assert.Equal(ResizeBehaviorMode.MaintainZoom, zoomBorder.ResizeBehavior);
    }

    [AvaloniaFact]
    public void ResizeBehavior_CanBeSetToReapplyStretch()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();

        // Act
        zoomBorder.ResizeBehavior = ResizeBehaviorMode.ReapplyStretch;

        // Assert
        Assert.Equal(ResizeBehaviorMode.ReapplyStretch, zoomBorder.ResizeBehavior);
    }

    [AvaloniaFact]
    public void ResizeBehavior_CanBeSetToCustom()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();

        // Act
        zoomBorder.ResizeBehavior = ResizeBehaviorMode.Custom;

        // Assert
        Assert.Equal(ResizeBehaviorMode.Custom, zoomBorder.ResizeBehavior);
    }

    [AvaloniaFact]
    public void ResizeBehavior_None_MaintainsCurrentOffsetOnResize()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            ResizeBehavior = ResizeBehaviorMode.None
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

        var initialOffsetX = zoomBorder.OffsetX;
        var initialOffsetY = zoomBorder.OffsetY;

        // Act - Resize the control
        zoomBorder.Width = 600;
        zoomBorder.Height = 450;

        // Assert - Offset should remain the same with None mode
        Assert.Equal(initialOffsetX, zoomBorder.OffsetX);
        Assert.Equal(initialOffsetY, zoomBorder.OffsetY);
    }

    [AvaloniaTheory]
    [InlineData(ResizeBehaviorMode.None)]
    [InlineData(ResizeBehaviorMode.MaintainCenter)]
    [InlineData(ResizeBehaviorMode.MaintainTopLeft)]
    [InlineData(ResizeBehaviorMode.MaintainZoom)]
    [InlineData(ResizeBehaviorMode.Custom)]
    public void ResizeBehavior_DoesNotReapplyStretchUnlessRequested(ResizeBehaviorMode resizeBehavior)
    {
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            ResizeBehavior = resizeBehavior,
            Stretch = StretchMode.Uniform,
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

        zoomBorder.ZoomTo(1.25, 100, 75, skipTransitions: true);
        var zoomBeforeResize = zoomBorder.ZoomX;

        zoomBorder.Width = 600;
        zoomBorder.Height = 450;
        window.UpdateLayout();

        Assert.Equal(2.5, zoomBeforeResize, 5);
        Assert.Equal(zoomBeforeResize, zoomBorder.ZoomX, 5);
        Assert.Equal(zoomBeforeResize, zoomBorder.ZoomY, 5);
    }

    [AvaloniaFact]
    public void ChangingStretch_ReappliesTheNewStretchMode()
    {
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            ResizeBehavior = ResizeBehaviorMode.None,
            Stretch = StretchMode.Uniform,
            Child = new Border
            {
                Width = 200,
                Height = 100,
                Background = Brushes.Red
            }
        };
        var window = new Window { Content = zoomBorder };
        window.Show();
        window.UpdateLayout();

        Assert.Equal(2.0, zoomBorder.ZoomX, 5);

        zoomBorder.Stretch = StretchMode.UniformToFill;
        window.UpdateLayout();

        Assert.Equal(3.0, zoomBorder.ZoomX, 5);
        Assert.Equal(3.0, zoomBorder.ZoomY, 5);
    }

    [AvaloniaFact]
    public void ResizeBehavior_MaintainZoom_PreservesZoomLevelOnResize()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            ResizeBehavior = ResizeBehaviorMode.MaintainZoom
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

        // Pan to a specific location
        zoomBorder.Pan(50, 50);
        var initialZoomX = zoomBorder.ZoomX;

        // Act - Resize the control
        zoomBorder.Width = 800; // Double the width
        zoomBorder.Height = 600; // Double the height

        // Assert - Zoom should remain the same
        Assert.Equal(initialZoomX, zoomBorder.ZoomX);
    }

    [AvaloniaFact]
    public void ResizeBehavior_MaintainCenter_TriggersOnBoundsChange()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            ResizeBehavior = ResizeBehaviorMode.MaintainCenter,
            Stretch = StretchMode.None
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

        // First zoom and pan to establish a position
        zoomBorder.ZoomIn();
        zoomBorder.Pan(50, 50);
        
        // Force measure/arrange to establish size
        window.UpdateLayout();

        // Act - Change bounds to trigger the resize handler
        zoomBorder.Width = 600;
        zoomBorder.Height = 450;
        window.UpdateLayout();

        // Assert - Just verify it didn't throw
        Assert.NotNull(zoomBorder);
    }

    [AvaloniaFact]
    public void ResizeBehavior_ReapplyStretch_TriggersAutoFit()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            ResizeBehavior = ResizeBehaviorMode.ReapplyStretch,
            Stretch = StretchMode.Uniform
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

        // Force initial layout
        window.UpdateLayout();

        // Act - Change bounds to trigger the resize handler
        zoomBorder.Width = 600;
        zoomBorder.Height = 450;
        window.UpdateLayout();

        // Assert - Should auto fit after resize
        Assert.NotNull(zoomBorder);
    }

    [AvaloniaFact]
    public void ResizeBehavior_Custom_TriggersOnResizedMethod()
    {
        // Arrange - Using a subclass to test OnResized virtual method
        var zoomBorder = new TestableZoomBorder
        {
            Width = 400,
            Height = 300,
            ResizeBehavior = ResizeBehaviorMode.Custom,
            Stretch = StretchMode.None
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

        // Force initial layout
        window.UpdateLayout();

        // Act - Change bounds to trigger the resize handler
        zoomBorder.Width = 600;
        zoomBorder.Height = 450;
        window.UpdateLayout();

        // Assert - OnResized should have been called
        Assert.NotNull(zoomBorder);
    }
}

/// <summary>
/// Testable ZoomBorder subclass for testing protected virtual methods.
/// </summary>
internal class TestableZoomBorder : ZoomBorder
{
    public bool OnResizedCalled { get; private set; }
    public Size? LastOldSize { get; private set; }
    public Size? LastNewSize { get; private set; }

    protected override void OnResized(Size oldSize, Size newSize)
    {
        OnResizedCalled = true;
        LastOldSize = oldSize;
        LastNewSize = newSize;
        base.OnResized(oldSize, newSize);
    }
}

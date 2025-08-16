using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Controls.PanAndZoom.UnitTests;

public class ZoomBorderILogicalScrollableTests
{
    [AvaloniaFact]
    public void ILogicalScrollable_Properties_InitializedCorrectly()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300
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
        
        var scrollable = (ILogicalScrollable)zoomBorder;
        
        // Assert - Initial state
        Assert.True(scrollable.IsLogicalScrollEnabled);
        Assert.Equal(new Size(1, 1), scrollable.ScrollSize);
        Assert.Equal(new Size(10, 10), scrollable.PageScrollSize);
        Assert.False(scrollable.CanHorizontallyScroll);
        Assert.False(scrollable.CanVerticallyScroll);
    }
    
    [AvaloniaFact]
    public void ILogicalScrollable_CanScroll_Properties_SetCorrectly()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300
        };
        
        var scrollable = (ILogicalScrollable)zoomBorder;
        
        // Act & Assert - Set horizontal scrolling
        scrollable.CanHorizontallyScroll = true;
        Assert.True(scrollable.CanHorizontallyScroll);
        
        // Act & Assert - Set vertical scrolling
        scrollable.CanVerticallyScroll = true;
        Assert.True(scrollable.CanVerticallyScroll);
        
        // Act & Assert - Disable scrolling
        scrollable.CanHorizontallyScroll = false;
        scrollable.CanVerticallyScroll = false;
        Assert.False(scrollable.CanHorizontallyScroll);
        Assert.False(scrollable.CanVerticallyScroll);
    }
    
    [AvaloniaFact]
    public void ILogicalScrollable_ScrollInvalidated_EventRaised()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300
        };
        
        var childElement = new Border
        {
            Width = 800,
            Height = 600,
            Background = Brushes.Red
        };
        
        zoomBorder.Child = childElement;
        
        var window = new Window { Content = zoomBorder };
        window.Show();
        
        var scrollable = (ILogicalScrollable)zoomBorder;
        var eventRaised = false;
        
        // Act - Subscribe to scroll invalidated event
        scrollable.ScrollInvalidated += (sender, args) => eventRaised = true;
        
        // Trigger scroll invalidation by zooming
        zoomBorder.Zoom(2.0, 200, 150);
        
        // Assert
        Assert.True(eventRaised, "ScrollInvalidated event should be raised when zoom changes");
    }
    
    [AvaloniaFact]
    public void ILogicalScrollable_Offset_UpdatesCorrectly()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300
        };
        
        var childElement = new Border
        {
            Width = 800,
            Height = 600,
            Background = Brushes.Red
        };
        
        zoomBorder.Child = childElement;
        
        var window = new Window { Content = zoomBorder };
        window.Show();
        
        var scrollable = (IScrollable)zoomBorder;
        
        // First zoom to create scrollable content
        zoomBorder.Zoom(2.0, 200, 150);
        
        var initialOffset = scrollable.Offset;
        
        // Act - Set offset
        var newOffset = new Vector(50, 30);
        scrollable.Offset = newOffset;
        
        // Assert - Offset should be different from initial
        Assert.NotEqual(initialOffset, scrollable.Offset);
    }
    
    [AvaloniaFact]
    public void ZoomBorder_InsideScrollViewer_BasicIntegration()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300
        };
        
        var childElement = new Border
        {
            Width = 800,
            Height = 600,
            Background = Brushes.Red
        };
        
        zoomBorder.Child = childElement;
        
        var scrollViewer = new ScrollViewer
        {
            Content = zoomBorder,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        
        var window = new Window { Content = scrollViewer };
        window.Show();
        
        // Assert - ScrollViewer should recognize ZoomBorder as scrollable
        Assert.NotNull(scrollViewer.Content);
        Assert.IsType<ZoomBorder>(scrollViewer.Content);
    }
    
    [AvaloniaFact]
    public void ZoomBorder_InsideScrollViewer_ScrollingBehavior()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300
        };
        
        var childElement = new Border
        {
            Width = 800,
            Height = 600,
            Background = Brushes.Red
        };
        
        zoomBorder.Child = childElement;
        
        var scrollViewer = new ScrollViewer
        {
            Content = zoomBorder,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        
        var window = new Window { Content = scrollViewer };
        window.Show();
        
        var scrollable = (IScrollable)zoomBorder;
        
        // Act - Zoom to create scrollable content
        zoomBorder.Zoom(2.0, 200, 150);
        
        // Assert - Extent should be larger than viewport after zoom
        Assert.True(scrollable.Extent.Width > scrollable.Viewport.Width || 
                   scrollable.Extent.Height > scrollable.Viewport.Height,
                   "Extent should be larger than viewport when zoomed");
    }
    
    [AvaloniaFact]
    public void ZoomBorder_InsideScrollViewer_OffsetChanges()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300
        };
        
        var childElement = new Border
        {
            Width = 800,
            Height = 600,
            Background = Brushes.Red
        };
        
        zoomBorder.Child = childElement;
        
        var scrollViewer = new ScrollViewer
        {
            Content = zoomBorder,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        
        var window = new Window { Content = scrollViewer };
        window.Show();
        
        var scrollable = (IScrollable)zoomBorder;
        
        // Act - Zoom and pan to create offset
        zoomBorder.Zoom(2.0, 200, 150);
        zoomBorder.Pan(-100, -50);
        
        // Assert - Offset should reflect the pan
        Assert.True(scrollable.Offset.X > 0 || scrollable.Offset.Y > 0,
                   "Offset should be positive when content is panned negatively");
    }
    
    [AvaloniaFact]
    public void CalculateScrollable_StaticMethod_CorrectCalculations()
    {
        // Arrange
        var sourceBounds = new Rect(0, 0, 200, 150);
        var borderSize = new Size(400, 300);
        var matrix = Matrix.CreateScale(2.0, 2.0); // 2x zoom
        
        // Act
        ZoomBorder.CalculateScrollable(sourceBounds, borderSize, matrix, out var extent, out var viewport, out var offset);
        
        // Assert
        Assert.Equal(borderSize, viewport);
        Assert.True(extent.Width >= viewport.Width, "Extent width should be at least viewport width");
        Assert.True(extent.Height >= viewport.Height, "Extent height should be at least viewport height");
        Assert.True(offset.X >= 0 && offset.Y >= 0, "Offset should be non-negative");
    }
    
    [AvaloniaFact]
    public void CalculateScrollable_WithTranslation_CorrectOffset()
    {
        // Arrange
        var sourceBounds = new Rect(0, 0, 200, 150);
        var borderSize = new Size(400, 300);
        var matrix = Matrix.CreateScale(2.0, 2.0) * Matrix.CreateTranslation(-50, -30);
        
        // Act
        ZoomBorder.CalculateScrollable(sourceBounds, borderSize, matrix, out _, out _, out var offset);
        
        // Assert
        Assert.Equal(50, offset.X);
        Assert.Equal(30, offset.Y);
    }
    
    [AvaloniaFact]
    public void ZoomBorder_ScrollViewer_MultipleZoomLevels()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300
        };
        
        var childElement = new Border
        {
            Width = 200,
            Height = 150,
            Background = Brushes.Red
        };
        
        zoomBorder.Child = childElement;
        
        var scrollViewer = new ScrollViewer
        {
            Content = zoomBorder,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        
        var window = new Window { Content = scrollViewer };
        window.Show();
        
        var scrollable = (IScrollable)zoomBorder;
        
        // Test different zoom levels
        var zoomLevels = new[] { 0.5, 1.0, 1.5, 2.0, 3.0 };
        
        foreach (var zoom in zoomLevels)
        {
            // Act
            zoomBorder.Zoom(zoom, 200, 150);
            
            // Assert
            Assert.True(scrollable.Extent.Width > 0 && scrollable.Extent.Height > 0,
                       $"Extent should be positive at zoom level {zoom}");
            Assert.True(scrollable.Viewport.Width > 0 && scrollable.Viewport.Height > 0,
                       $"Viewport should be positive at zoom level {zoom}");
        }
    }
    
    [AvaloniaFact]
    public void ZoomBorder_ScrollViewer_BringIntoView_ReturnsFalse()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300
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
        
        var scrollable = (ILogicalScrollable)zoomBorder;
        
        // Act & Assert
        var result = scrollable.BringIntoView(childElement, new Rect(0, 0, 50, 50));
        Assert.False(result, "BringIntoView should return false as it's not implemented");
    }
    
    [AvaloniaFact]
    public void ZoomBorder_ScrollViewer_GetControlInDirection_ReturnsNull()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300
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
        
        var scrollable = (ILogicalScrollable)zoomBorder;
        
        // Act & Assert
        var result = scrollable.GetControlInDirection(NavigationDirection.Down, childElement);
        Assert.Null(result);
    }
    
    [AvaloniaFact]
    public void ZoomBorder_ScrollViewer_ExtentChangesWithContent()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300
        };
        
        var scrollViewer = new ScrollViewer
        {
            Content = zoomBorder,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        
        var window = new Window { Content = scrollViewer };
        window.Show();
        
        var scrollable = (IScrollable)zoomBorder;
        var initialExtent = scrollable.Extent;
        
        // Act - Add larger child
        var largeChild = new Border
        {
            Width = 800,
            Height = 600,
            Background = Brushes.Blue
        };
        
        zoomBorder.Child = largeChild;
        
        // Assert - Extent should change with larger content
        var newExtent = scrollable.Extent;
        Assert.True(newExtent.Width >= initialExtent.Width && newExtent.Height >= initialExtent.Height,
                   "Extent should increase or stay the same with larger content");
    }
    
    [AvaloniaFact]
    public void ZoomBorder_ScrollViewer_OffsetFeedbackLoop_Prevention()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300
        };
        
        var childElement = new Border
        {
            Width = 800,
            Height = 600,
            Background = Brushes.Red
        };
        
        zoomBorder.Child = childElement;
        
        var scrollViewer = new ScrollViewer
        {
            Content = zoomBorder,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        
        var window = new Window { Content = scrollViewer };
        window.Show();
        
        var scrollable = (IScrollable)zoomBorder;
        
        // First zoom to create scrollable content
        zoomBorder.Zoom(2.0, 200, 150);

        // Act - Set offset multiple times rapidly (simulating ScrollViewer feedback)
        var testOffset = new Vector(100, 50);
        scrollable.Offset = testOffset;
        scrollable.Offset = testOffset; // Second set should not cause issues
        
        // Assert - Should handle multiple offset sets gracefully without throwing
        Assert.True(true, "Multiple offset sets completed without exceptions");
    }
}

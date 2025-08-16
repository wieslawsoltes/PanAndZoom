using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Controls.PanAndZoom.UnitTests;

public class ZoomBorderScrollGestureTests
{
    [AvaloniaFact]
    public void ScrollGesture_PanRight_ChangesOffsetX()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableGestureTranslation = true,
            EnableGestures = true
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
        
        // Act - Simulate scroll gesture to the right
        var scrollEventArgs = new ScrollGestureEventArgs(1, new Vector(50, 0))
        {
            RoutedEvent = Gestures.ScrollGestureEvent,
            Source = zoomBorder
        };
        
        zoomBorder.RaiseEvent(scrollEventArgs);
        
        // Assert
        Assert.NotEqual(initialOffsetX, zoomBorder.OffsetX);
        Assert.True(scrollEventArgs.Handled, "Scroll gesture event should be handled");
    }
    
    [AvaloniaFact]
    public void ScrollGesture_PanLeft_ChangesOffsetX()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableGestureTranslation = true,
            EnableGestures = true
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
        
        // Act - Simulate scroll gesture to the left
        var scrollEventArgs = new ScrollGestureEventArgs(1, new Vector(-30, 0))
        {
            RoutedEvent = Gestures.ScrollGestureEvent,
            Source = zoomBorder
        };
        
        zoomBorder.RaiseEvent(scrollEventArgs);
        
        // Assert
        Assert.NotEqual(initialOffsetX, zoomBorder.OffsetX);
        Assert.True(scrollEventArgs.Handled, "Scroll gesture event should be handled");
    }
    
    [AvaloniaFact]
    public void ScrollGesture_PanUp_ChangesOffsetY()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableGestureTranslation = true,
            EnableGestures = true
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
        
        var initialOffsetY = zoomBorder.OffsetY;
        
        // Act - Simulate scroll gesture upward
        var scrollEventArgs = new ScrollGestureEventArgs(1, new Vector(0, -25))
        {
            RoutedEvent = Gestures.ScrollGestureEvent,
            Source = zoomBorder
        };
        
        zoomBorder.RaiseEvent(scrollEventArgs);
        
        // Assert
        Assert.NotEqual(initialOffsetY, zoomBorder.OffsetY);
        Assert.True(scrollEventArgs.Handled, "Scroll gesture event should be handled");
    }
    
    [AvaloniaFact]
    public void ScrollGesture_PanDown_ChangesOffsetY()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableGestureTranslation = true,
            EnableGestures = true
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
        
        var initialOffsetY = zoomBorder.OffsetY;
        
        // Act - Simulate scroll gesture downward
        var scrollEventArgs = new ScrollGestureEventArgs(1, new Vector(0, 40))
        {
            RoutedEvent = Gestures.ScrollGestureEvent,
            Source = zoomBorder
        };
        
        zoomBorder.RaiseEvent(scrollEventArgs);
        
        // Assert
        Assert.NotEqual(initialOffsetY, zoomBorder.OffsetY);
        Assert.True(scrollEventArgs.Handled, "Scroll gesture event should be handled");
    }
    
    [AvaloniaFact]
    public void ScrollGesture_DiagonalPan_ChangesBothOffsets()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableGestureTranslation = true,
            EnableGestures = true
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
        
        // Act - Simulate diagonal scroll gesture
        var scrollEventArgs = new ScrollGestureEventArgs(1, new Vector(30, 40))
        {
            RoutedEvent = Gestures.ScrollGestureEvent,
            Source = zoomBorder
        };
        
        zoomBorder.RaiseEvent(scrollEventArgs);
        
        // Assert
        Assert.NotEqual(initialOffsetX, zoomBorder.OffsetX);
        Assert.NotEqual(initialOffsetY, zoomBorder.OffsetY);
        Assert.True(scrollEventArgs.Handled, "Scroll gesture event should be handled");
    }
    
    [AvaloniaFact]
    public void ScrollGesture_GesturePanDisabled_DoesNotPan()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableGestureTranslation = false,
            EnableGestures = true
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
        
        // Act - Simulate scroll gesture when gesture pan is disabled
        var scrollEventArgs = new ScrollGestureEventArgs(1, new Vector(50, 50))
        {
            RoutedEvent = Gestures.ScrollGestureEvent,
            Source = zoomBorder
        };
        
        zoomBorder.RaiseEvent(scrollEventArgs);
        
        // Assert
        Assert.Equal(initialOffsetX, zoomBorder.OffsetX);
        Assert.Equal(initialOffsetY, zoomBorder.OffsetY);
        Assert.False(scrollEventArgs.Handled, "Scroll gesture event should not be handled when gesture translation is disabled");
    }
    
    [AvaloniaFact]
    public void ScrollGesture_NoChildElement_DoesNotCrash()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableGestureTranslation = true,
            EnableGestures = true
        };
        
        // No child element set
        
        var window = new Window { Content = zoomBorder };
        window.Show();
        
        // Act - Simulate scroll gesture without child element
        var scrollEventArgs = new ScrollGestureEventArgs(1, new Vector(50, 50))
        {
            RoutedEvent = Gestures.ScrollGestureEvent,
            Source = zoomBorder
        };
        
        // Should not throw exception
        zoomBorder.RaiseEvent(scrollEventArgs);
        
        // Assert
        Assert.False(scrollEventArgs.Handled, "Scroll gesture event should not be handled when no child element");
    }
    
    [AvaloniaFact]
    public void ScrollGestureEnded_HandlesEvent()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableGestureTranslation = true,
            EnableGestures = true
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
        
        // Act - Simulate scroll gesture ended
        var scrollEndedEventArgs = new ScrollGestureEndedEventArgs(1)
        {
            RoutedEvent = Gestures.ScrollGestureEndedEvent,
            Source = zoomBorder
        };
        
        zoomBorder.RaiseEvent(scrollEndedEventArgs);
        
        // Assert
        Assert.True(scrollEndedEventArgs.Handled, "Scroll gesture ended event should be handled");
    }
    
    [AvaloniaFact]
    public void ScrollGesture_MultipleDeltas_AccumulatesPan()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableGestureTranslation = true,
            EnableGestures = true
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
        
        // Act - Simulate multiple scroll gestures
        var scrollEventArgs1 = new ScrollGestureEventArgs(1, new Vector(20, 0))
        {
            RoutedEvent = Gestures.ScrollGestureEvent,
            Source = zoomBorder
        };
        
        zoomBorder.RaiseEvent(scrollEventArgs1);
        var intermediateOffsetX = zoomBorder.OffsetX;
        
        var scrollEventArgs2 = new ScrollGestureEventArgs(1, new Vector(30, 0))
        {
            RoutedEvent = Gestures.ScrollGestureEvent,
            Source = zoomBorder
        };
        
        zoomBorder.RaiseEvent(scrollEventArgs2);
        
        // Assert
        Assert.NotEqual(initialOffsetX, intermediateOffsetX);
        Assert.NotEqual(intermediateOffsetX, zoomBorder.OffsetX);
        // The exact offset calculation depends on the implementation,
        // but we can verify that multiple gestures accumulate
    }
    
    [AvaloniaFact]
    public void ScrollGesture_ZeroDelta_NoOffsetChange()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableGestureTranslation = true,
            EnableGestures = true
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
        
        // Act - Simulate scroll gesture with zero delta
        var scrollEventArgs = new ScrollGestureEventArgs(1, new Vector(0, 0))
        {
            RoutedEvent = Gestures.ScrollGestureEvent,
            Source = zoomBorder
        };
        
        zoomBorder.RaiseEvent(scrollEventArgs);
        
        // Assert
        Assert.Equal(initialOffsetX, zoomBorder.OffsetX);
        Assert.Equal(initialOffsetY, zoomBorder.OffsetY);
        Assert.True(scrollEventArgs.Handled, "Scroll gesture event should still be handled");
    }
}
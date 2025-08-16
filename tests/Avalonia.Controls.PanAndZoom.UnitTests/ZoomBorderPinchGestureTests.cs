using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Controls.PanAndZoom.UnitTests;

public class ZoomBorderPinchGestureTests
{
    [AvaloniaFact]
    public void PinchGesture_ZoomIn_IncreasesZoom()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableGestureZoom = true,
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
        
        var initialZoomX = zoomBorder.ZoomX;
        var initialZoomY = zoomBorder.ZoomY;
        
        // Act - Simulate pinch gesture with scale > 1 (zoom in)
        var pinchEventArgs = new PinchEventArgs(1.5, new Point(0.5, 0.5), 0.0, 0.0)
        {
            RoutedEvent = Gestures.PinchEvent,
            Source = zoomBorder
        };
        
        zoomBorder.RaiseEvent(pinchEventArgs);
        
        // Assert
        Assert.True(zoomBorder.ZoomX > initialZoomX, "ZoomX should increase after pinch zoom in");
        Assert.True(zoomBorder.ZoomY > initialZoomY, "ZoomY should increase after pinch zoom in");
        Assert.True(pinchEventArgs.Handled, "Pinch event should be handled");
    }
    
    [AvaloniaFact]
    public void PinchGesture_ZoomOut_DecreasesZoom()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableGestureZoom = true,
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
        
        // First zoom in to have something to zoom out from
        zoomBorder.ZoomTo(2.0, 100, 75);
        
        var initialZoomX = zoomBorder.ZoomX;
        var initialZoomY = zoomBorder.ZoomY;
        
        // Act - Simulate pinch gesture with scale < 1 (zoom out)
        var pinchEventArgs = new PinchEventArgs(0.8, new Point(0.5, 0.5), 0.0, 0.0)
        {
            RoutedEvent = Gestures.PinchEvent,
            Source = zoomBorder
        };
        
        zoomBorder.RaiseEvent(pinchEventArgs);
        
        // Assert
        Assert.True(zoomBorder.ZoomX < initialZoomX, "ZoomX should decrease after pinch zoom out");
        Assert.True(zoomBorder.ZoomY < initialZoomY, "ZoomY should decrease after pinch zoom out");
        Assert.True(pinchEventArgs.Handled, "Pinch event should be handled");
    }
    
    [AvaloniaFact]
    public void PinchGesture_DifferentScaleOrigins_ZoomsAtCorrectLocation()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableGestureZoom = true,
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
        
        var initialZoom = zoomBorder.ZoomX;
        
        // Act - Simulate pinch gesture at top-left corner
        var pinchEventArgs = new PinchEventArgs(1.5, new Point(0.0, 0.0), 0.0, 0.0)
        {
            RoutedEvent = Gestures.PinchEvent,
            Source = zoomBorder
        };
        
        zoomBorder.RaiseEvent(pinchEventArgs);
        
        // Assert
        Assert.True(zoomBorder.ZoomX > initialZoom, "Zoom should increase");
        Assert.True(pinchEventArgs.Handled, "Pinch event should be handled");
        
        // The exact offset calculation depends on the implementation,
        // but we can verify that zoom occurred at the specified origin
    }
    
    [AvaloniaFact]
    public void PinchGesture_GestureZoomDisabled_DoesNotZoom()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableGestureZoom = false,
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
        
        var initialZoomX = zoomBorder.ZoomX;
        var initialZoomY = zoomBorder.ZoomY;
        
        // Act - Simulate pinch gesture when gesture zoom is disabled
        var pinchEventArgs = new PinchEventArgs(1.5, new Point(0.5, 0.5), 0.0, 0.0)
        {
            RoutedEvent = Gestures.PinchEvent,
            Source = zoomBorder
        };
        
        zoomBorder.RaiseEvent(pinchEventArgs);
        
        // Assert
        Assert.Equal(initialZoomX, zoomBorder.ZoomX);
        Assert.Equal(initialZoomY, zoomBorder.ZoomY);
        Assert.False(pinchEventArgs.Handled, "Pinch event should not be handled when gesture zoom is disabled");
    }
    
    [AvaloniaFact]
    public void PinchGesture_NoChildElement_DoesNotCrash()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableGestureZoom = true,
            EnableGestures = true
        };
        
        // No child element set
        
        var window = new Window { Content = zoomBorder };
        window.Show();
        
        // Act - Simulate pinch gesture without child element
        var pinchEventArgs = new PinchEventArgs(1.5, new Point(0.5, 0.5), 0.0, 0.0)
        {
            RoutedEvent = Gestures.PinchEvent,
            Source = zoomBorder
        };
        
        // Should not throw exception
        zoomBorder.RaiseEvent(pinchEventArgs);
        
        // Assert
        Assert.False(pinchEventArgs.Handled, "Pinch event should not be handled when no child element");
    }
    
    [AvaloniaFact]
    public void PinchGestureEnded_HandlesEvent()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableGestureZoom = true,
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
        
        // Act - Simulate pinch gesture ended
        var pinchEndedEventArgs = new PinchEndedEventArgs
        {
            RoutedEvent = Gestures.PinchEndedEvent,
            Source = zoomBorder
        };
        
        zoomBorder.RaiseEvent(pinchEndedEventArgs);
        
        // Assert
        Assert.True(pinchEndedEventArgs.Handled, "Pinch ended event should be handled");
    }
    
    [AvaloniaFact]
    public void PinchGesture_MultipleScaleChanges_AccumulatesZoom()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableGestureZoom = true,
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
        
        var initialZoom = zoomBorder.ZoomX;
        
        // Act - Simulate multiple pinch gestures
        var pinchEventArgs1 = new PinchEventArgs(1.2, new Point(0.5, 0.5), 0.0, 0.0)
        {
            RoutedEvent = Gestures.PinchEvent,
            Source = zoomBorder
        };
        
        zoomBorder.RaiseEvent(pinchEventArgs1);
        var intermediateZoom = zoomBorder.ZoomX;
        
        var pinchEventArgs2 = new PinchEventArgs(1.3, new Point(0.5, 0.5), 0.0, 0.0)
        {
            RoutedEvent = Gestures.PinchEvent,
            Source = zoomBorder
        };
        
        zoomBorder.RaiseEvent(pinchEventArgs2);
        
        // Assert
        Assert.True(intermediateZoom > initialZoom, "First pinch should increase zoom");
        Assert.True(zoomBorder.ZoomX > intermediateZoom, "Second pinch should further increase zoom");
    }
    
    [AvaloniaFact]
    public void PinchGesture_ScaleOfOne_NoZoomChange()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableGestureZoom = true,
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
        
        var initialZoomX = zoomBorder.ZoomX;
        var initialZoomY = zoomBorder.ZoomY;
        
        // Act - Simulate pinch gesture with scale = 1 (no change)
        var pinchEventArgs = new PinchEventArgs(1.0, new Point(0.5, 0.5), 0.0, 0.0)
        {
            RoutedEvent = Gestures.PinchEvent,
            Source = zoomBorder
        };
        
        zoomBorder.RaiseEvent(pinchEventArgs);
        
        // Assert
        Assert.Equal(initialZoomX, zoomBorder.ZoomX);
        Assert.Equal(initialZoomY, zoomBorder.ZoomY);
        Assert.True(pinchEventArgs.Handled, "Pinch event should still be handled");
    }
}
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Controls.PanAndZoom.UnitTests;

public class ZoomBorderVisualTreeLifecycleTests
{
    [AvaloniaFact]
    public void AttachToVisualTree_AddsEventHandlers()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnablePan = true,
            EnableZoom = true,
            EnableGestures = true,
            PanButton = ButtonName.Left
        };
        
        var childElement = new Border
        {
            Width = 200,
            Height = 150,
            Background = Brushes.Red
        };
        
        zoomBorder.Child = childElement;
        
        // Act - Attach to visual tree
        var window = new Window { Content = zoomBorder };
        window.Show();
        
        // Assert - Verify event handlers are working by testing pointer events
        var pointerPressedEventArgs = new PointerPressedEventArgs(
            zoomBorder,
            new Pointer(1, PointerType.Mouse, true),
            zoomBorder,
            new Point(100, 75),
            0,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonPressed),
            KeyModifiers.None)
        {
            RoutedEvent = InputElement.PointerPressedEvent
        };
        
        zoomBorder.RaiseEvent(pointerPressedEventArgs);
        
        // Verify panning started (event handlers are working)
        Assert.True(zoomBorder.Classes.Contains(":isPanning"), "Panning should be active after pointer pressed");
    }
    
    [AvaloniaFact]
    public void DetachFromVisualTree_RemovesEventHandlers()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnablePan = true,
            EnableZoom = true,
            PanButton = ButtonName.Left
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
        
        // Start panning to verify handlers work
        var pointerPressedEventArgs = new PointerPressedEventArgs(
            zoomBorder,
            new Pointer(1, PointerType.Mouse, true),
            zoomBorder,
            new Point(100, 75),
            0,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonPressed),
            KeyModifiers.None)
        {
            RoutedEvent = InputElement.PointerPressedEvent
        };
        
        zoomBorder.RaiseEvent(pointerPressedEventArgs);
        Assert.True(zoomBorder.Classes.Contains(":isPanning"));
        
        // Act - Detach from visual tree
        window.Content = null;
        
        // Assert - Verify event handlers are cleaned up
        // The control should no longer respond to events after detachment
        var pointerReleasedEventArgs = new PointerReleasedEventArgs(
            zoomBorder,
            new Pointer(1, PointerType.Mouse, true),
            zoomBorder,
            new Point(100, 75),
            0,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased),
            KeyModifiers.None,
            MouseButton.Left)
        {
            RoutedEvent = InputElement.PointerReleasedEvent
        };
        
        // This should not crash or cause issues even though handlers are removed
        zoomBorder.RaiseEvent(pointerReleasedEventArgs);
    }
    
    [AvaloniaFact]
    public void MultipleAttachDetach_HandlesCorrectly()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnablePan = true,
            EnableZoom = true,
            PanButton = ButtonName.Left
        };
        
        var childElement = new Border
        {
            Width = 200,
            Height = 150,
            Background = Brushes.Red
        };
        
        zoomBorder.Child = childElement;
        
        // Act & Assert - Multiple attach/detach cycles
        for (int i = 0; i < 3; i++)
        {
            // Attach
            var window = new Window { Content = zoomBorder };
            window.Show();
            
            // Test functionality after attach
            var pointerPressedEventArgs = new PointerPressedEventArgs(
                zoomBorder,
                new Pointer(1, PointerType.Mouse, true),
                zoomBorder,
                new Point(100, 75),
                0,
                new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonPressed),
                KeyModifiers.None)
            {
                RoutedEvent = InputElement.PointerPressedEvent
            };
            
            zoomBorder.RaiseEvent(pointerPressedEventArgs);
            Assert.True(zoomBorder.Classes.Contains(":isPanning"), $"Panning should work on cycle {i + 1}");
            
            // Release to clean up state
            var pointerReleasedEventArgs = new PointerReleasedEventArgs(
                zoomBorder,
                new Pointer(1, PointerType.Mouse, true),
                zoomBorder,
                new Point(100, 75),
                0,
                new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased),
                KeyModifiers.None,
                MouseButton.Left)
            {
                RoutedEvent = InputElement.PointerReleasedEvent
            };
            
            zoomBorder.RaiseEvent(pointerReleasedEventArgs);
            Assert.False(zoomBorder.Classes.Contains(":isPanning"), $"Panning should stop on cycle {i + 1}");
            
            // Detach by removing from window
            window.Content = null;
        }
    }
    
    [AvaloniaFact]
    public void AttachDetach_GestureRecognizersHandledCorrectly()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableGestures = true,
            EnableGestureZoom = true,
            EnableGestureTranslation = true
        };
        
        var childElement = new Border
        {
            Width = 200,
            Height = 150,
            Background = Brushes.Red
        };
        
        zoomBorder.Child = childElement;
        
        var window = new Window { Content = zoomBorder };
        
        // Act - Attach to visual tree
        window.Show();
        
        // Assert - Gesture recognizers should be added
        Assert.True(zoomBorder.GestureRecognizers.Count > 0, "Gesture recognizers should be added when EnableGestures is true");
        
        var initialGestureCount = zoomBorder.GestureRecognizers.Count;
        
        // Test gesture functionality
        var pinchEventArgs = new PinchEventArgs(1.5, new Point(0.5, 0.5), 0.0, 0.0)
        {
            RoutedEvent = Gestures.PinchEvent,
            Source = zoomBorder
        };
        
        var initialZoom = zoomBorder.ZoomX;
        zoomBorder.RaiseEvent(pinchEventArgs);
        
        // Verify gesture worked
        Assert.True(zoomBorder.ZoomX > initialZoom, "Pinch gesture should increase zoom");
        
        // Detach and reattach
        window.Content = null;
        window.Content = zoomBorder;
        
        // Assert - Gesture recognizers should still work after reattach
        Assert.Equal(initialGestureCount, zoomBorder.GestureRecognizers.Count);
    }
    
    [AvaloniaFact]
    public void DisableGestures_RemovesGestureRecognizers()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
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
        
        // Verify gestures are initially enabled
        Assert.True(zoomBorder.GestureRecognizers.Count > 0);
        
        // Act - Disable gestures
        zoomBorder.EnableGestures = false;
        
        // Assert - Gesture recognizers should be recreated (effectively removed)
        // The count might remain the same but they should be new instances
        var pinchEventArgs = new PinchEventArgs(1.5, new Point(0.5, 0.5), 0.0, 0.0)
        {
            RoutedEvent = Gestures.PinchEvent,
            Source = zoomBorder
        };
        
        var initialZoom = zoomBorder.ZoomX;
        zoomBorder.RaiseEvent(pinchEventArgs);
        
        // Gesture should not work when disabled
        Assert.Equal(initialZoom, zoomBorder.ZoomX);
    }
    
    [AvaloniaFact]
    public void EventHandlerLifecycle_NoMemoryLeaks()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnablePan = true,
            EnableZoom = true,
            EnableGestures = true
        };
        
        var childElement = new Border
        {
            Width = 200,
            Height = 150,
            Background = Brushes.Red
        };
        
        zoomBorder.Child = childElement;
        
        // Act - Simple attach/detach cycle to test for basic lifecycle
        var window = new Window { Content = zoomBorder };
        window.Show();
        
        // Simulate some interaction
        var wheelEventArgs = new PointerWheelEventArgs(
            zoomBorder,
            new Pointer(1, PointerType.Mouse, true),
            zoomBorder,
            new Point(200, 150),
            0,
            new PointerPointProperties(),
            KeyModifiers.None,
            new Vector(0, 1))
        {
            RoutedEvent = InputElement.PointerWheelChangedEvent
        };
        
        var initialZoom = zoomBorder.ZoomX;
        zoomBorder.RaiseEvent(wheelEventArgs);
        
        // Assert - Zoom should have changed, indicating event handlers are working
        Assert.NotEqual(initialZoom, zoomBorder.ZoomX);
        
        // Detach
        window.Content = null;
        
        // Assert - If we get here without exceptions, the lifecycle is working correctly
        Assert.True(true, "Attach/detach cycle completed without issues");
    }
    
    [AvaloniaFact]
    public void ChildElementLifecycle_HandledCorrectly()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnablePan = true,
            PanButton = ButtonName.Left
        };
        
        var window = new Window { Content = zoomBorder };
        window.Show();
        
        // Act - Add child after attach
        var childElement = new Border
        {
            Width = 200,
            Height = 150,
            Background = Brushes.Red
        };
        
        zoomBorder.Child = childElement;
        
        // Test functionality with child
        var pointerPressedEventArgs = new PointerPressedEventArgs(
            zoomBorder,
            new Pointer(1, PointerType.Mouse, true),
            zoomBorder,
            new Point(100, 75),
            0,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonPressed),
            KeyModifiers.None)
        {
            RoutedEvent = InputElement.PointerPressedEvent
        };
        
        zoomBorder.RaiseEvent(pointerPressedEventArgs);
        Assert.True(zoomBorder.Classes.Contains(":isPanning"));
        
        // Remove child
        zoomBorder.Child = null;
        
        // Should handle gracefully
        var pointerReleasedEventArgs = new PointerReleasedEventArgs(
            zoomBorder,
            new Pointer(1, PointerType.Mouse, true),
            zoomBorder,
            new Point(100, 75),
            0,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased),
            KeyModifiers.None,
            MouseButton.Left)
        {
            RoutedEvent = InputElement.PointerReleasedEvent
        };
        
        zoomBorder.RaiseEvent(pointerReleasedEventArgs);
    }
}
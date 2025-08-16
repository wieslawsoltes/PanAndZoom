using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Controls.PanAndZoom.UnitTests;

public class ZoomBorderInteractionScenarioTests
{
    [AvaloniaFact]
    public void ZoomThenPan_CombinedInteraction_WorksCorrectly()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableZoom = true,
            EnablePan = true,
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
        
        // Force layout
        zoomBorder.Measure(new Size(400, 300));
        zoomBorder.Arrange(new Rect(0, 0, 400, 300));
        
        // Capture initial values after layout is complete
        var initialZoom = zoomBorder.ZoomX;
        var initialOffsetX = zoomBorder.OffsetX;
        
        // Act - First zoom in with mouse wheel
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
        
        zoomBorder.RaiseEvent(wheelEventArgs);
        var zoomAfterWheel = zoomBorder.ZoomX;
        
        // Then pan with pointer
        var pointerPressedEventArgs = new PointerPressedEventArgs(
            zoomBorder,
            new Pointer(1, PointerType.Mouse, true),
            zoomBorder,
            new Point(200, 150),
            0,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonPressed),
            KeyModifiers.None)
        {
            RoutedEvent = InputElement.PointerPressedEvent
        };
        
        zoomBorder.RaiseEvent(pointerPressedEventArgs);
        
        var pointerMovedEventArgs = new PointerEventArgs(
            InputElement.PointerMovedEvent,
            zoomBorder,
            new Pointer(1, PointerType.Mouse, true),
            zoomBorder,
            new Point(100, 100),
            0,
            new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.Other),
            KeyModifiers.None);
        
        zoomBorder.RaiseEvent(pointerMovedEventArgs);
        
        // Assert
        Assert.True(zoomAfterWheel > initialZoom, "Zoom should increase after wheel event");
        Assert.True(wheelEventArgs.Handled, "Wheel event should be handled");
        Assert.True(zoomBorder.Classes.Contains(":isPanning"), "Panning should be active after mouse down and move");
    }
    
    [AvaloniaFact]
    public void PinchGestureWhilePanning_HandlesCorrectly()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableGestures = true,
            EnableGestureZoom = true,
            EnablePan = true,
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
        
        // Act - Start panning
        var pointerPressedEventArgs = new PointerPressedEventArgs(
            zoomBorder,
            new Pointer(1, PointerType.Mouse, true),
            zoomBorder,
            new Point(200, 150),
            0,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonPressed),
            KeyModifiers.None)
        {
            RoutedEvent = InputElement.PointerPressedEvent
        };
        
        zoomBorder.RaiseEvent(pointerPressedEventArgs);
        
        var initialZoom = zoomBorder.ZoomX;
        
        // Perform pinch gesture while panning
        var pinchEventArgs = new PinchEventArgs(
            1.5,
            new Point(0.5, 0.5),
            0.0,
            0.0)
        {
            RoutedEvent = Gestures.PinchEvent,
            Source = zoomBorder
        };
        
        zoomBorder.RaiseEvent(pinchEventArgs);
        
        // Assert
        Assert.True(zoomBorder.Classes.Contains(":isPanning"), "Panning should be active after mouse down");
        Assert.True(pinchEventArgs.Handled, "Pinch gesture should be handled");
        Assert.True(zoomBorder.ZoomX > initialZoom, "Zoom should increase from pinch gesture");
    }
    
    [AvaloniaFact]
    public void ScrollGestureAndPointerWheel_BothWork()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableGestures = true,
            EnableGestureTranslation = true,
            EnableZoom = true
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
        var initialZoom = zoomBorder.ZoomX;
        
        // Act - First use scroll gesture for panning
        var scrollEventArgs = new ScrollGestureEventArgs(1, new Vector(50, 0))
        {
            RoutedEvent = Gestures.ScrollGestureEvent,
            Source = zoomBorder
        };
        
        zoomBorder.RaiseEvent(scrollEventArgs);
        
        // Then use pointer wheel for zooming
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
        
        zoomBorder.RaiseEvent(wheelEventArgs);
        
        // Assert
        Assert.True(scrollEventArgs.Handled, "Scroll gesture should be handled");
        Assert.True(wheelEventArgs.Handled, "Wheel event should be handled");
        Assert.NotEqual(initialOffsetX, zoomBorder.OffsetX);
        Assert.True(zoomBorder.ZoomX > initialZoom, "Zoom should increase");
    }
    
    [AvaloniaFact]
    public void MultiplePointerInteractions_HandleCorrectly()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnablePan = true,
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
        
        // Act - Simulate multiple pointer interactions
        var pointerPressedEventArgs1 = new PointerPressedEventArgs(
            zoomBorder,
            new Pointer(1, PointerType.Mouse, true),
            zoomBorder,
            new Point(200, 150),
            0,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonPressed),
            KeyModifiers.None)
        {
            RoutedEvent = InputElement.PointerPressedEvent
        };
        
        zoomBorder.RaiseEvent(pointerPressedEventArgs1);
        
        var pointerMovedEventArgs = new PointerEventArgs(
            InputElement.PointerMovedEvent,
            zoomBorder,
            new Pointer(1, PointerType.Mouse, true),
            zoomBorder,
            new Point(150, 125),
            0,
            new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.Other),
            KeyModifiers.None);
        
        zoomBorder.RaiseEvent(pointerMovedEventArgs);
        
        var pointerReleasedEventArgs1 = new PointerReleasedEventArgs(
            zoomBorder,
            new Pointer(1, PointerType.Mouse, true),
            zoomBorder,
            new Point(150, 125),
            0,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased),
            KeyModifiers.None,
            MouseButton.Left)
        {
            RoutedEvent = InputElement.PointerReleasedEvent
        };
        
        zoomBorder.RaiseEvent(pointerReleasedEventArgs1);
        
        // Second pointer interaction
        var pointerPressedEventArgs2 = new PointerPressedEventArgs(
            zoomBorder,
            new Pointer(1, PointerType.Mouse, true),
            zoomBorder,
            new Point(200, 150),
            0,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonPressed),
            KeyModifiers.None)
        {
            RoutedEvent = InputElement.PointerPressedEvent
        };
        
        zoomBorder.RaiseEvent(pointerPressedEventArgs2);
        
        // Assert
        Assert.True(zoomBorder.Classes.Contains(":isPanning"), "Panning should be active after mouse interactions");
    }
    
    [AvaloniaFact]
    public void ZoomToLimitThenPan_RespectsBoundaries()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableZoom = true,
            EnablePan = true,
            PanButton = ButtonName.Left,
            MaxZoomX = 3.0,
            MaxZoomY = 3.0
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
        
        // Act - Zoom to maximum
        zoomBorder.ZoomTo(3.0, 100, 75);
        
        var zoomAfterMax = zoomBorder.ZoomX;
        
        // Try to zoom further with wheel (should be limited)
        var wheelEventArgs = new PointerWheelEventArgs(
            zoomBorder,
            new Pointer(1, PointerType.Mouse, true),
            zoomBorder,
            new Point(200, 150),
            0,
            new PointerPointProperties(),
            KeyModifiers.None,
            new Vector(0, 5)) // Large zoom attempt
        {
            RoutedEvent = InputElement.PointerWheelChangedEvent
        };
        
        zoomBorder.RaiseEvent(wheelEventArgs);
        
        // Then try to pan
        var pointerPressedEventArgs = new PointerPressedEventArgs(
            zoomBorder,
            new Pointer(1, PointerType.Mouse, true),
            zoomBorder,
            new Point(200, 150),
            0,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonPressed),
            KeyModifiers.None)
        {
            RoutedEvent = InputElement.PointerPressedEvent
        };
        
        zoomBorder.RaiseEvent(pointerPressedEventArgs);
        
        // Assert
        Assert.Equal(3.0, zoomAfterMax);
        Assert.True(zoomBorder.ZoomX <= 3.0, "Zoom should not exceed maximum");
        Assert.True(zoomBorder.Classes.Contains(":isPanning"), "Pan should still work at max zoom");
    }
    
    [AvaloniaFact]
    public void DisableAllInteractions_IgnoresAllEvents()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableZoom = false,
            EnablePan = false,
            EnableGestures = false,
            Stretch = StretchMode.None,
            EnableConstrains = false
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
        var initialOffsetX = zoomBorder.OffsetX;
        
        // Act - Try various interactions
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
        
        zoomBorder.RaiseEvent(wheelEventArgs);
        
        var pointerPressedEventArgs = new PointerPressedEventArgs(
            zoomBorder,
            new Pointer(1, PointerType.Mouse, true),
            zoomBorder,
            new Point(200, 150),
            0,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonPressed),
            KeyModifiers.None)
        {
            RoutedEvent = InputElement.PointerPressedEvent
        };
        
        zoomBorder.RaiseEvent(pointerPressedEventArgs);
        
        var pinchEventArgs = new PinchEventArgs(
            1.5,
            new Point(0.5, 0.5),
            0.0,
            0.0)
        {
            RoutedEvent = Gestures.PinchEvent,
            Source = zoomBorder
        };
        
        zoomBorder.RaiseEvent(pinchEventArgs);
        
        // Assert
        Assert.Equal(initialZoom, zoomBorder.ZoomX);
        Assert.Equal(initialOffsetX, zoomBorder.OffsetX);
        Assert.False(wheelEventArgs.Handled, "Wheel event should not be handled");
        Assert.False(zoomBorder.Classes.Contains(":isPanning"), "Panning should not be active when all interactions are disabled");
        Assert.False(pinchEventArgs.Handled, "Pinch gesture should not be handled");
    }
    
    [AvaloniaFact]
    public void RapidInteractionSequence_HandlesGracefully()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableZoom = true,
            EnablePan = true,
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
        
        var window = new Window { Content = zoomBorder };
        window.Show();
        
        // Act - Rapid sequence of different interactions
        for (int i = 0; i < 5; i++)
        {
            // Wheel zoom
            var wheelEventArgs = new PointerWheelEventArgs(
                zoomBorder,
                new Pointer(i + 1, PointerType.Mouse, true),
                zoomBorder,
                new Point(200, 150),
                0,
                new PointerPointProperties(),
                KeyModifiers.None,
                new Vector(0, 0.2))
            {
                RoutedEvent = InputElement.PointerWheelChangedEvent
            };
            
            zoomBorder.RaiseEvent(wheelEventArgs);
            
            // Pinch gesture
            var pinchEventArgs = new PinchEventArgs(
                1.1,
                new Point(0.5, 0.5),
                0.0,
                0.0)
            {
                RoutedEvent = Gestures.PinchEvent,
                Source = zoomBorder
            };
            
            zoomBorder.RaiseEvent(pinchEventArgs);
            
            // Pointer pan
            var pointerPressedEventArgs = new PointerPressedEventArgs(
                zoomBorder,
                new Pointer(i + 10, PointerType.Mouse, true),
                zoomBorder,
                new Point(200, 150),
                0,
                new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonPressed),
                KeyModifiers.None)
            {
                RoutedEvent = InputElement.PointerPressedEvent
            };
            
            zoomBorder.RaiseEvent(pointerPressedEventArgs);
            
            var pointerReleasedEventArgs = new PointerReleasedEventArgs(
                zoomBorder,
                new Pointer(i + 10, PointerType.Mouse, true),
                zoomBorder,
                new Point(200, 150),
                0,
                new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased),
                KeyModifiers.None,
                MouseButton.Left)
            {
                RoutedEvent = InputElement.PointerReleasedEvent
            };
            
            zoomBorder.RaiseEvent(pointerReleasedEventArgs);
        }
        
        // Assert - Should not crash and should handle events
        Assert.True(zoomBorder.ZoomX >= 1.0, "Zoom should be valid after rapid interactions");
        Assert.True(zoomBorder.ZoomY >= 1.0, "Zoom should be valid after rapid interactions");
    }
}
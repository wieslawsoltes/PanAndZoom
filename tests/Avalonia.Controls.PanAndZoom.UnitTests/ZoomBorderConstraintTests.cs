using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Controls.PanAndZoom.UnitTests;

public class ZoomBorderConstraintTests
{
    [AvaloniaFact]
    public void PointerWheel_ExceedsMaxZoom_ClampedToMaximum()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableZoom = true,
            MaxZoomX = 2.0,
            MaxZoomY = 2.0
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
        
        // Act - Try to zoom beyond maximum with large wheel delta
        var wheelEventArgs = new PointerWheelEventArgs(
            zoomBorder,
            new Pointer(1, PointerType.Mouse, true),
            zoomBorder,
            new Point(200, 150),
            0,
            new PointerPointProperties(),
            KeyModifiers.None,
            new Vector(0, 10))
        {
            RoutedEvent = InputElement.PointerWheelChangedEvent
        };
        
        zoomBorder.RaiseEvent(wheelEventArgs);
        
        // Assert
        Assert.True(zoomBorder.ZoomX <= 2.0, "ZoomX should not exceed maximum");
        Assert.True(zoomBorder.ZoomY <= 2.0, "ZoomY should not exceed maximum");
        Assert.True(wheelEventArgs.Handled, "Wheel event should be handled");
    }
    
    [AvaloniaFact]
    public void PointerWheel_BelowMinZoom_ClampedToMinimum()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableZoom = true,
            MinZoomX = 0.5,
            MinZoomY = 0.5
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
        
        // Act - Try to zoom below minimum with large negative wheel delta
        var wheelEventArgs = new PointerWheelEventArgs(
            zoomBorder,
            new Pointer(1, PointerType.Mouse, true),
            zoomBorder,
            new Point(200, 150),
            0,
            new PointerPointProperties(),
            KeyModifiers.None,
            new Vector(0, -10))
        {
            RoutedEvent = InputElement.PointerWheelChangedEvent
        };
        
        zoomBorder.RaiseEvent(wheelEventArgs);
        
        // Assert
        Assert.True(zoomBorder.ZoomX >= 0.5, "ZoomX should not go below minimum");
        Assert.True(zoomBorder.ZoomY >= 0.5, "ZoomY should not go below minimum");
        Assert.True(wheelEventArgs.Handled, "Wheel event should be handled");
    }
    
    [AvaloniaFact]
    public void PinchGesture_ExceedsMaxZoom_ClampedToMaximum()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableGestures = true,
            EnableGestureZoom = true,
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
        
        // First zoom close to maximum
        zoomBorder.ZoomTo(2.8, 100, 75);
        
        // Act - Try to zoom beyond maximum with pinch gesture
        var pinchEventArgs = new PinchEventArgs(2.0, new Point(0.5, 0.5), 0.0, 0.0)
        {
            RoutedEvent = Gestures.PinchEvent,
            Source = zoomBorder
        };
        
        zoomBorder.RaiseEvent(pinchEventArgs);
        
        // Assert
        Assert.True(zoomBorder.ZoomX <= 3.0, "ZoomX should not exceed maximum");
        Assert.True(zoomBorder.ZoomY <= 3.0, "ZoomY should not exceed maximum");
        Assert.True(pinchEventArgs.Handled, "Pinch event should be handled");
    }
    
    [AvaloniaFact]
    public void PinchGesture_BelowMinZoom_ClampedToMinimum()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableGestures = true,
            EnableGestureZoom = true,
            MinZoomX = 0.2,
            MinZoomY = 0.2
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
        
        // First zoom close to minimum
        zoomBorder.ZoomTo(0.3, 100, 75);
        
        // Act - Try to zoom below minimum with pinch gesture
        var pinchEventArgs = new PinchEventArgs(0.1, new Point(0.5, 0.5), 0.0, 0.0)
        {
            RoutedEvent = Gestures.PinchEvent,
            Source = zoomBorder
        };
        
        zoomBorder.RaiseEvent(pinchEventArgs);
        
        // Assert
        Assert.True(zoomBorder.ZoomX >= 0.2, "ZoomX should not go below minimum");
        Assert.True(zoomBorder.ZoomY >= 0.2, "ZoomY should not go below minimum");
        Assert.True(pinchEventArgs.Handled, "Pinch event should be handled");
    }
    
    [AvaloniaFact]
    public void Pan_WithOffsetLimits_RespectsBoundaries()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnablePan = true,
            PanButton = ButtonName.Left,
            MinOffsetX = -100,
            MaxOffsetX = 100,
            MinOffsetY = -75,
            MaxOffsetY = 75
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
        
        // Set offset close to maximum
        zoomBorder.Pan(95, 70);
        
        // Act - Try to pan beyond limits
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
        
        // Simulate large movement that would exceed limits
        var pointerMovedEventArgs = new PointerEventArgs(
            InputElement.PointerMovedEvent,
            zoomBorder,
            new Pointer(1, PointerType.Mouse, true),
            zoomBorder,
            new Point(300, 200),
            0,
            new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.Other),
            KeyModifiers.None);
        
        zoomBorder.RaiseEvent(pointerMovedEventArgs);
        
        // Assert
        Assert.True(zoomBorder.OffsetX <= 100, "OffsetX should not exceed maximum");
        Assert.True(zoomBorder.OffsetY <= 75, "OffsetY should not exceed maximum");
        Assert.True(zoomBorder.OffsetX >= -100, "OffsetX should not go below minimum");
        Assert.True(zoomBorder.OffsetY >= -75, "OffsetY should not go below minimum");
    }
    
    [AvaloniaFact]
    public void ScrollGesture_WithOffsetLimits_RespectsBoundaries()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableGestures = true,
            EnableGestureTranslation = true,
            MinOffsetX = -50,
            MaxOffsetX = 50,
            MinOffsetY = -50,
            MaxOffsetY = 50
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
        
        // Set offset close to limits
        zoomBorder.Pan(45, 45);
        
        // Act - Try to scroll beyond limits
        var scrollEventArgs = new ScrollGestureEventArgs(1, new Vector(100, 100))
        {
            RoutedEvent = Gestures.ScrollGestureEvent,
            Source = zoomBorder
        };
        
        zoomBorder.RaiseEvent(scrollEventArgs);
        
        // Assert
        Assert.True(zoomBorder.OffsetX <= 50, "OffsetX should not exceed maximum");
        Assert.True(zoomBorder.OffsetY <= 50, "OffsetY should not exceed maximum");
        Assert.True(scrollEventArgs.Handled, "Scroll gesture should be handled");
    }
    
    [AvaloniaFact]
    public void ZoomConstraints_DifferentXAndY_HandledIndependently()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableZoom = true,
            MinZoomX = 0.5,
            MaxZoomX = 2.0,
            MinZoomY = 1.0,
            MaxZoomY = 4.0
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
        
        // Act - Try to zoom beyond different limits for X and Y
        var wheelEventArgs = new PointerWheelEventArgs(
            zoomBorder,
            new Pointer(1, PointerType.Mouse, true),
            zoomBorder,
            new Point(200, 150),
            0,
            new PointerPointProperties(),
            KeyModifiers.None,
            new Vector(0, 10))
        {
            RoutedEvent = InputElement.PointerWheelChangedEvent
        };
        
        zoomBorder.RaiseEvent(wheelEventArgs);
        
        // Assert
        Assert.True(zoomBorder.ZoomX <= 2.0, "ZoomX should respect its maximum");
        Assert.True(zoomBorder.ZoomY <= 4.0, "ZoomY should respect its maximum");
        Assert.True(zoomBorder.ZoomX >= 0.5, "ZoomX should respect its minimum");
        Assert.True(zoomBorder.ZoomY >= 1.0, "ZoomY should respect its minimum");
    }
    
    [AvaloniaFact]
    public void OffsetConstraints_DifferentXAndY_HandledIndependently()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnablePan = true,
            PanButton = ButtonName.Left,
            MinOffsetX = -200,
            MaxOffsetX = 200,
            MinOffsetY = -50,
            MaxOffsetY = 50
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
        
        // Act - Set offsets that test different limits
        zoomBorder.Pan(250, 100); // Should be clamped to 200, 50
        
        // Assert
        Assert.True(zoomBorder.OffsetX <= 200, "OffsetX should respect its maximum");
        Assert.True(zoomBorder.OffsetY <= 50, "OffsetY should respect its maximum");
        
        // Test minimum limits
        zoomBorder.Pan(-250, -100); // Should be clamped to -200, -50
        
        Assert.True(zoomBorder.OffsetX >= -200, "OffsetX should respect its minimum");
        Assert.True(zoomBorder.OffsetY >= -50, "OffsetY should respect its minimum");
    }
    
    [AvaloniaFact]
    public void ConstraintValidation_WithNegativeValues_HandlesCorrectly()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableZoom = true,
            EnablePan = true,
            MinZoomX = 0.1,
            MaxZoomX = 10.0,
            MinZoomY = 0.1,
            MaxZoomY = 10.0,
            MinOffsetX = -1000,
            MaxOffsetX = 1000,
            MinOffsetY = -1000,
            MaxOffsetY = 1000
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
        
        // Act - Test with extreme values
        zoomBorder.Zoom(0.05, 100, 75); // Below minimum zoom
        zoomBorder.Pan(-2000, -2000); // Below minimum offset
        
        // Assert
        Assert.True(zoomBorder.ZoomX >= 0.1, "ZoomX should be clamped to minimum");
        Assert.True(zoomBorder.ZoomY >= 0.1, "ZoomY should be clamped to minimum");
        Assert.True(zoomBorder.OffsetX >= -1000, "OffsetX should be clamped to minimum");
        Assert.True(zoomBorder.OffsetY >= -1000, "OffsetY should be clamped to minimum");
        
        // Test with values above maximum
        zoomBorder.Zoom(15.0, 100, 75); // Above maximum zoom
        zoomBorder.Pan(2000, 2000); // Above maximum offset
        
        Assert.True(zoomBorder.ZoomX <= 10.0, "ZoomX should be clamped to maximum");
        Assert.True(zoomBorder.ZoomY <= 10.0, "ZoomY should be clamped to maximum");
        Assert.True(zoomBorder.OffsetX <= 1000, "OffsetX should be clamped to maximum");
        Assert.True(zoomBorder.OffsetY <= 1000, "OffsetY should be clamped to maximum");
    }
    
    [AvaloniaFact]
    public void ConstraintValidation_DuringInteraction_MaintainsConsistency()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableZoom = true,
            EnablePan = true,
            EnableGestures = true,
            PanButton = ButtonName.Left,
            MinZoomX = 0.5,
            MaxZoomX = 3.0,
            MinOffsetX = -100,
            MaxOffsetX = 100
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
        
        // Act - Perform multiple interactions that could violate constraints
        for (int i = 0; i < 10; i++)
        {
            // Zoom
            var wheelEventArgs = new PointerWheelEventArgs(
                zoomBorder,
                new Pointer(i + 1, PointerType.Mouse, true),
                zoomBorder,
                new Point(200, 150),
                0,
                new PointerPointProperties(),
                KeyModifiers.None,
                new Vector(0, i % 2 == 0 ? 1 : -1))
            {
                RoutedEvent = InputElement.PointerWheelChangedEvent
            };
            
            zoomBorder.RaiseEvent(wheelEventArgs);
            
            // Pan
            var scrollEventArgs = new ScrollGestureEventArgs(i + 1, new Vector(i % 2 == 0 ? 20 : -20, 0))
            {
                RoutedEvent = Gestures.ScrollGestureEvent,
                Source = zoomBorder
            };
            
            zoomBorder.RaiseEvent(scrollEventArgs);
            
            // Assert constraints are maintained after each interaction
            Assert.True(zoomBorder.ZoomX >= 0.5 && zoomBorder.ZoomX <= 3.0, 
                $"ZoomX constraint violated at iteration {i}: {zoomBorder.ZoomX}");
            Assert.True(zoomBorder.OffsetX >= -100 && zoomBorder.OffsetX <= 100, 
                $"OffsetX constraint violated at iteration {i}: {zoomBorder.OffsetX}");
        }
    }
}

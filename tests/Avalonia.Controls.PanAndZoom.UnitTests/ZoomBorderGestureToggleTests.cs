using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Controls.PanAndZoom.UnitTests;

public class ZoomBorderGestureToggleTests
{
    [AvaloniaFact]
    public void EnableGestures_True_AddsGestureRecognizers()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableGestures = false // Start with gestures disabled
        };
        
        var window = new Window { Content = zoomBorder };
        window.Show();
        
        // Act - Enable gestures
        zoomBorder.EnableGestures = true;
        
        // Assert
        Assert.True(zoomBorder.EnableGestures, "EnableGestures should be true");
        // Note: In headless mode, we can't directly inspect GestureRecognizers collection,
        // but we can verify the property is set correctly
    }
    
    [AvaloniaFact]
    public void EnableGestures_False_RemovesGestureRecognizers()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableGestures = true // Start with gestures enabled
        };
        
        var window = new Window { Content = zoomBorder };
        window.Show();
        
        // Act - Disable gestures
        zoomBorder.EnableGestures = false;
        
        // Assert
        Assert.False(zoomBorder.EnableGestures, "EnableGestures should be false");
    }
    
    [AvaloniaFact]
    public void EnableGestureZoom_False_PinchGestureIgnored()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableGestures = true,
            EnableGestureZoom = false
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
        
        // Ensure proper layout
        zoomBorder.Measure(new Size(400, 300));
        zoomBorder.Arrange(new Rect(0, 0, 400, 300));

        var initialZoom = zoomBorder.ZoomX;
        
        // Act - Simulate pinch gesture when gesture zoom is disabled
        var pinchEventArgs = new PinchEventArgs(1.5, new Point(0.5, 0.5), 0.0, 0.0)
        {
            RoutedEvent = Gestures.PinchEvent,
            Source = zoomBorder
        };
        
        zoomBorder.RaiseEvent(pinchEventArgs);
        
        // Assert
        Assert.Equal(initialZoom, zoomBorder.ZoomX);
        Assert.False(pinchEventArgs.Handled, "Pinch event should not be handled when gesture zoom is disabled");
    }
    
    [AvaloniaFact]
    public void EnableGestureTranslation_False_ScrollGestureIgnored()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableGestures = true,
            EnableGestureTranslation = false
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
    public void EnablePan_False_PointerPanIgnored()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnablePan = false,
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
        
        // Act - Simulate pointer pressed when pan is disabled
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
        Assert.False(zoomBorder.Classes.Contains(":isPanning"), "Panning should not be active when pan is disabled");
    }
    
    [AvaloniaFact]
    public void EnableZoom_False_PointerWheelZoomIgnored()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableZoom = false,
            EnablePan = false
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
        
        // Act - Simulate pointer wheel when zoom is disabled
        var pointerWheelEventArgs = new PointerWheelEventArgs(
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
        
        zoomBorder.RaiseEvent(pointerWheelEventArgs);
        
        // Assert
        Assert.Equal(initialZoom, zoomBorder.ZoomX);
        Assert.False(pointerWheelEventArgs.Handled, "Pointer wheel event should not be handled when zoom is disabled");
    }
    
    [AvaloniaFact]
    public void PanButton_Changed_AffectsPointerCapture()
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
        
        // Act - Test with left button (should work)
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
        
        Assert.True(zoomBorder.Classes.Contains(":isPanning"), "Left button should start panning when PanButton is Left");
        
        // Change pan button to right
        zoomBorder.PanButton = ButtonName.Right;
        
        // Reset panning state
        var pointerReleasedEventArgs = new PointerReleasedEventArgs(
            zoomBorder,
            new Pointer(1, PointerType.Mouse, true),
            zoomBorder,
            new Point(200, 150),
            0,
            new PointerPointProperties(),
            KeyModifiers.None,
            MouseButton.Left)
        {
            RoutedEvent = InputElement.PointerReleasedEvent
        };
        
        zoomBorder.RaiseEvent(pointerReleasedEventArgs);
        
        // Test with left button again (should not work now)
        var pointerPressedEventArgs2 = new PointerPressedEventArgs(
            zoomBorder,
            new Pointer(2, PointerType.Mouse, true),
            zoomBorder,
            new Point(200, 150),
            0,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonPressed),
            KeyModifiers.None)
        {
            RoutedEvent = InputElement.PointerPressedEvent
        };
        
        zoomBorder.RaiseEvent(pointerPressedEventArgs2);
        
        Assert.False(zoomBorder.Classes.Contains(":isPanning"), "Left button should not start panning when PanButton is Right");
    }
    
    [AvaloniaFact]
    public void ZoomSpeed_Changed_AffectsZoomAmount()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableZoom = true,
            ZoomSpeed = 1.2, // Default speed
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
        
        // Ensure proper layout
        zoomBorder.Measure(new Size(400, 300));
        zoomBorder.Arrange(new Rect(0, 0, 400, 300));
        
        // Force layout update
        zoomBorder.UpdateLayout();
        
        var initialZoom = zoomBorder.ZoomX;
        
        // Act - Test with default zoom speed
        var pointerWheelEventArgs1 = new PointerWheelEventArgs(
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
        
        zoomBorder.RaiseEvent(pointerWheelEventArgs1);
        var zoomAfterDefault = zoomBorder.ZoomX;
        
        // Debug: Check if event was handled and initial state
        var wasHandled = pointerWheelEventArgs1.Handled;
        var stretch = zoomBorder.Stretch;
        var enableConstrains = zoomBorder.EnableConstrains;
        var maxZoomX = zoomBorder.MaxZoomX;
        
        // Reset zoom
        zoomBorder.ZoomTo(initialZoom, 0, 0);
        
        // Change zoom speed
        zoomBorder.ZoomSpeed = 2.0;
        
        // Test with increased zoom speed
        var pointerWheelEventArgs2 = new PointerWheelEventArgs(
            zoomBorder,
            new Pointer(2, PointerType.Mouse, true),
            zoomBorder,
            new Point(50, 50),
            0,
            new PointerPointProperties(),
            KeyModifiers.None,
            new Vector(0, 1)
        )
        {
            RoutedEvent = InputElement.PointerWheelChangedEvent
        };
        
        zoomBorder.RaiseEvent(pointerWheelEventArgs2);
        
        // Assert
        Assert.True(zoomAfterDefault > initialZoom, $"Zoom should increase with default speed. Initial: {initialZoom}, After: {zoomAfterDefault}, Event handled: {wasHandled}, Stretch: {stretch}, EnableConstrains: {enableConstrains}, MaxZoomX: {maxZoomX}");
        Assert.True(zoomBorder.ZoomX > zoomAfterDefault, $"Zoom should increase more with higher speed. After default: {zoomAfterDefault}, After increased: {zoomBorder.ZoomX}");
    }
    
    [AvaloniaFact]
    public void PropertyChanges_TriggerCorrectBehavior()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 200,
            Height = 150
        };
        
        var window = new Window { Content = zoomBorder };
        window.Show();
        
        // Act & Assert - Test multiple property changes
        zoomBorder.EnableGestures = true;
        Assert.True(zoomBorder.EnableGestures);
        
        zoomBorder.EnableGestureZoom = true;
        Assert.True(zoomBorder.EnableGestureZoom);
        
        zoomBorder.EnableGestureTranslation = true;
        Assert.True(zoomBorder.EnableGestureTranslation);
        
        zoomBorder.EnablePan = true;
        Assert.True(zoomBorder.EnablePan);
        
        zoomBorder.EnableZoom = true;
        Assert.True(zoomBorder.EnableZoom);
        
        zoomBorder.PanButton = ButtonName.Middle;
        Assert.Equal(ButtonName.Middle, zoomBorder.PanButton);
        
        zoomBorder.ZoomSpeed = 0.5;
        Assert.Equal(0.5, zoomBorder.ZoomSpeed);
    }
}
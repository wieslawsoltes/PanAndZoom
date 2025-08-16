using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Controls.PanAndZoom.UnitTests;

public class ZoomBorderPointerPanTests
{
    [AvaloniaFact]
    public void PointerPressed_LeftButton_StartsPanning()
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
        
        // Act - Simulate left mouse button press
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
        
        // Assert
        Assert.True(zoomBorder.Classes.Contains(":isPanning"), "Should have isPanning pseudo class when panning starts");
    }
    
    [AvaloniaFact]
    public void PointerPressed_RightButton_StartsPanning_WhenConfigured()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnablePan = true,
            PanButton = ButtonName.Right
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
        
        // Act - Simulate right mouse button press
        var pointerPressedEventArgs = new PointerPressedEventArgs(
            zoomBorder,
            new Pointer(1, PointerType.Mouse, true),
            zoomBorder,
            new Point(100, 75),
            0,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.RightButtonPressed),
            KeyModifiers.None)
        {
            RoutedEvent = InputElement.PointerPressedEvent
        };
        
        zoomBorder.RaiseEvent(pointerPressedEventArgs);
        
        // Assert
        Assert.True(zoomBorder.Classes.Contains(":isPanning"), "Should have isPanning pseudo class when right button panning starts");
    }
    
    [AvaloniaFact]
    public void PointerPressed_MiddleButton_StartsPanning_WhenConfigured()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnablePan = true,
            PanButton = ButtonName.Middle
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
        
        // Act - Simulate middle mouse button press
        var pointerPressedEventArgs = new PointerPressedEventArgs(
            zoomBorder,
            new Pointer(1, PointerType.Mouse, true),
            zoomBorder,
            new Point(100, 75),
            0,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.MiddleButtonPressed),
            KeyModifiers.None)
        {
            RoutedEvent = InputElement.PointerPressedEvent
        };
        
        zoomBorder.RaiseEvent(pointerPressedEventArgs);
        
        // Assert
        Assert.True(zoomBorder.Classes.Contains(":isPanning"), "Should have isPanning pseudo class when middle button panning starts");
    }
    
    [AvaloniaFact]
    public void PointerPressed_WrongButton_DoesNotStartPanning()
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
        
        // Act - Simulate right mouse button press when left is configured
        var pointerPressedEventArgs = new PointerPressedEventArgs(
            zoomBorder,
            new Pointer(1, PointerType.Mouse, true),
            zoomBorder,
            new Point(100, 75),
            0,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.RightButtonPressed),
            KeyModifiers.None)
        {
            RoutedEvent = InputElement.PointerPressedEvent
        };
        
        zoomBorder.RaiseEvent(pointerPressedEventArgs);
        
        // Assert
        Assert.False(zoomBorder.Classes.Contains(":isPanning"), "Should not have isPanning pseudo class when wrong button is pressed");
    }
    
    [AvaloniaFact]
    public void PointerMoved_WhilePanning_ChangesOffset()
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
        
        // Start panning
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
        
        var initialOffsetX = zoomBorder.OffsetX;
        var initialOffsetY = zoomBorder.OffsetY;
        
        // Act - Simulate pointer move while panning
        var pointerMovedEventArgs = new PointerEventArgs(
            InputElement.PointerMovedEvent,
            zoomBorder,
            new Pointer(1, PointerType.Mouse, true),
            zoomBorder,
            new Point(150, 125), // Move 50 pixels right and down
            0,
            new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.Other),
            KeyModifiers.None);
        
        zoomBorder.RaiseEvent(pointerMovedEventArgs);
        
        // Assert
        Assert.True(zoomBorder.OffsetX != initialOffsetX || zoomBorder.OffsetY != initialOffsetY, 
            "Offset should change when pointer moves during panning");
    }
    
    [AvaloniaFact]
    public void PointerReleased_EndsPanning()
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
        
        // Start panning
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
        
        Assert.True(zoomBorder.Classes.Contains(":isPanning"), "Should be panning after press");
        
        // Act - Simulate pointer release
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
        
        // Assert
        Assert.False(zoomBorder.Classes.Contains(":isPanning"), "Should not be panning after release");
    }
    
    [AvaloniaFact]
    public void PointerPressed_PanDisabled_DoesNotStartPanning()
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
        
        // Act - Simulate left mouse button press when pan is disabled
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
        
        // Assert
        Assert.False(zoomBorder.Classes.Contains(":isPanning"), "Should not start panning when EnablePan is false");
    }
    
    [AvaloniaFact]
    public void PointerMoved_NotPanning_DoesNotChangeOffset()
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
        
        var initialOffsetX = zoomBorder.OffsetX;
        var initialOffsetY = zoomBorder.OffsetY;
        
        // Act - Simulate pointer move without starting panning
        var pointerMovedEventArgs = new PointerEventArgs(
            InputElement.PointerMovedEvent,
            zoomBorder,
            new Pointer(1, PointerType.Mouse, true),
            zoomBorder,
            new Point(150, 125),
            0,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.Other),
            KeyModifiers.None);
        
        zoomBorder.RaiseEvent(pointerMovedEventArgs);
        
        // Assert
        Assert.Equal(initialOffsetX, zoomBorder.OffsetX);
        Assert.Equal(initialOffsetY, zoomBorder.OffsetY);
    }
}
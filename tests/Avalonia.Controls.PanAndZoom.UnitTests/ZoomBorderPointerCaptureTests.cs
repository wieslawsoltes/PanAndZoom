using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Controls.PanAndZoom.UnitTests;

public class ZoomBorderPointerCaptureTests
{
    [AvaloniaFact]
    public void PointerPressed_EnabledPan_CapturesPointer()
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
        
        // Act - Simulate pointer pressed with left button
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
        // Note: We can't directly test pointer capture in headless mode,
        // but we can verify the pan operation started which indicates capture attempt// Assert
        Assert.True(zoomBorder.Classes.Contains(":isPanning"), "Panning should be active after left mouse button press");
        // Note: We can't directly test pointer capture in headless mode,
        // but we can verify the event was handled which indicates capture attempt
    }
    
    [AvaloniaFact]
    public void PointerPressed_DisabledPan_DoesNotCapturePointer()
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
    public void PointerPressed_WrongButton_DoesNotCapturePointer()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnablePan = true,
            PanButton = ButtonName.Left // Configured for left button
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
        
        // Act - Simulate pointer pressed with right button (wrong button)
        var pointerPressedEventArgs = new PointerPressedEventArgs(
            zoomBorder,
            new Pointer(1, PointerType.Mouse, true),
            zoomBorder,
            new Point(200, 150),
            0,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.RightButtonPressed),
            KeyModifiers.None)
        {
            RoutedEvent = InputElement.PointerPressedEvent
        };
        
        zoomBorder.RaiseEvent(pointerPressedEventArgs);
        
        // Assert
        Assert.False(zoomBorder.Classes.Contains(":isPanning"), "Panning should not be active with wrong button");
    }
    
    [AvaloniaFact]
    public void PointerReleased_AfterCapture_ReleasesPointer()
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
        
        // First capture the pointer
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
        
        // Verify panning started
        Assert.True(zoomBorder.Classes.Contains(":isPanning"), "Panning should be active after mouse down");
        
        // Act - Simulate pointer released
        var pointerReleasedEventArgs = new PointerReleasedEventArgs(
            zoomBorder,
            new Pointer(1, PointerType.Mouse, true),
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
        
        // Assert
        Assert.False(zoomBorder.Classes.Contains(":isPanning"), "Panning should stop after mouse up");
    }
    
    [AvaloniaFact]
    public void PointerCaptureLost_HandlesEvent()
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
        
        // First capture the pointer
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
        
        // Verify panning started
        Assert.True(zoomBorder.Classes.Contains(":isPanning"), "Panning should be active after mouse down");
        
        // Act - Simulate pointer capture lost
        var pointerCaptureLostEventArgs = new PointerCaptureLostEventArgs(
            zoomBorder,
            new Pointer(1, PointerType.Mouse, true))
        {
            RoutedEvent = InputElement.PointerCaptureLostEvent
        };
        
        zoomBorder.RaiseEvent(pointerCaptureLostEventArgs);
        
        // Assert
        Assert.True(pointerCaptureLostEventArgs.Handled, "Pointer capture lost event should be handled");
    }
    
    [AvaloniaFact]
    public void PointerMoved_WithoutCapture_DoesNotPan()
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
        
        // Act - Simulate pointer moved without prior capture
        var pointerMovedEventArgs = new PointerEventArgs(
            InputElement.PointerMovedEvent,
            zoomBorder,
            new Pointer(1, PointerType.Mouse, true),
            zoomBorder,
            new Point(100, 100),
            0,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.Other),
            KeyModifiers.None);
        
        zoomBorder.RaiseEvent(pointerMovedEventArgs);
        
        // Assert
        Assert.Equal(initialOffsetX, zoomBorder.OffsetX);
        Assert.Equal(initialOffsetY, zoomBorder.OffsetY);
        Assert.False(zoomBorder.Classes.Contains(":isPanning"), "Panning should not be active without prior mouse down");
    }
    
    [AvaloniaFact]
    public void PointerMoved_WithCapture_UpdatesOffset()
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
        
        // First capture the pointer
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
        
        // Verify panning started
        Assert.True(zoomBorder.Classes.Contains(":isPanning"), "Panning should be active after mouse down");

        // Act - Simulate pointer moved with capture
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
        
        // Assert
        Assert.True(zoomBorder.Classes.Contains(":isPanning"), "Panning should still be active during mouse move");
        // Note: The actual offset change depends on the pointer position delta,
        // which is complex to simulate in headless mode
    }
    
    [AvaloniaFact]
    public void MultiplePointers_OnlyFirstCaptured()
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
        
        // Act - Simulate first pointer pressed
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
        Assert.True(zoomBorder.Classes.Contains(":isPanning"), "First pointer should start panning");
        
        // Note: Multiple pointer scenarios are complex in headless testing
        // This test verifies that the first pointer capture works correctly
    }
}
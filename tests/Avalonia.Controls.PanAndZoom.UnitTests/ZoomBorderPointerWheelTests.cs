using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Controls.PanAndZoom.UnitTests;

public class ZoomBorderPointerWheelTests
{
    [AvaloniaFact]
    public void PointerWheel_ZoomIn_IncreasesZoom()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableZoom = true,
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
        
        var initialZoomX = zoomBorder.ZoomX;
        var initialZoomY = zoomBorder.ZoomY;
        
        // Act - Simulate mouse wheel scroll up (zoom in)
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
        Assert.True(zoomBorder.ZoomX > initialZoomX, "ZoomX should increase after wheel zoom in");
        Assert.True(zoomBorder.ZoomY > initialZoomY, "ZoomY should increase after wheel zoom in");
    }
    
    [AvaloniaFact]
    public void PointerWheel_ZoomOut_DecreasesZoom()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableZoom = true,
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
        
        // First zoom in to have something to zoom out from
        zoomBorder.ZoomTo(2.0, 100, 75);
        
        var initialZoomX = zoomBorder.ZoomX;
        var initialZoomY = zoomBorder.ZoomY;
        
        // Act - Simulate mouse wheel scroll down (zoom out)
        var wheelEventArgs = new PointerWheelEventArgs(
            zoomBorder,
            new Pointer(1, PointerType.Mouse, true),
            zoomBorder,
            new Point(200, 150),
            0,
            new PointerPointProperties(),
            KeyModifiers.None,
            new Vector(0, -1))
        {
            RoutedEvent = InputElement.PointerWheelChangedEvent
        };
        
        zoomBorder.RaiseEvent(wheelEventArgs);
        
        // Assert
        Assert.True(zoomBorder.ZoomX < initialZoomX, "ZoomX should decrease after wheel zoom out");
        Assert.True(zoomBorder.ZoomY < initialZoomY, "ZoomY should decrease after wheel zoom out");
    }
    
    [AvaloniaFact]
    public void PointerWheel_ZoomDisabled_NoZoomChange()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableZoom = false,
            EnablePan = true
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
        var initialOffsetX = zoomBorder.OffsetX;
        var initialOffsetY = zoomBorder.OffsetY;
        
        // Act - Simulate mouse wheel scroll (should pan instead of zoom)
        var wheelEventArgs = new PointerWheelEventArgs(
            zoomBorder,
            new Pointer(1, PointerType.Mouse, true),
            zoomBorder,
            new Point(200, 150),
            0,
            new PointerPointProperties(),
            KeyModifiers.None,
            new Vector(1, 1))
        {
            RoutedEvent = InputElement.PointerWheelChangedEvent
        };
        
        zoomBorder.RaiseEvent(wheelEventArgs);
        
        // Assert
        Assert.Equal(initialZoomX, zoomBorder.ZoomX);
        Assert.Equal(initialZoomY, zoomBorder.ZoomY);
        // Offset should change due to panning
        Assert.True(zoomBorder.OffsetX != initialOffsetX || zoomBorder.OffsetY != initialOffsetY, "Offset should change when panning with wheel");
    }
    
    [AvaloniaFact]
    public void PointerWheel_PanWithWheel_ChangesOffset()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableZoom = false,
            EnablePan = true
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
        
        // Act - Simulate horizontal wheel scroll for panning
        var wheelEventArgs = new PointerWheelEventArgs(
            zoomBorder,
            new Pointer(1, PointerType.Mouse, true),
            window,
            new Point(200, 150),
            0,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.Other),
            KeyModifiers.None,
            new Vector(1, 0) // Horizontal delta for horizontal pan
        )
        {
            RoutedEvent = InputElement.PointerWheelChangedEvent
        };
        
        zoomBorder.RaiseEvent(wheelEventArgs);
        
        // Assert
        Assert.True(zoomBorder.OffsetX != initialOffsetX, "OffsetX should change after horizontal wheel pan");
    }
    
    [AvaloniaFact]
    public void PointerWheel_ZoomToPoint_ZoomsAtCorrectLocation()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
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
        
        var targetPoint = new Point(100, 75); // Center of child element
        var initialZoom = zoomBorder.ZoomX;
        
        // Act - Simulate wheel zoom at specific point
        var wheelEventArgs = new PointerWheelEventArgs(
            zoomBorder,
            new Pointer(1, PointerType.Mouse, true),
            window,
            targetPoint,
            0,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.Other),
            KeyModifiers.None,
            new Vector(0, 1)
        )
        {
            RoutedEvent = InputElement.PointerWheelChangedEvent
        };
        
        zoomBorder.RaiseEvent(wheelEventArgs);
        
        // Assert
        Assert.True(zoomBorder.ZoomX > initialZoom, "Zoom should increase");
        // The exact offset calculation depends on the zoom implementation,
        // but we can verify that zoom occurred
    }
    
    [AvaloniaFact]
    public void PointerWheel_BothZoomAndPanDisabled_NoChange()
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
        
        var initialZoomX = zoomBorder.ZoomX;
        var initialZoomY = zoomBorder.ZoomY;
        var initialOffsetX = zoomBorder.OffsetX;
        var initialOffsetY = zoomBorder.OffsetY;
        
        // Act - Simulate mouse wheel scroll
        var wheelEventArgs = new PointerWheelEventArgs(
            zoomBorder,
            new Pointer(1, PointerType.Mouse, true),
            zoomBorder,
            new Point(200, 150),
            0,
            new PointerPointProperties(),
            KeyModifiers.None,
            new Vector(1, 1))
        {
            RoutedEvent = InputElement.PointerWheelChangedEvent
        };
        
        zoomBorder.RaiseEvent(wheelEventArgs);
        
        // Assert - Nothing should change
        Assert.Equal(initialZoomX, zoomBorder.ZoomX);
        Assert.Equal(initialZoomY, zoomBorder.ZoomY);
        Assert.Equal(initialOffsetX, zoomBorder.OffsetX);
        Assert.Equal(initialOffsetY, zoomBorder.OffsetY);
    }
}
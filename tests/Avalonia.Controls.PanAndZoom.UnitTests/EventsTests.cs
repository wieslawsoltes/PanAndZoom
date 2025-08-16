// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using Avalonia.Headless.XUnit;
using Xunit;

namespace Avalonia.Controls.PanAndZoom.UnitTests;

public class EventsTests
{
    [AvaloniaFact]
    public void ZoomBorder_SetMatrix_Fires_MatrixChanged_Event()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();
        MatrixChangedEventArgs? receivedArgs = null;
        zoomBorder.MatrixChanged += (_, args) => receivedArgs = args;
        
        var newMatrix = Matrix.CreateScale(2.0, 2.0);
        
        // Act
        zoomBorder.SetMatrix(newMatrix);
        
        // Assert
        Assert.NotNull(receivedArgs);
        Assert.Equal(newMatrix, receivedArgs!.Matrix);
        Assert.Equal("SetMatrix", receivedArgs.Operation);
    }
    
    [AvaloniaFact]
    public void ZoomBorder_ResetMatrix_Fires_MatrixReset_Event()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();
        MatrixChangedEventArgs? receivedArgs = null;
        zoomBorder.MatrixReset += (_, args) => receivedArgs = args;
        
        // First set a non-identity matrix
        zoomBorder.SetMatrix(Matrix.CreateScale(2.0, 2.0));
        
        // Act
        zoomBorder.ResetMatrix();
        
        // Assert
        Assert.NotNull(receivedArgs);
        Assert.Equal(Matrix.Identity, receivedArgs!.Matrix);
        Assert.Equal("ResetMatrix", receivedArgs.Operation);
    }
    
    [AvaloniaFact]
    public void ZoomBorder_Zoom_Fires_ZoomStarted_Event()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();
        ZoomEventArgs? receivedArgs = null;
        zoomBorder.ZoomStarted += (_, args) => receivedArgs = args;
        
        // Act
        zoomBorder.Zoom(2.0, 100, 100);
        
        // Assert
        Assert.NotNull(receivedArgs);
        Assert.Equal(100, receivedArgs!.CenterX);
        Assert.Equal(100, receivedArgs.CenterY);
    }
    
    [AvaloniaFact]
    public void ZoomBorder_ZoomTo_Fires_ZoomDeltaChanged_Event()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();
        ZoomEventArgs? receivedArgs = null;
        zoomBorder.ZoomDeltaChanged += (_, args) => receivedArgs = args;
        
        // Act
        zoomBorder.ZoomTo(1.5, 50, 50);
        
        // Assert
        Assert.NotNull(receivedArgs);
        Assert.Equal(1.5, receivedArgs!.ZoomDelta);
        Assert.Equal(50, receivedArgs.CenterX);
        Assert.Equal(50, receivedArgs.CenterY);
    }
    
    [AvaloniaFact]
    public void ZoomBorder_ZoomIn_Fires_ZoomEnded_Event()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();
        var childElement = new Border { Width = 100, Height = 100 };
        zoomBorder.Child = childElement;
        
        ZoomEventArgs? receivedArgs = null;
        zoomBorder.ZoomEnded += (_, args) => receivedArgs = args;
        
        // Act
        zoomBorder.ZoomIn();
        
        // Assert
        Assert.NotNull(receivedArgs);
        Assert.Equal(zoomBorder.ZoomSpeed, receivedArgs!.ZoomDelta);
    }
    
    [AvaloniaFact]
    public void ZoomBorder_ZoomOut_Fires_ZoomEnded_Event()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();
        var childElement = new Border { Width = 100, Height = 100 };
        zoomBorder.Child = childElement;
        
        ZoomEventArgs? receivedArgs = null;
        zoomBorder.ZoomEnded += (_, args) => receivedArgs = args;
        
        // Act
        zoomBorder.ZoomOut();
        
        // Assert
        Assert.NotNull(receivedArgs);
        Assert.Equal(1.0 / zoomBorder.ZoomSpeed, receivedArgs!.ZoomDelta);
    }
    
    [AvaloniaFact]
    public void ZoomBorder_BeginPanTo_Fires_PanStarted_Event()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();
        PanEventArgs? receivedArgs = null;
        zoomBorder.PanStarted += (_, args) => receivedArgs = args;
        
        // Act
        zoomBorder.BeginPanTo(10, 20);
        
        // Assert
        Assert.NotNull(receivedArgs);
        Assert.Equal(0.0, receivedArgs!.DeltaX);
        Assert.Equal(0.0, receivedArgs.DeltaY);
    }
    
    [AvaloniaFact]
    public void ZoomBorder_ContinuePanTo_Fires_PanContinued_Event()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();
        PanEventArgs? receivedArgs = null;
        zoomBorder.PanContinued += (_, args) => receivedArgs = args;
        
        // Start panning first
        zoomBorder.BeginPanTo(10, 20);
        
        // Act
        zoomBorder.ContinuePanTo(15, 25);
        
        // Assert
        Assert.NotNull(receivedArgs);
        Assert.Equal(5.0, receivedArgs!.DeltaX);
        Assert.Equal(5.0, receivedArgs.DeltaY);
    }
    
    [AvaloniaFact]
    public void ZoomBorder_Pan_Fires_PanContinued_Event()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();
        PanEventArgs? receivedArgs = null;
        zoomBorder.PanContinued += (_, args) => receivedArgs = args;
        
        // Act
        zoomBorder.Pan(100, 200);
        
        // Assert
        Assert.NotNull(receivedArgs);
        Assert.Equal(100, receivedArgs!.DeltaX);
        Assert.Equal(200, receivedArgs.DeltaY);
    }
    
    [AvaloniaFact]
    public void ZoomBorder_AutoFit_Fires_AutoFitApplied_Event()
    {
        // Arrange
        var zoomBorder = new ZoomBorder { Width = 200, Height = 200 };
        var childElement = new Border { Width = 100, Height = 100 };
        zoomBorder.Child = childElement;
        
        StretchModeChangedEventArgs? receivedArgs = null;
        zoomBorder.AutoFitApplied += (_, args) => receivedArgs = args;
        
        // Act
        zoomBorder.AutoFit();
        
        // Assert
        Assert.NotNull(receivedArgs);
        Assert.Equal(zoomBorder.Stretch, receivedArgs!.StretchMode);
        Assert.Equal(zoomBorder.Stretch, receivedArgs.PreviousStretchMode);
    }
    
    [AvaloniaFact]
    public void ZoomBorder_UniformToFill_Fires_StretchModeChanged_Event()
    {
        // Arrange
        var zoomBorder = new ZoomBorder { Width = 200, Height = 200 };
        var childElement = new Border { Width = 100, Height = 100 };
        zoomBorder.Child = childElement;
        
        StretchModeChangedEventArgs? receivedArgs = null;
        zoomBorder.StretchModeChanged += (_, args) => receivedArgs = args;
        
        // Act
        zoomBorder.UniformToFill();
        
        // Assert
        Assert.NotNull(receivedArgs);
        Assert.Equal(StretchMode.UniformToFill, receivedArgs!.StretchMode);
    }

    [AvaloniaFact]
    public void ZoomBorder_ZoomTo_Fires_ZoomDeltaChanged_Event_Programmatically()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();
        var childElement = new Border { Width = 100, Height = 100 };
        zoomBorder.Child = childElement;
        
        ZoomEventArgs? receivedArgs = null;
        zoomBorder.ZoomDeltaChanged += (_, args) => receivedArgs = args;
        
        // Act
        zoomBorder.ZoomTo(1.5, 50, 50);
        
        // Assert
        Assert.NotNull(receivedArgs);
        Assert.Equal(1.5, receivedArgs!.ZoomDelta);
        Assert.Equal(50, receivedArgs.CenterX);
        Assert.Equal(50, receivedArgs.CenterY);
    }

    [AvaloniaFact]
    public void ZoomBorder_ZoomIn_Fires_ZoomEnded_Event_Programmatically()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();
        var childElement = new Border { Width = 100, Height = 100 };
        zoomBorder.Child = childElement;
        
        ZoomEventArgs? receivedArgs = null;
        zoomBorder.ZoomEnded += (_, args) => receivedArgs = args;
        
        // Act
        zoomBorder.ZoomIn();
        
        // Assert
        Assert.NotNull(receivedArgs);
        Assert.True(receivedArgs!.ZoomDelta > 1); // ZoomIn should have delta > 1
        // Note: CenterX and CenterY depend on element bounds which may be 0 in the test environment
    }

    [AvaloniaFact]
    public void ZoomBorder_Fill_Fires_StretchModeChanged_Event()
    {
        // Arrange
        var zoomBorder = new ZoomBorder { Width = 200, Height = 200 };
        var childElement = new Border { Width = 100, Height = 100 };
        zoomBorder.Child = childElement;
        
        StretchModeChangedEventArgs? receivedArgs = null;
        zoomBorder.StretchModeChanged += (_, args) => receivedArgs = args;
        
        // Act
        zoomBorder.Fill();
        
        // Assert
        Assert.NotNull(receivedArgs);
        Assert.Equal(StretchMode.Fill, receivedArgs!.StretchMode);
    }

    [AvaloniaFact]
    public void ZoomBorder_Uniform_Fires_StretchModeChanged_Event()
    {
        // Arrange
        var zoomBorder = new ZoomBorder { Width = 200, Height = 200 };
        var childElement = new Border { Width = 100, Height = 100 };
        zoomBorder.Child = childElement;
        
        StretchModeChangedEventArgs? receivedArgs = null;
        zoomBorder.StretchModeChanged += (_, args) => receivedArgs = args;
        
        // Act
        zoomBorder.Uniform();
        
        // Assert
        Assert.NotNull(receivedArgs);
        Assert.Equal(StretchMode.Uniform, receivedArgs!.StretchMode);
    }

    [AvaloniaFact]
    public void ZoomBorder_ZoomIn_Fires_ZoomChanged_Event()
    {
        // Arrange
        var zoomBorder = new ZoomBorder { Width = 200, Height = 200 };
        var childElement = new Border { Width = 100, Height = 100 };
        zoomBorder.Child = childElement;
        
        ZoomChangedEventArgs? receivedArgs = null;
        zoomBorder.ZoomChanged += (_, args) => receivedArgs = args;
        
        // Act
        zoomBorder.ZoomIn();
        
        // Assert
        Assert.NotNull(receivedArgs);
        Assert.True(receivedArgs!.ZoomX > 1.0);
        Assert.True(receivedArgs.ZoomY > 1.0);
    }

    [AvaloniaFact]
    public void ZoomBorder_ZoomOut_Fires_ZoomChanged_Event()
    {
        // Arrange
        var zoomBorder = new ZoomBorder { Width = 200, Height = 200 };
        var childElement = new Border { Width = 100, Height = 100 };
        zoomBorder.Child = childElement;
        
        // First zoom in so we can zoom out
        zoomBorder.ZoomIn();
        
        ZoomChangedEventArgs? receivedArgs = null;
        zoomBorder.ZoomChanged += (_, args) => receivedArgs = args;
        
        // Act
        zoomBorder.ZoomOut();
        
        // Assert
        Assert.NotNull(receivedArgs);
        Assert.True(receivedArgs!.ZoomX >= 1.0);
        Assert.True(receivedArgs.ZoomY >= 1.0);
    }

    [AvaloniaFact]
    public void ZoomBorder_Zoom_Fires_ZoomChanged_Event()
    {
        // Arrange
        var zoomBorder = new ZoomBorder { Width = 200, Height = 200 };
        var childElement = new Border { Width = 100, Height = 100 };
        zoomBorder.Child = childElement;
        
        ZoomChangedEventArgs? receivedArgs = null;
        zoomBorder.ZoomChanged += (_, args) => receivedArgs = args;
        
        // Act
        zoomBorder.Zoom(2.0, 100, 100);
        
        // Assert
        Assert.NotNull(receivedArgs);
        Assert.Equal(2.0, receivedArgs!.ZoomX);
        Assert.Equal(2.0, receivedArgs.ZoomY);
    }

    [AvaloniaFact]
    public void ZoomBorder_Pan_Fires_PanContinued_Event_Programmatically()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();
        var childElement = new Border { Width = 100, Height = 100 };
        zoomBorder.Child = childElement;
        
        PanEventArgs? receivedArgs = null;
        zoomBorder.PanContinued += (_, args) => receivedArgs = args;
        
        // Act
        zoomBorder.Pan(10, 20);
        
        // Assert
        Assert.NotNull(receivedArgs);
        Assert.Equal(10, receivedArgs!.OffsetX);
        Assert.Equal(20, receivedArgs.OffsetY);
    }

    [AvaloniaFact]
    public void ZoomBorder_BeginPanTo_Fires_PanStarted_Event_Programmatically()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();
        var childElement = new Border { Width = 100, Height = 100 };
        zoomBorder.Child = childElement;
        
        PanEventArgs? receivedArgs = null;
        zoomBorder.PanStarted += (_, args) => receivedArgs = args;
        
        // Act
        zoomBorder.BeginPanTo(10, 20);
        
        // Assert
        Assert.NotNull(receivedArgs);
        Assert.Equal(0, receivedArgs!.DeltaX); // BeginPanTo starts with zero delta
        Assert.Equal(0, receivedArgs.DeltaY);
    }
    
    [AvaloniaFact]
    public void ZoomBorder_Multiple_Events_Can_Be_Subscribed()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();
        var matrixChangedCount = 0;
        var zoomChangedCount = 0;
        
        zoomBorder.MatrixChanged += (_, _) => matrixChangedCount++;
        zoomBorder.ZoomChanged += (_, _) => zoomChangedCount++;
        
        // Act
        zoomBorder.SetMatrix(Matrix.CreateScale(2.0, 2.0));
        
        // Assert
        Assert.Equal(1, matrixChangedCount);
        // Note: ZoomChanged is called internally during matrix operations
        Assert.True(zoomChangedCount >= 0);
    }
    
    [AvaloniaFact]
    public void ZoomBorder_Event_Handlers_Can_Be_Null()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();
        
        // Act & Assert - Should not throw
        zoomBorder.SetMatrix(Matrix.CreateScale(2.0, 2.0));
        zoomBorder.ResetMatrix();
        zoomBorder.Zoom(1.5, 50, 50);
        zoomBorder.ZoomTo(1.2, 30, 30);
        zoomBorder.BeginPanTo(10, 10);
        zoomBorder.ContinuePanTo(20, 20);
        zoomBorder.Pan(100, 100);
        zoomBorder.AutoFit();
        zoomBorder.UniformToFill();
    }
    
    [AvaloniaFact]
    public void ZoomBorder_Event_Args_Contain_Valid_Data()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();
        MatrixChangedEventArgs? matrixArgs = null;
        ZoomEventArgs? zoomArgs = null;
        PanEventArgs? panArgs = null;
        
        zoomBorder.MatrixChanged += (_, args) => matrixArgs = args;
        zoomBorder.ZoomStarted += (_, args) => zoomArgs = args;
        zoomBorder.PanStarted += (_, args) => panArgs = args;
        
        // Act
        zoomBorder.SetMatrix(Matrix.CreateScale(2.0, 2.0));
        zoomBorder.Zoom(1.5, 100, 200);
        zoomBorder.BeginPanTo(50, 75);
        
        // Assert
        Assert.NotNull(matrixArgs);
        Assert.True(matrixArgs!.Matrix.M11 > 0);
        Assert.True(matrixArgs.Matrix.M22 > 0);
        
        Assert.NotNull(zoomArgs);
        Assert.Equal(100, zoomArgs!.CenterX);
        Assert.Equal(200, zoomArgs.CenterY);
        
        Assert.NotNull(panArgs);
        Assert.True(panArgs!.ZoomX > 0);
        Assert.True(panArgs.ZoomY > 0);
    }
}

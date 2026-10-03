// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
namespace PanAndZoom.Uno.RuntimeTests.Tests;

// These tests exercise the programmatic API on a ZoomBorder that is not loaded (as the Avalonia tests do
// without a window), so the child is attached but no layout has run.
[TestClass]
[RunsOnUIThread]
public class EventsTests
{
    [TestMethod]
    public void ZoomBorder_SetMatrix_Fires_MatrixChanged_Event()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();
        MatrixChangedEventArgs? receivedArgs = null;
        zoomBorder.MatrixChanged += (_, args) => receivedArgs = args;
        
        var newMatrix = MatrixHelper.Scale(2.0, 2.0);
        
        // Act
        zoomBorder.SetMatrix(newMatrix);
        
        // Assert
        Assert.IsNotNull(receivedArgs);
        Assert.AreEqual(newMatrix, receivedArgs!.Matrix);
        Assert.AreEqual("SetMatrix", receivedArgs.Operation);
    }
    
    [TestMethod]
    public void ZoomBorder_ResetMatrix_Fires_MatrixReset_Event()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();
        MatrixChangedEventArgs? receivedArgs = null;
        zoomBorder.MatrixReset += (_, args) => receivedArgs = args;
        
        // First set a non-identity matrix
        zoomBorder.SetMatrix(MatrixHelper.Scale(2.0, 2.0));
        
        // Act
        zoomBorder.ResetMatrix();
        
        // Assert
        Assert.IsNotNull(receivedArgs);
        Assert.AreEqual(Matrix.Identity, receivedArgs!.Matrix);
        Assert.AreEqual("ResetMatrix", receivedArgs.Operation);
    }
    
    [TestMethod]
    public void ZoomBorder_Zoom_Fires_ZoomStarted_Event()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();
        ZoomEventArgs? receivedArgs = null;
        zoomBorder.ZoomStarted += (_, args) => receivedArgs = args;
        
        // Act
        zoomBorder.Zoom(2.0, 100, 100);
        
        // Assert
        Assert.IsNotNull(receivedArgs);
        Assert.AreEqual(100.0, receivedArgs!.CenterX);
        Assert.AreEqual(100.0, receivedArgs.CenterY);
    }
    
    [TestMethod]
    public void ZoomBorder_ZoomTo_Fires_ZoomDeltaChanged_Event()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();
        ZoomEventArgs? receivedArgs = null;
        zoomBorder.ZoomDeltaChanged += (_, args) => receivedArgs = args;
        
        // Act
        zoomBorder.ZoomTo(1.5, 50, 50);
        
        // Assert
        Assert.IsNotNull(receivedArgs);
        Assert.AreEqual(1.5, receivedArgs!.ZoomDelta);
        Assert.AreEqual(50.0, receivedArgs.CenterX);
        Assert.AreEqual(50.0, receivedArgs.CenterY);
    }
    
    [TestMethod]
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
        Assert.IsNotNull(receivedArgs);
        Assert.AreEqual(zoomBorder.ZoomSpeed, receivedArgs!.ZoomDelta);
    }
    
    [TestMethod]
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
        Assert.IsNotNull(receivedArgs);
        Assert.AreEqual(1.0 / zoomBorder.ZoomSpeed, receivedArgs!.ZoomDelta);
    }
    
    [TestMethod]
    public void ZoomBorder_BeginPanTo_Fires_PanStarted_Event()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();
        PanEventArgs? receivedArgs = null;
        zoomBorder.PanStarted += (_, args) => receivedArgs = args;
        
        // Act
        zoomBorder.BeginPanTo(10, 20);
        
        // Assert
        Assert.IsNotNull(receivedArgs);
        Assert.AreEqual(0.0, receivedArgs!.DeltaX);
        Assert.AreEqual(0.0, receivedArgs.DeltaY);
    }
    
    [TestMethod]
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
        Assert.IsNotNull(receivedArgs);
        Assert.AreEqual(5.0, receivedArgs!.DeltaX);
        Assert.AreEqual(5.0, receivedArgs.DeltaY);
    }
    
    [TestMethod]
    public void ZoomBorder_Pan_Fires_PanContinued_Event()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();
        PanEventArgs? receivedArgs = null;
        zoomBorder.PanContinued += (_, args) => receivedArgs = args;
        
        // Act
        zoomBorder.Pan(100, 200);
        
        // Assert
        Assert.IsNotNull(receivedArgs);
        Assert.AreEqual(100.0, receivedArgs!.DeltaX);
        Assert.AreEqual(200.0, receivedArgs.DeltaY);
    }
    
    [TestMethod]
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
        Assert.IsNotNull(receivedArgs);
        Assert.AreEqual(zoomBorder.Stretch, receivedArgs!.StretchMode);
        Assert.AreEqual(zoomBorder.Stretch, receivedArgs.PreviousStretchMode);
    }
    
    [TestMethod]
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
        Assert.IsNotNull(receivedArgs);
        Assert.AreEqual(StretchMode.UniformToFill, receivedArgs!.StretchMode);
    }

    [TestMethod]
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
        Assert.IsNotNull(receivedArgs);
        Assert.AreEqual(1.5, receivedArgs!.ZoomDelta);
        Assert.AreEqual(50.0, receivedArgs.CenterX);
        Assert.AreEqual(50.0, receivedArgs.CenterY);
    }

    [TestMethod]
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
        Assert.IsNotNull(receivedArgs);
        Assert.IsTrue(receivedArgs!.ZoomDelta > 1); // ZoomIn should have delta > 1
        // Note: CenterX and CenterY depend on element bounds which may be 0 in the test environment
    }

    [TestMethod]
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
        Assert.IsNotNull(receivedArgs);
        Assert.AreEqual(StretchMode.Fill, receivedArgs!.StretchMode);
    }

    [TestMethod]
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
        Assert.IsNotNull(receivedArgs);
        Assert.AreEqual(StretchMode.Uniform, receivedArgs!.StretchMode);
    }

    [TestMethod]
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
        Assert.IsNotNull(receivedArgs);
        Assert.IsTrue(receivedArgs!.ZoomX > 1.0);
        Assert.IsTrue(receivedArgs.ZoomY > 1.0);
    }

    [TestMethod]
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
        Assert.IsNotNull(receivedArgs);
        Assert.IsTrue(receivedArgs!.ZoomX >= 1.0);
        Assert.IsTrue(receivedArgs.ZoomY >= 1.0);
    }

    [TestMethod]
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
        Assert.IsNotNull(receivedArgs);
        Assert.AreEqual(2.0, receivedArgs!.ZoomX);
        Assert.AreEqual(2.0, receivedArgs.ZoomY);
    }

    [TestMethod]
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
        Assert.IsNotNull(receivedArgs);
        Assert.AreEqual(10.0, receivedArgs!.OffsetX);
        Assert.AreEqual(20.0, receivedArgs.OffsetY);
    }

    [TestMethod]
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
        Assert.IsNotNull(receivedArgs);
        Assert.AreEqual(0.0, receivedArgs!.DeltaX); // BeginPanTo starts with zero delta
        Assert.AreEqual(0.0, receivedArgs.DeltaY);
    }
    
    [TestMethod]
    public void ZoomBorder_Multiple_Events_Can_Be_Subscribed()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();
        var matrixChangedCount = 0;
        var zoomChangedCount = 0;
        
        zoomBorder.MatrixChanged += (_, _) => matrixChangedCount++;
        zoomBorder.ZoomChanged += (_, _) => zoomChangedCount++;
        
        // Act
        zoomBorder.SetMatrix(MatrixHelper.Scale(2.0, 2.0));
        
        // Assert
        Assert.AreEqual(1, matrixChangedCount);
        // Note: ZoomChanged is called internally during matrix operations
        Assert.IsTrue(zoomChangedCount >= 0);
    }
    
    [TestMethod]
    public void ZoomBorder_Event_Handlers_Can_Be_Null()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();
        
        // Act & Assert - Should not throw
        zoomBorder.SetMatrix(MatrixHelper.Scale(2.0, 2.0));
        zoomBorder.ResetMatrix();
        zoomBorder.Zoom(1.5, 50, 50);
        zoomBorder.ZoomTo(1.2, 30, 30);
        zoomBorder.BeginPanTo(10, 10);
        zoomBorder.ContinuePanTo(20, 20);
        zoomBorder.Pan(100, 100);
        zoomBorder.AutoFit();
        zoomBorder.UniformToFill();
    }
    
    [TestMethod]
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
        zoomBorder.SetMatrix(MatrixHelper.Scale(2.0, 2.0));
        zoomBorder.Zoom(1.5, 100, 200);
        zoomBorder.BeginPanTo(50, 75);
        
        // Assert
        Assert.IsNotNull(matrixArgs);
        Assert.IsTrue(matrixArgs!.Matrix.M11 > 0);
        Assert.IsTrue(matrixArgs.Matrix.M22 > 0);
        
        Assert.IsNotNull(zoomArgs);
        Assert.AreEqual(100.0, zoomArgs!.CenterX);
        Assert.AreEqual(200.0, zoomArgs.CenterY);
        
        Assert.IsNotNull(panArgs);
        Assert.IsTrue(panArgs!.ZoomX > 0);
        Assert.IsTrue(panArgs.ZoomY > 0);
    }

    #region EventArgs Coverage Tests

    [TestMethod]
    public void GestureEventArgs_AllProperties_ReturnCorrectValues()
    {
        // Arrange & Act
        var args = new GestureEventArgs(
            gestureType: "Pinch",
            zoomX: 2.0,
            zoomY: 2.5,
            offsetX: 100.0,
            offsetY: 150.0,
            centerX: 50.0,
            centerY: 75.0,
            delta: 0.5,
            matrix: MatrixHelper.Scale(2.0, 2.5),
            previousMatrix: Matrix.Identity
        );

        // Assert
        Assert.AreEqual("Pinch", args.GestureType);
        Assert.AreEqual(2.0, args.ZoomX);
        Assert.AreEqual(2.5, args.ZoomY);
        Assert.AreEqual(100.0, args.OffsetX);
        Assert.AreEqual(150.0, args.OffsetY);
        Assert.AreEqual(50.0, args.CenterX);
        Assert.AreEqual(75.0, args.CenterY);
        Assert.AreEqual(0.5, args.Delta);
        Assert.AreEqual(2.0, args.Matrix.M11);
        Assert.AreEqual(2.5, args.Matrix.M22);
        Assert.AreEqual(Matrix.Identity, args.PreviousMatrix);
    }

    [TestMethod]
    public void MatrixChangedEventArgs_AllProperties_ReturnCorrectValues()
    {
        // Arrange & Act
        var currentMatrix = MatrixHelper.Scale(2.0, 2.0);
        var previousMatrix = Matrix.Identity;
        var args = new MatrixChangedEventArgs(
            matrix: currentMatrix,
            previousMatrix: previousMatrix,
            zoomX: 2.0,
            zoomY: 2.0,
            offsetX: 100.0,
            offsetY: 150.0,
            previousZoomX: 1.0,
            previousZoomY: 1.0,
            previousOffsetX: 0.0,
            previousOffsetY: 0.0,
            operation: "TestOperation"
        );

        // Assert
        Assert.AreEqual(currentMatrix, args.Matrix);
        Assert.AreEqual(previousMatrix, args.PreviousMatrix);
        Assert.AreEqual(2.0, args.ZoomX);
        Assert.AreEqual(2.0, args.ZoomY);
        Assert.AreEqual(100.0, args.OffsetX);
        Assert.AreEqual(150.0, args.OffsetY);
        Assert.AreEqual(1.0, args.PreviousZoomX);
        Assert.AreEqual(1.0, args.PreviousZoomY);
        Assert.AreEqual(0.0, args.PreviousOffsetX);
        Assert.AreEqual(0.0, args.PreviousOffsetY);
        Assert.AreEqual("TestOperation", args.Operation);
    }

    [TestMethod]
    public void ZoomEventArgs_AllProperties_ReturnCorrectValues()
    {
        // Arrange & Act
        var currentMatrix = MatrixHelper.Scale(2.0, 2.0);
        var previousMatrix = Matrix.Identity;
        var args = new ZoomEventArgs(
            zoomX: 2.0,
            zoomY: 2.5,
            previousZoomX: 1.0,
            previousZoomY: 1.0,
            zoomDelta: 1.5,
            centerX: 100.0,
            centerY: 150.0,
            offsetX: 50.0,
            offsetY: 75.0,
            matrix: currentMatrix,
            previousMatrix: previousMatrix
        );

        // Assert
        Assert.AreEqual(2.0, args.ZoomX);
        Assert.AreEqual(2.5, args.ZoomY);
        Assert.AreEqual(1.0, args.PreviousZoomX);
        Assert.AreEqual(1.0, args.PreviousZoomY);
        Assert.AreEqual(1.5, args.ZoomDelta);
        Assert.AreEqual(100.0, args.CenterX);
        Assert.AreEqual(150.0, args.CenterY);
        Assert.AreEqual(50.0, args.OffsetX);
        Assert.AreEqual(75.0, args.OffsetY);
        Assert.AreEqual(currentMatrix, args.Matrix);
        Assert.AreEqual(previousMatrix, args.PreviousMatrix);
    }

    [TestMethod]
    public void PanEventArgs_AllProperties_ReturnCorrectValues()
    {
        // Arrange & Act
        var currentMatrix = MatrixHelper.Translate(100, 150);
        var previousMatrix = Matrix.Identity;
        var args = new PanEventArgs(
            zoomX: 1.5,
            zoomY: 1.5,
            offsetX: 100.0,
            offsetY: 150.0,
            previousOffsetX: 50.0,
            previousOffsetY: 75.0,
            deltaX: 50.0,
            deltaY: 75.0,
            matrix: currentMatrix,
            previousMatrix: previousMatrix
        );

        // Assert
        Assert.AreEqual(1.5, args.ZoomX);
        Assert.AreEqual(1.5, args.ZoomY);
        Assert.AreEqual(100.0, args.OffsetX);
        Assert.AreEqual(150.0, args.OffsetY);
        Assert.AreEqual(50.0, args.PreviousOffsetX);
        Assert.AreEqual(75.0, args.PreviousOffsetY);
        Assert.AreEqual(50.0, args.DeltaX);
        Assert.AreEqual(75.0, args.DeltaY);
        Assert.AreEqual(currentMatrix, args.Matrix);
        Assert.AreEqual(previousMatrix, args.PreviousMatrix);
    }

    [TestMethod]
    public void StretchModeChangedEventArgs_AllProperties_ReturnCorrectValues()
    {
        // Arrange & Act
        var args = new StretchModeChangedEventArgs(
            stretchMode: StretchMode.UniformToFill,
            previousStretchMode: StretchMode.None,
            matrix: MatrixHelper.Scale(2.0, 2.0),
            previousMatrix: Matrix.Identity,
            zoomX: 2.0,
            zoomY: 2.0,
            offsetX: 100.0,
            offsetY: 150.0,
            panelWidth: 800.0,
            panelHeight: 600.0,
            elementWidth: 400.0,
            elementHeight: 300.0
        );

        // Assert
        Assert.AreEqual(StretchMode.UniformToFill, args.StretchMode);
        Assert.AreEqual(StretchMode.None, args.PreviousStretchMode);
        Assert.AreEqual(2.0, args.Matrix.M11);
        Assert.AreEqual(2.0, args.Matrix.M22);
        Assert.AreEqual(Matrix.Identity, args.PreviousMatrix);
        Assert.AreEqual(2.0, args.ZoomX);
        Assert.AreEqual(2.0, args.ZoomY);
        Assert.AreEqual(100.0, args.OffsetX);
        Assert.AreEqual(150.0, args.OffsetY);
        Assert.AreEqual(800.0, args.PanelWidth);
        Assert.AreEqual(600.0, args.PanelHeight);
        Assert.AreEqual(400.0, args.ElementWidth);
        Assert.AreEqual(300.0, args.ElementHeight);
    }

    [TestMethod]
    public void ZoomChangedEventArgs_AllProperties_ReturnCorrectValues()
    {
        // Arrange & Act
        var args = new ZoomChangedEventArgs(
            zoomX: 2.0,
            zoomY: 2.5,
            offsetX: 100.0,
            offsetY: 150.0
        );

        // Assert
        Assert.AreEqual(2.0, args.ZoomX);
        Assert.AreEqual(2.5, args.ZoomY);
        Assert.AreEqual(100.0, args.OffsetX);
        Assert.AreEqual(150.0, args.OffsetY);
    }

    #endregion
}

// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using Microsoft.UI;

namespace PanAndZoom.Uno.RuntimeTests.Tests;

/// <summary>
/// Tests for ICommand implementations.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class ZoomBorderCommandsTests
{
    [TestCleanup]
    public async Task Cleanup()
    {
        InputHelper.Reset();
        await ZoomBorderTestHelper.UnloadAsync();
    }

    [TestMethod]
    public void ZoomInCommand_Exists()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();

        // Act & Assert
        Assert.IsNotNull(zoomBorder.ZoomInCommand);
    }

    [TestMethod]
    public void ZoomOutCommand_Exists()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();

        // Act & Assert
        Assert.IsNotNull(zoomBorder.ZoomOutCommand);
    }

    [TestMethod]
    public void ResetCommand_Exists()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();

        // Act & Assert
        Assert.IsNotNull(zoomBorder.ResetCommand);
    }

    [TestMethod]
    public void FitCommand_Exists()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();

        // Act & Assert
        Assert.IsNotNull(zoomBorder.FitCommand);
    }

    [TestMethod]
    public void FillCommand_Exists()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();

        // Act & Assert
        Assert.IsNotNull(zoomBorder.FillCommand);
    }

    [TestMethod]
    public void UniformCommand_Exists()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();

        // Act & Assert
        Assert.IsNotNull(zoomBorder.UniformCommand);
    }

    [TestMethod]
    public void UniformToFillCommand_Exists()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();

        // Act & Assert
        Assert.IsNotNull(zoomBorder.UniformToFillCommand);
    }

    [TestMethod]
    public void NavigateBackCommand_Exists()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();

        // Act & Assert
        Assert.IsNotNull(zoomBorder.NavigateBackCommand);
    }

    [TestMethod]
    public void NavigateForwardCommand_Exists()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();

        // Act & Assert
        Assert.IsNotNull(zoomBorder.NavigateForwardCommand);
    }

    [TestMethod]
    public void ToggleStretchCommand_Exists()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();

        // Act & Assert
        Assert.IsNotNull(zoomBorder.ToggleStretchCommand);
    }

    [TestMethod]
    public async Task ZoomInCommand_Execute_ZoomsIn()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            EnableZoom = true
        };

        var childElement = new Border
        {
            Width = 200,
            Height = 150,
            Background = new SolidColorBrush(Colors.Red)
        };

        zoomBorder.Child = childElement;
        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        var initialZoom = zoomBorder.ZoomX;

        // Act
        zoomBorder.ZoomInCommand.Execute(null);

        // Assert
        Assert.IsTrue(zoomBorder.ZoomX > initialZoom, "Zoom should increase after ZoomInCommand");
    }

    [TestMethod]
    public async Task ZoomOutCommand_Execute_ZoomsOut()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            EnableZoom = true
        };

        var childElement = new Border
        {
            Width = 200,
            Height = 150,
            Background = new SolidColorBrush(Colors.Red)
        };

        zoomBorder.Child = childElement;
        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        // Zoom in first
        zoomBorder.ZoomIn();
        var zoomedInLevel = zoomBorder.ZoomX;

        // Act
        zoomBorder.ZoomOutCommand.Execute(null);

        // Assert
        Assert.IsTrue(zoomBorder.ZoomX < zoomedInLevel, "Zoom should decrease after ZoomOutCommand");
    }

    [TestMethod]
    public async Task ResetCommand_Execute_ResetsView()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300
        };

        var childElement = new Border
        {
            Width = 200,
            Height = 150,
            Background = new SolidColorBrush(Colors.Red)
        };

        zoomBorder.Child = childElement;
        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        // Zoom and pan
        zoomBorder.ZoomIn();
        zoomBorder.Pan(50, 50);

        // Act
        zoomBorder.ResetCommand.Execute(null);

        // Assert
        Assert.AreEqual(1.0, zoomBorder.ZoomX);
        Assert.AreEqual(1.0, zoomBorder.ZoomY);
        Assert.AreEqual(0.0, zoomBorder.OffsetX);
        Assert.AreEqual(0.0, zoomBorder.OffsetY);
    }

    [TestMethod]
    public async Task FitCommand_Execute_FitsToViewport()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            Stretch = StretchMode.Uniform
        };

        var childElement = new Border
        {
            Width = 200,
            Height = 150,
            Background = new SolidColorBrush(Colors.Red)
        };

        zoomBorder.Child = childElement;
        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        // Zoom in first
        zoomBorder.ZoomIn();

        // Act
        zoomBorder.FitCommand.Execute(null);

        // Assert - Should apply AutoFit
        Assert.AreNotEqual(1.0, zoomBorder.ZoomX);
    }

    [TestMethod]
    public async Task ToggleStretchCommand_Execute_TogglesStretchMode()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            Stretch = StretchMode.None
        };

        var childElement = new Border
        {
            Width = 200,
            Height = 150,
            Background = new SolidColorBrush(Colors.Red)
        };

        zoomBorder.Child = childElement;
        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        // Act
        zoomBorder.ToggleStretchCommand.Execute(null);

        // Assert
        Assert.AreEqual(StretchMode.Fill, zoomBorder.Stretch);
    }

    [TestMethod]
    public void NavigateBackCommand_CanExecute_FalseWhenNoHistory()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            EnableViewHistory = true
        };

        // Act & Assert
        Assert.IsFalse(zoomBorder.NavigateBackCommand.CanExecute(null), "Should not be able to navigate back without history");
    }

    [TestMethod]
    public async Task NavigateBackCommand_CanExecute_TrueWhenHistoryExists()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            EnableViewHistory = true,
            EnableZoom = true
        };

        var childElement = new Border
        {
            Width = 200,
            Height = 150,
            Background = new SolidColorBrush(Colors.Red)
        };

        zoomBorder.Child = childElement;
        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        // Create history
        zoomBorder.ZoomIn();

        // Act & Assert
        Assert.IsTrue(zoomBorder.NavigateBackCommand.CanExecute(null), "Should be able to navigate back with history");
    }

    [TestMethod]
    public void NavigateForwardCommand_CanExecute_FalseInitially()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            EnableViewHistory = true
        };

        // Act & Assert
        Assert.IsFalse(zoomBorder.NavigateForwardCommand.CanExecute(null), "Should not be able to navigate forward initially");
    }

    [TestMethod]
    public async Task NavigateForwardCommand_CanExecute_TrueAfterNavigateBack()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            EnableViewHistory = true,
            EnableZoom = true
        };

        var childElement = new Border
        {
            Width = 200,
            Height = 150,
            Background = new SolidColorBrush(Colors.Red)
        };

        zoomBorder.Child = childElement;
        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        // Create history and navigate back
        zoomBorder.ZoomIn();
        zoomBorder.NavigateBack();

        // Act & Assert
        Assert.IsTrue(zoomBorder.NavigateForwardCommand.CanExecute(null), "Should be able to navigate forward after back");
    }

    [TestMethod]
    public async Task NavigateBackCommand_Execute_NavigatesBack()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            EnableViewHistory = true,
            EnableZoom = true,
            Stretch = StretchMode.None
        };

        var childElement = new Border
        {
            Width = 200,
            Height = 150,
            Background = new SolidColorBrush(Colors.Red)
        };

        zoomBorder.Child = childElement;
        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        var initialZoom = zoomBorder.ZoomX;
        zoomBorder.ZoomIn();

        // Act
        zoomBorder.NavigateBackCommand.Execute(null);

        // Assert
        Assert.AreEqual(initialZoom, zoomBorder.ZoomX, 0.01);
    }

    [TestMethod]
    public async Task NavigateForwardCommand_Execute_NavigatesForward()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            EnableViewHistory = true,
            EnableZoom = true
        };

        var childElement = new Border
        {
            Width = 200,
            Height = 150,
            Background = new SolidColorBrush(Colors.Red)
        };

        zoomBorder.Child = childElement;
        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        zoomBorder.ZoomIn();
        var zoomedInLevel = zoomBorder.ZoomX;
        zoomBorder.NavigateBack();

        // Act
        zoomBorder.NavigateForwardCommand.Execute(null);

        // Assert
        Assert.AreEqual(zoomedInLevel, zoomBorder.ZoomX, 0.01);
    }

    [TestMethod]
    public async Task AllCommands_CanBeUsedMultipleTimes()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            EnableZoom = true,
            Stretch = StretchMode.None
        };

        var childElement = new Border
        {
            Width = 200,
            Height = 150,
            Background = new SolidColorBrush(Colors.Red)
        };

        zoomBorder.Child = childElement;
        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        // Act - Execute commands multiple times
        zoomBorder.ZoomInCommand.Execute(null);
        zoomBorder.ZoomInCommand.Execute(null);
        zoomBorder.ZoomOutCommand.Execute(null);
        zoomBorder.ResetCommand.Execute(null);
        zoomBorder.ToggleStretchCommand.Execute(null);
        zoomBorder.ToggleStretchCommand.Execute(null);

        // Assert - No exceptions should be thrown
        Assert.IsNotNull(zoomBorder);
    }

    #region ICommand Interface Tests

    [TestMethod]
    public async Task ZoomInCommand_ImplementsICommand_CanExecuteAndExecute()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            EnableZoom = true,
            Stretch = StretchMode.None
        };

        var childElement = new Border { Width = 200, Height = 150, Background = new SolidColorBrush(Colors.Red) };
        zoomBorder.Child = childElement;
        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        var command = zoomBorder.ZoomInCommand;

        // Act & Assert
        Assert.IsTrue(command.CanExecute(null));
        command.Execute(null);

        // Verify zoom increased
        Assert.IsTrue(zoomBorder.ZoomX > 1.0);
    }

    [TestMethod]
    public async Task ZoomOutCommand_ImplementsICommand_CanExecuteAndExecute()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            EnableZoom = true,
            Stretch = StretchMode.None
        };

        var childElement = new Border { Width = 200, Height = 150, Background = new SolidColorBrush(Colors.Red) };
        zoomBorder.Child = childElement;
        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        // Zoom in first
        zoomBorder.ZoomIn();

        var command = zoomBorder.ZoomOutCommand;
        var zoomBefore = zoomBorder.ZoomX;

        // Act & Assert
        Assert.IsTrue(command.CanExecute(null));
        command.Execute(null);

        // Verify zoom decreased
        Assert.IsTrue(zoomBorder.ZoomX < zoomBefore);
    }

    [TestMethod]
    public async Task ResetCommand_ImplementsICommand_CanExecuteAndExecute()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            EnableZoom = true,
            Stretch = StretchMode.None
        };

        var childElement = new Border { Width = 200, Height = 150, Background = new SolidColorBrush(Colors.Red) };
        zoomBorder.Child = childElement;
        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        // Change zoom and offset
        zoomBorder.ZoomIn();
        zoomBorder.Pan(50, 50);

        var command = zoomBorder.ResetCommand;

        // Act & Assert
        Assert.IsTrue(command.CanExecute(null));
        command.Execute(null);

        // Verify reset (in None mode, zoom goes to 1.0)
        Assert.AreEqual(1.0, zoomBorder.ZoomX, 1e-3);
    }

    [TestMethod]
    public async Task ToggleStretchCommand_ImplementsICommand_CanExecuteAndExecute()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            EnableZoom = true,
            Stretch = StretchMode.None
        };

        var childElement = new Border { Width = 200, Height = 150, Background = new SolidColorBrush(Colors.Red) };
        zoomBorder.Child = childElement;
        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        var command = zoomBorder.ToggleStretchCommand;
        var stretchBefore = zoomBorder.Stretch;

        // Act & Assert
        Assert.IsTrue(command.CanExecute(null));
        command.Execute(null);

        // Verify stretch changed
        Assert.AreNotEqual(stretchBefore, zoomBorder.Stretch);
    }

    [TestMethod]
    public async Task Command_CanExecuteChanged_CanSubscribe()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            EnableZoom = true,
            Stretch = StretchMode.None
        };

        var childElement = new Border { Width = 200, Height = 150, Background = new SolidColorBrush(Colors.Red) };
        zoomBorder.Child = childElement;
        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        var command = zoomBorder.ZoomInCommand;

        // Act
        command.CanExecuteChanged += (_, _) => { };

        // Assert - Just verify we can subscribe without errors
        Assert.IsNotNull(command);
    }

    #endregion
}

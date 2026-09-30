// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using System.Reflection;
using System.Windows.Input;

namespace PanAndZoom.Uno.RuntimeTests.Tests;

/// <summary>
/// Unit tests for the shared ZoomBorderCommand class (PanAndZoom.Core) via reflection and ZoomBorder commands.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class ZoomBorderCommandUnitTests
{
    [TestCleanup]
    public async Task Cleanup()
    {
        InputHelper.Reset();
        await ZoomBorderTestHelper.UnloadAsync();
    }

    // Helper to get internal ZoomBorderCommand type
    private static Type GetZoomBorderCommandType()
    {
        return typeof(global::PanAndZoom.Core.ZoomBorderCommand);
    }

    // Helper to create ZoomBorderCommand instance via reflection
    private static ICommand CreateZoomBorderCommand(Action execute, Func<bool>? canExecute = null)
    {
        var type = GetZoomBorderCommandType();
        var constructor = type.GetConstructor(new[] { typeof(Action), typeof(Func<bool>) });
        return (ICommand)constructor!.Invoke(new object?[] { execute, canExecute });
    }

    [TestMethod]
    public void ZoomBorderCommand_Constructor_ThrowsOnNullExecute()
    {
        // Arrange & Act & Assert
        var type = GetZoomBorderCommandType();
        var constructor = type.GetConstructor(new[] { typeof(Action), typeof(Func<bool>) });

        var ex = Assert.Throws<TargetInvocationException>(() =>
            constructor!.Invoke(new object?[] { null, null }));
        Assert.IsInstanceOfType(ex.InnerException, typeof(ArgumentNullException));
    }

    [TestMethod]
    public void ZoomBorderCommand_CanExecute_WithCanExecuteFunc_ReturnsTrue()
    {
        // Arrange
        var command = CreateZoomBorderCommand(() => { }, () => true);

        // Act
        var result = command.CanExecute(null);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ZoomBorderCommand_CanExecute_WithCanExecuteFuncReturnsFalse_ReturnsFalse()
    {
        // Arrange
        var command = CreateZoomBorderCommand(() => { }, () => false);

        // Act
        var result = command.CanExecute(null);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ZoomBorderCommand_CanExecute_WithNullCanExecuteFunc_ReturnsTrue()
    {
        // Arrange
        var command = CreateZoomBorderCommand(() => { }, null);

        // Act
        var result = command.CanExecute(null);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ZoomBorderCommand_Execute_InvokesAction()
    {
        // Arrange
        var executed = false;
        var command = CreateZoomBorderCommand(() => executed = true, null);

        // Act
        command.Execute(null);

        // Assert
        Assert.IsTrue(executed);
    }

    [TestMethod]
    public void ZoomBorderCommand_RaiseCanExecuteChanged_RaisesEvent()
    {
        // Arrange
        var command = CreateZoomBorderCommand(() => { }, () => true);
        var eventRaised = false;
        command.CanExecuteChanged += (s, e) => eventRaised = true;

        // Act - Call RaiseCanExecuteChanged via reflection
        var type = GetZoomBorderCommandType();
        var method = type.GetMethod("RaiseCanExecuteChanged");
        method!.Invoke(command, null);

        // Assert
        Assert.IsTrue(eventRaised);
    }

    [TestMethod]
    public void ZoomBorderCommand_RaiseCanExecuteChanged_WithNoHandlers_DoesNotThrow()
    {
        // Arrange
        var command = CreateZoomBorderCommand(() => { }, () => true);

        // Act & Assert - Should not throw even with no handlers subscribed
        var type = GetZoomBorderCommandType();
        var method = type.GetMethod("RaiseCanExecuteChanged");
        var exception = ApiTestHelpers.RecordException(() => method!.Invoke(command, null));
        Assert.IsNull(exception);
    }

    [TestMethod]
    public void ZoomBorderCommand_CanExecute_DynamicCanExecute_ReflectsChanges()
    {
        // Arrange
        var canExecuteValue = true;
        var command = CreateZoomBorderCommand(() => { }, () => canExecuteValue);

        // Act & Assert - Initial state
        Assert.IsTrue(command.CanExecute(null));

        // Change the value
        canExecuteValue = false;
        Assert.IsFalse(command.CanExecute(null));

        // Change back
        canExecuteValue = true;
        Assert.IsTrue(command.CanExecute(null));
    }

    // ===== ZoomBorder Command Tests via Public API =====

    [TestMethod]
    public async Task ZoomInCommand_CanExecute_WithChild_ReturnsTrue()
    {
        // Arrange
        var canvas = new Canvas { Width = 200, Height = 200 };
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            EnableZoom = true,
            Child = canvas
        };
        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        // Act
        var result = zoomBorder.ZoomInCommand.CanExecute(null);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task ZoomInCommand_CanExecute_WithoutChild_ReturnsFalse()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            EnableZoom = true
            // No child
        };
        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        // Act
        var result = zoomBorder.ZoomInCommand.CanExecute(null);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task ZoomInCommand_CanExecute_WhenZoomDisabled_ReturnsFalse()
    {
        // Arrange
        var canvas = new Canvas { Width = 200, Height = 200 };
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            EnableZoom = false,
            Child = canvas
        };
        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        // Act
        var result = zoomBorder.ZoomInCommand.CanExecute(null);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task ZoomOutCommand_CanExecute_WithChild_ReturnsTrue()
    {
        // Arrange
        var canvas = new Canvas { Width = 200, Height = 200 };
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            EnableZoom = true,
            Child = canvas
        };
        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        // Act
        var result = zoomBorder.ZoomOutCommand.CanExecute(null);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task ResetCommand_CanExecute_WithChild_ReturnsTrue()
    {
        // Arrange
        var canvas = new Canvas { Width = 200, Height = 200 };
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            Child = canvas
        };
        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        // Act
        var result = zoomBorder.ResetCommand.CanExecute(null);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task FitCommand_CanExecute_WithChild_ReturnsTrue()
    {
        // Arrange
        var canvas = new Canvas { Width = 200, Height = 200 };
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            Child = canvas
        };
        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        // Act
        var result = zoomBorder.FitCommand.CanExecute(null);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task FillCommand_CanExecute_WithChild_ReturnsTrue()
    {
        // Arrange
        var canvas = new Canvas { Width = 200, Height = 200 };
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            Child = canvas
        };
        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        // Act
        var result = zoomBorder.FillCommand.CanExecute(null);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task ToggleStretchCommand_CanExecute_WithChild_ReturnsTrue()
    {
        // Arrange
        var canvas = new Canvas { Width = 200, Height = 200 };
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            Child = canvas
        };
        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        // Act
        var result = zoomBorder.ToggleStretchCommand.CanExecute(null);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task ZoomInCommand_CanExecuteChanged_RaisedWhenChildAdded()
    {
        // Arrange - This tests the fix for GitHub issue #126
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            EnableZoom = true
        };
        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        // Subscribe to CanExecuteChanged
        var canExecuteChangedRaised = false;
        zoomBorder.ZoomInCommand.CanExecuteChanged += (s, e) => canExecuteChangedRaised = true;

        // Initially should return false (no child)
        Assert.IsFalse(zoomBorder.ZoomInCommand.CanExecute(null));

        // Act - Add a child
        zoomBorder.Child = new Canvas { Width = 200, Height = 200 };

        // Assert - CanExecuteChanged should have been raised and CanExecute should now return true
        Assert.IsTrue(canExecuteChangedRaised, "CanExecuteChanged should be raised when child is added");
        Assert.IsTrue(zoomBorder.ZoomInCommand.CanExecute(null), "CanExecute should return true after child is added");
    }

    [TestMethod]
    public async Task ZoomInCommand_CanExecuteChanged_RaisedWhenChildRemoved()
    {
        // Arrange
        var canvas = new Canvas { Width = 200, Height = 200 };
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            EnableZoom = true,
            Child = canvas
        };
        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        // Initially should return true (has child)
        Assert.IsTrue(zoomBorder.ZoomInCommand.CanExecute(null));

        // Subscribe to CanExecuteChanged
        var canExecuteChangedRaised = false;
        zoomBorder.ZoomInCommand.CanExecuteChanged += (s, e) => canExecuteChangedRaised = true;

        // Act - Remove the child
        zoomBorder.Child = null;

        // Assert - CanExecuteChanged should have been raised and CanExecute should now return false
        Assert.IsTrue(canExecuteChangedRaised, "CanExecuteChanged should be raised when child is removed");
        Assert.IsFalse(zoomBorder.ZoomInCommand.CanExecute(null), "CanExecute should return false after child is removed");
    }

    [TestMethod]
    public async Task FitCommand_CanExecuteChanged_RaisedWhenChildAdded()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300
        };
        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        // Subscribe to CanExecuteChanged
        var canExecuteChangedRaised = false;
        zoomBorder.FitCommand.CanExecuteChanged += (s, e) => canExecuteChangedRaised = true;

        // Initially should return false (no child)
        Assert.IsFalse(zoomBorder.FitCommand.CanExecute(null));

        // Act - Add a child
        zoomBorder.Child = new Canvas { Width = 200, Height = 200 };

        // Assert
        Assert.IsTrue(canExecuteChangedRaised, "CanExecuteChanged should be raised when child is added");
        Assert.IsTrue(zoomBorder.FitCommand.CanExecute(null), "CanExecute should return true after child is added");
    }

    [TestMethod]
    public async Task AllCommands_CanExecuteChanged_RaisedWhenChildAdded()
    {
        // Arrange - This tests all commands receive CanExecuteChanged notification
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            EnableZoom = true
        };
        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        // Track CanExecuteChanged events for all commands
        var commandsNotified = new Dictionary<string, bool>
        {
            { "ZoomIn", false },
            { "ZoomOut", false },
            { "Reset", false },
            { "Fit", false },
            { "Fill", false },
            { "Uniform", false },
            { "UniformToFill", false },
            { "ToggleStretch", false }
        };

        zoomBorder.ZoomInCommand.CanExecuteChanged += (s, e) => commandsNotified["ZoomIn"] = true;
        zoomBorder.ZoomOutCommand.CanExecuteChanged += (s, e) => commandsNotified["ZoomOut"] = true;
        zoomBorder.ResetCommand.CanExecuteChanged += (s, e) => commandsNotified["Reset"] = true;
        zoomBorder.FitCommand.CanExecuteChanged += (s, e) => commandsNotified["Fit"] = true;
        zoomBorder.FillCommand.CanExecuteChanged += (s, e) => commandsNotified["Fill"] = true;
        zoomBorder.UniformCommand.CanExecuteChanged += (s, e) => commandsNotified["Uniform"] = true;
        zoomBorder.UniformToFillCommand.CanExecuteChanged += (s, e) => commandsNotified["UniformToFill"] = true;
        zoomBorder.ToggleStretchCommand.CanExecuteChanged += (s, e) => commandsNotified["ToggleStretch"] = true;

        // Act - Add a child
        zoomBorder.Child = new Canvas { Width = 200, Height = 200 };

        // Assert - All commands should have been notified
        foreach (var kvp in commandsNotified)
        {
            Assert.IsTrue(kvp.Value, $"{kvp.Key}Command should have raised CanExecuteChanged when child was added");
        }
    }

    [TestMethod]
    public async Task NavigateBackCommand_CanExecuteChanged_RaisedWhenViewHistoryChanges()
    {
        // Arrange - This tests the fix for navigation commands not updating
        var canvas = new Canvas { Width = 200, Height = 200 };
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            EnableViewHistory = true,
            Child = canvas
        };
        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        // After showing, there's one initial state in history, so we can't go back yet
        // We need at least 2 states to navigate back (current + one more)
        var initialCanNavigateBack = zoomBorder.NavigateBackCommand.CanExecute(null);

        // Subscribe to CanExecuteChanged
        var canExecuteChangedCount = 0;
        zoomBorder.NavigateBackCommand.CanExecuteChanged += (s, e) => canExecuteChangedCount++;

        // Act - Make a zoom change which adds to history
        zoomBorder.ZoomIn();

        // Assert - CanExecuteChanged should have been raised
        Assert.IsTrue(canExecuteChangedCount > 0, "CanExecuteChanged should be raised when view history changes");
        // After adding a second state, we can now navigate back
        Assert.IsTrue(zoomBorder.NavigateBackCommand.CanExecute(null), "CanExecute should return true after second history entry is added");
    }

    [TestMethod]
    public async Task NavigateForwardCommand_CanExecuteChanged_RaisedAfterNavigateBack()
    {
        // Arrange
        var canvas = new Canvas { Width = 200, Height = 200 };
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            EnableViewHistory = true,
            Child = canvas
        };
        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        // Create some history
        zoomBorder.ZoomIn();
        zoomBorder.ZoomIn();

        // Initially should not be able to navigate forward (at end of history)
        Assert.IsFalse(zoomBorder.NavigateForwardCommand.CanExecute(null));
        Assert.IsTrue(zoomBorder.NavigateBackCommand.CanExecute(null));

        // Subscribe to CanExecuteChanged
        var forwardChangedCount = 0;
        zoomBorder.NavigateForwardCommand.CanExecuteChanged += (s, e) => forwardChangedCount++;

        // Act - Navigate back
        zoomBorder.NavigateBack();

        // Assert - Forward command should now be enabled
        Assert.IsTrue(forwardChangedCount > 0, "CanExecuteChanged should be raised on NavigateForwardCommand after NavigateBack");
        Assert.IsTrue(zoomBorder.NavigateForwardCommand.CanExecute(null), "NavigateForwardCommand.CanExecute should return true after navigating back");
    }

    [TestMethod]
    public async Task NavigationCommands_CanExecuteChanged_RaisedWhenHistoryCleared()
    {
        // Arrange
        var canvas = new Canvas { Width = 200, Height = 200 };
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            EnableViewHistory = true,
            Child = canvas
        };
        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        // Create some history
        zoomBorder.ZoomIn();
        zoomBorder.ZoomIn();

        Assert.IsTrue(zoomBorder.NavigateBackCommand.CanExecute(null));

        // Subscribe to CanExecuteChanged
        var backChangedCount = 0;
        zoomBorder.NavigateBackCommand.CanExecuteChanged += (s, e) => backChangedCount++;

        // Act - Clear history
        zoomBorder.ClearViewHistory();

        // Assert - Back command should now be disabled
        Assert.IsTrue(backChangedCount > 0, "CanExecuteChanged should be raised when history is cleared");
        Assert.IsFalse(zoomBorder.NavigateBackCommand.CanExecute(null), "NavigateBackCommand.CanExecute should return false after clearing history");
    }
}

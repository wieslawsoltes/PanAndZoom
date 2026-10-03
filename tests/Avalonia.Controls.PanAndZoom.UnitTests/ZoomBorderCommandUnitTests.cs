// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using System;
using System.Reflection;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Xunit;

namespace Avalonia.Controls.PanAndZoom.UnitTests;

/// <summary>
/// Unit tests for the shared ZoomBorderCommand class (PanAndZoom.Core) via reflection and ZoomBorder commands.
/// </summary>
public class ZoomBorderCommandUnitTests
{
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

    [AvaloniaFact]
    public void ZoomBorderCommand_Constructor_ThrowsOnNullExecute()
    {
        // Arrange & Act & Assert
        var type = GetZoomBorderCommandType();
        var constructor = type.GetConstructor(new[] { typeof(Action), typeof(Func<bool>) });
        
        var ex = Assert.Throws<TargetInvocationException>(() => 
            constructor!.Invoke(new object?[] { null, null }));
        Assert.IsType<ArgumentNullException>(ex.InnerException);
    }

    [AvaloniaFact]
    public void ZoomBorderCommand_CanExecute_WithCanExecuteFunc_ReturnsTrue()
    {
        // Arrange
        var command = CreateZoomBorderCommand(() => { }, () => true);

        // Act
        var result = command.CanExecute(null);

        // Assert
        Assert.True(result);
    }

    [AvaloniaFact]
    public void ZoomBorderCommand_CanExecute_WithCanExecuteFuncReturnsFalse_ReturnsFalse()
    {
        // Arrange
        var command = CreateZoomBorderCommand(() => { }, () => false);

        // Act
        var result = command.CanExecute(null);

        // Assert
        Assert.False(result);
    }

    [AvaloniaFact]
    public void ZoomBorderCommand_CanExecute_WithNullCanExecuteFunc_ReturnsTrue()
    {
        // Arrange
        var command = CreateZoomBorderCommand(() => { }, null);

        // Act
        var result = command.CanExecute(null);

        // Assert
        Assert.True(result);
    }

    [AvaloniaFact]
    public void ZoomBorderCommand_Execute_InvokesAction()
    {
        // Arrange
        var executed = false;
        var command = CreateZoomBorderCommand(() => executed = true, null);

        // Act
        command.Execute(null);

        // Assert
        Assert.True(executed);
    }

    [AvaloniaFact]
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
        Assert.True(eventRaised);
    }

    [AvaloniaFact]
    public void ZoomBorderCommand_RaiseCanExecuteChanged_WithNoHandlers_DoesNotThrow()
    {
        // Arrange
        var command = CreateZoomBorderCommand(() => { }, () => true);

        // Act & Assert - Should not throw even with no handlers subscribed
        var type = GetZoomBorderCommandType();
        var method = type.GetMethod("RaiseCanExecuteChanged");
        var exception = Record.Exception(() => method!.Invoke(command, null));
        Assert.Null(exception);
    }

    [AvaloniaFact]
    public void ZoomBorderCommand_CanExecute_DynamicCanExecute_ReflectsChanges()
    {
        // Arrange
        var canExecuteValue = true;
        var command = CreateZoomBorderCommand(() => { }, () => canExecuteValue);

        // Act & Assert - Initial state
        Assert.True(command.CanExecute(null));

        // Change the value
        canExecuteValue = false;
        Assert.False(command.CanExecute(null));

        // Change back
        canExecuteValue = true;
        Assert.True(command.CanExecute(null));
    }

    // ===== ZoomBorder Command Tests via Public API =====

    [AvaloniaFact]
    public void ZoomInCommand_CanExecute_WithChild_ReturnsTrue()
    {
        // Arrange
        var canvas = new Canvas { Width = 200, Height = 200 };
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableZoom = true,
            Child = canvas
        };
        var window = new Window { Content = zoomBorder };
        window.Show();

        // Act
        var result = zoomBorder.ZoomInCommand.CanExecute(null);

        // Assert
        Assert.True(result);
    }

    [AvaloniaFact]
    public void ZoomInCommand_CanExecute_WithoutChild_ReturnsFalse()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableZoom = true
            // No child
        };
        var window = new Window { Content = zoomBorder };
        window.Show();

        // Act
        var result = zoomBorder.ZoomInCommand.CanExecute(null);

        // Assert
        Assert.False(result);
    }

    [AvaloniaFact]
    public void ZoomInCommand_CanExecute_WhenZoomDisabled_ReturnsFalse()
    {
        // Arrange
        var canvas = new Canvas { Width = 200, Height = 200 };
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableZoom = false,
            Child = canvas
        };
        var window = new Window { Content = zoomBorder };
        window.Show();

        // Act
        var result = zoomBorder.ZoomInCommand.CanExecute(null);

        // Assert
        Assert.False(result);
    }

    [AvaloniaFact]
    public void ZoomOutCommand_CanExecute_WithChild_ReturnsTrue()
    {
        // Arrange
        var canvas = new Canvas { Width = 200, Height = 200 };
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableZoom = true,
            Child = canvas
        };
        var window = new Window { Content = zoomBorder };
        window.Show();

        // Act
        var result = zoomBorder.ZoomOutCommand.CanExecute(null);

        // Assert
        Assert.True(result);
    }

    [AvaloniaFact]
    public void ResetCommand_CanExecute_WithChild_ReturnsTrue()
    {
        // Arrange
        var canvas = new Canvas { Width = 200, Height = 200 };
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            Child = canvas
        };
        var window = new Window { Content = zoomBorder };
        window.Show();

        // Act
        var result = zoomBorder.ResetCommand.CanExecute(null);

        // Assert
        Assert.True(result);
    }

    [AvaloniaFact]
    public void FitCommand_CanExecute_WithChild_ReturnsTrue()
    {
        // Arrange
        var canvas = new Canvas { Width = 200, Height = 200 };
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            Child = canvas
        };
        var window = new Window { Content = zoomBorder };
        window.Show();

        // Act
        var result = zoomBorder.FitCommand.CanExecute(null);

        // Assert
        Assert.True(result);
    }

    [AvaloniaFact]
    public void FillCommand_CanExecute_WithChild_ReturnsTrue()
    {
        // Arrange
        var canvas = new Canvas { Width = 200, Height = 200 };
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            Child = canvas
        };
        var window = new Window { Content = zoomBorder };
        window.Show();

        // Act
        var result = zoomBorder.FillCommand.CanExecute(null);

        // Assert
        Assert.True(result);
    }

    [AvaloniaFact]
    public void ToggleStretchCommand_CanExecute_WithChild_ReturnsTrue()
    {
        // Arrange
        var canvas = new Canvas { Width = 200, Height = 200 };
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            Child = canvas
        };
        var window = new Window { Content = zoomBorder };
        window.Show();

        // Act
        var result = zoomBorder.ToggleStretchCommand.CanExecute(null);

        // Assert
        Assert.True(result);
    }

    [AvaloniaFact]
    public void ZoomInCommand_CanExecuteChanged_RaisedWhenChildAdded()
    {
        // Arrange - This tests the fix for GitHub issue #126
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableZoom = true
        };
        var window = new Window { Content = zoomBorder };
        window.Show();

        // Subscribe to CanExecuteChanged
        var canExecuteChangedRaised = false;
        zoomBorder.ZoomInCommand.CanExecuteChanged += (s, e) => canExecuteChangedRaised = true;

        // Initially should return false (no child)
        Assert.False(zoomBorder.ZoomInCommand.CanExecute(null));

        // Act - Add a child
        zoomBorder.Child = new Canvas { Width = 200, Height = 200 };

        // Assert - CanExecuteChanged should have been raised and CanExecute should now return true
        Assert.True(canExecuteChangedRaised, "CanExecuteChanged should be raised when child is added");
        Assert.True(zoomBorder.ZoomInCommand.CanExecute(null), "CanExecute should return true after child is added");
    }

    [AvaloniaFact]
    public void ZoomInCommand_CanExecuteChanged_RaisedWhenChildRemoved()
    {
        // Arrange
        var canvas = new Canvas { Width = 200, Height = 200 };
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableZoom = true,
            Child = canvas
        };
        var window = new Window { Content = zoomBorder };
        window.Show();

        // Initially should return true (has child)
        Assert.True(zoomBorder.ZoomInCommand.CanExecute(null));

        // Subscribe to CanExecuteChanged
        var canExecuteChangedRaised = false;
        zoomBorder.ZoomInCommand.CanExecuteChanged += (s, e) => canExecuteChangedRaised = true;

        // Act - Remove the child
        zoomBorder.Child = null;

        // Assert - CanExecuteChanged should have been raised and CanExecute should now return false
        Assert.True(canExecuteChangedRaised, "CanExecuteChanged should be raised when child is removed");
        Assert.False(zoomBorder.ZoomInCommand.CanExecute(null), "CanExecute should return false after child is removed");
    }

    [AvaloniaFact]
    public void FitCommand_CanExecuteChanged_RaisedWhenChildAdded()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300
        };
        var window = new Window { Content = zoomBorder };
        window.Show();

        // Subscribe to CanExecuteChanged
        var canExecuteChangedRaised = false;
        zoomBorder.FitCommand.CanExecuteChanged += (s, e) => canExecuteChangedRaised = true;

        // Initially should return false (no child)
        Assert.False(zoomBorder.FitCommand.CanExecute(null));

        // Act - Add a child
        zoomBorder.Child = new Canvas { Width = 200, Height = 200 };

        // Assert
        Assert.True(canExecuteChangedRaised, "CanExecuteChanged should be raised when child is added");
        Assert.True(zoomBorder.FitCommand.CanExecute(null), "CanExecute should return true after child is added");
    }

    [AvaloniaFact]
    public void AllCommands_CanExecuteChanged_RaisedWhenChildAdded()
    {
        // Arrange - This tests all commands receive CanExecuteChanged notification
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableZoom = true
        };
        var window = new Window { Content = zoomBorder };
        window.Show();

        // Track CanExecuteChanged events for all commands
        var commandsNotified = new System.Collections.Generic.Dictionary<string, bool>
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
            Assert.True(kvp.Value, $"{kvp.Key}Command should have raised CanExecuteChanged when child was added");
        }
    }

    [AvaloniaFact]
    public void NavigateBackCommand_CanExecuteChanged_RaisedWhenViewHistoryChanges()
    {
        // Arrange - This tests the fix for navigation commands not updating
        var canvas = new Canvas { Width = 200, Height = 200 };
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableViewHistory = true,
            Child = canvas
        };
        var window = new Window { Content = zoomBorder };
        window.Show();

        // After showing, there's one initial state in history, so we can't go back yet
        // We need at least 2 states to navigate back (current + one more)
        var initialCanNavigateBack = zoomBorder.NavigateBackCommand.CanExecute(null);

        // Subscribe to CanExecuteChanged
        var canExecuteChangedCount = 0;
        zoomBorder.NavigateBackCommand.CanExecuteChanged += (s, e) => canExecuteChangedCount++;

        // Act - Make a zoom change which adds to history
        zoomBorder.ZoomIn();
        
        // Assert - CanExecuteChanged should have been raised
        Assert.True(canExecuteChangedCount > 0, "CanExecuteChanged should be raised when view history changes");
        // After adding a second state, we can now navigate back
        Assert.True(zoomBorder.NavigateBackCommand.CanExecute(null), "CanExecute should return true after second history entry is added");
    }

    [AvaloniaFact]
    public void NavigateForwardCommand_CanExecuteChanged_RaisedAfterNavigateBack()
    {
        // Arrange
        var canvas = new Canvas { Width = 200, Height = 200 };
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableViewHistory = true,
            Child = canvas
        };
        var window = new Window { Content = zoomBorder };
        window.Show();

        // Create some history
        zoomBorder.ZoomIn();
        zoomBorder.ZoomIn();

        // Initially should not be able to navigate forward (at end of history)
        Assert.False(zoomBorder.NavigateForwardCommand.CanExecute(null));
        Assert.True(zoomBorder.NavigateBackCommand.CanExecute(null));

        // Subscribe to CanExecuteChanged
        var forwardChangedCount = 0;
        zoomBorder.NavigateForwardCommand.CanExecuteChanged += (s, e) => forwardChangedCount++;

        // Act - Navigate back
        zoomBorder.NavigateBack();

        // Assert - Forward command should now be enabled
        Assert.True(forwardChangedCount > 0, "CanExecuteChanged should be raised on NavigateForwardCommand after NavigateBack");
        Assert.True(zoomBorder.NavigateForwardCommand.CanExecute(null), "NavigateForwardCommand.CanExecute should return true after navigating back");
    }

    [AvaloniaFact]
    public void NavigationCommands_CanExecuteChanged_RaisedWhenHistoryCleared()
    {
        // Arrange
        var canvas = new Canvas { Width = 200, Height = 200 };
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            EnableViewHistory = true,
            Child = canvas
        };
        var window = new Window { Content = zoomBorder };
        window.Show();

        // Create some history
        zoomBorder.ZoomIn();
        zoomBorder.ZoomIn();

        Assert.True(zoomBorder.NavigateBackCommand.CanExecute(null));

        // Subscribe to CanExecuteChanged
        var backChangedCount = 0;
        zoomBorder.NavigateBackCommand.CanExecuteChanged += (s, e) => backChangedCount++;

        // Act - Clear history
        zoomBorder.ClearViewHistory();

        // Assert - Back command should now be disabled
        Assert.True(backChangedCount > 0, "CanExecuteChanged should be raised when history is cleared");
        Assert.False(zoomBorder.NavigateBackCommand.CanExecute(null), "NavigateBackCommand.CanExecute should return false after clearing history");
    }
}

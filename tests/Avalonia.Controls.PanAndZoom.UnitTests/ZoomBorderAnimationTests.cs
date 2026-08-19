// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using System;
using System.Linq;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Xunit;

namespace Avalonia.Controls.PanAndZoom.UnitTests;

/// <summary>
/// Tests for animation functionality.
/// </summary>
public class ZoomBorderAnimationTests
{
    [AvaloniaFact]
    public void EnableAnimations_DefaultValue_IsFalse()
    {
        // Arrange & Act
        var zoomBorder = new ZoomBorder();

        // Assert
        Assert.False(zoomBorder.EnableAnimations);
    }

    [AvaloniaFact]
    public void EnableAnimations_CanBeSetToTrue()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();

        // Act
        zoomBorder.EnableAnimations = true;

        // Assert
        Assert.True(zoomBorder.EnableAnimations);
    }

    [AvaloniaFact]
    public void AnimationDuration_DefaultValue_Is300Milliseconds()
    {
        // Arrange & Act
        var zoomBorder = new ZoomBorder();

        // Assert
        Assert.Equal(TimeSpan.FromMilliseconds(300), zoomBorder.AnimationDuration);
    }

    [AvaloniaFact]
    public void AnimationDuration_CanBeSetToCustomValue()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();

        // Act
        zoomBorder.AnimationDuration = TimeSpan.FromMilliseconds(500);

        // Assert
        Assert.Equal(TimeSpan.FromMilliseconds(500), zoomBorder.AnimationDuration);
    }

    [AvaloniaFact]
    public void Animation_AllPropertiesCanBeSetTogether()
    {
        // Arrange & Act
        var zoomBorder = new ZoomBorder
        {
            EnableAnimations = true,
            AnimationDuration = TimeSpan.FromMilliseconds(250)
        };

        // Assert
        Assert.True(zoomBorder.EnableAnimations);
        Assert.Equal(TimeSpan.FromMilliseconds(250), zoomBorder.AnimationDuration);
    }

    [AvaloniaFact]
    public void AnimationDuration_CanBeSetToZero()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();

        // Act
        zoomBorder.AnimationDuration = TimeSpan.Zero;

        // Assert
        Assert.Equal(TimeSpan.Zero, zoomBorder.AnimationDuration);
    }

    [AvaloniaFact]
    public void AnimationDuration_CanBeSetToLongDuration()
    {
        // Arrange
        var zoomBorder = new ZoomBorder();

        // Act
        zoomBorder.AnimationDuration = TimeSpan.FromSeconds(2);

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(2), zoomBorder.AnimationDuration);
    }

    // ===== Functional Animation Tests =====

    [AvaloniaFact]
    public void ZoomTo_WithAnimationsEnabled_AppliesZoom()
    {
        // Arrange
        var canvas = new Canvas { Width = 400, Height = 400 };
        var zoomBorder = new ZoomBorder
        {
            Width = 800,
            Height = 600,
            EnableAnimations = true,
            AnimationDuration = TimeSpan.FromMilliseconds(100),
            Child = canvas
        };
        var window = new Window { Content = zoomBorder };
        window.Show();

        var initialZoomX = zoomBorder.ZoomX;

        // Act - ZoomTo with animation enabled (not skipping transitions)
        zoomBorder.ZoomTo(2.0, 100, 100, skipTransitions: false);

        // Assert - Zoom value should be updated even with animations enabled
        Assert.True(zoomBorder.ZoomX > initialZoomX, "ZoomX should increase after ZoomTo");
    }

    [AvaloniaFact]
    public void ZoomTo_WithAnimationsDisabled_AppliesZoomImmediately()
    {
        // Arrange
        var canvas = new Canvas { Width = 400, Height = 400 };
        var zoomBorder = new ZoomBorder
        {
            Width = 800,
            Height = 600,
            EnableAnimations = false, // Animations disabled
            Child = canvas
        };
        var window = new Window { Content = zoomBorder };
        window.Show();

        var initialZoomX = zoomBorder.ZoomX;

        // Act
        zoomBorder.ZoomTo(2.0, 100, 100);

        // Assert - Zoom should be applied immediately
        Assert.True(zoomBorder.ZoomX > initialZoomX);
    }

    [AvaloniaFact]
    public void SkipTransitions_True_DisablesTransitionsTemporarily()
    {
        // Arrange
        var canvas = new Canvas { Width = 400, Height = 400 };
        var zoomBorder = new ZoomBorder
        {
            Width = 800,
            Height = 600,
            EnableAnimations = true,
            AnimationDuration = TimeSpan.FromMilliseconds(100),
            Child = canvas
        };
        var window = new Window { Content = zoomBorder };
        window.Show();

        var initialZoomX = zoomBorder.ZoomX;

        // Act - Use skipTransitions: true to bypass animations
        zoomBorder.ZoomTo(2.0, 100, 100, skipTransitions: true);

        // Assert - Zoom should be applied
        Assert.True(zoomBorder.ZoomX > initialZoomX);
    }

    [AvaloniaFact]
    public void Pan_WithAnimationsEnabled_AppliesPan()
    {
        // Arrange
        var canvas = new Canvas { Width = 400, Height = 400 };
        var zoomBorder = new ZoomBorder
        {
            Width = 800,
            Height = 600,
            EnableAnimations = true,
            AnimationDuration = TimeSpan.FromMilliseconds(100),
            EnablePan = true,
            Child = canvas
        };
        var window = new Window { Content = zoomBorder };
        window.Show();

        var initialOffsetX = zoomBorder.OffsetX;

        // Act - PanDelta with animations (not skipping transitions)
        zoomBorder.PanDelta(50, 50, skipTransitions: false);

        // Assert - Offset should be updated
        Assert.NotEqual(initialOffsetX, zoomBorder.OffsetX);
    }

    [AvaloniaFact]
    public void ResetMatrix_WithAnimations_ResetsZoomAndPan()
    {
        // Arrange
        var canvas = new Canvas { Width = 400, Height = 400 };
        var zoomBorder = new ZoomBorder
        {
            Width = 800,
            Height = 600,
            EnableAnimations = true,
            AnimationDuration = TimeSpan.FromMilliseconds(100),
            Child = canvas
        };
        var window = new Window { Content = zoomBorder };
        window.Show();

        // First zoom and pan to a non-default state
        zoomBorder.ZoomTo(2.0, 100, 100, skipTransitions: true);
        zoomBorder.PanDelta(50, 50, skipTransitions: true);

        // Act - Reset with animation
        zoomBorder.ResetMatrix(skipTransitions: false);

        // Assert - Matrix should be reset to identity
        Assert.Equal(1.0, zoomBorder.ZoomX, 5);
        Assert.Equal(1.0, zoomBorder.ZoomY, 5);
    }

    [AvaloniaFact]
    public void AnimationDuration_Zero_TreatedAsNoAnimation()
    {
        // Arrange
        var canvas = new Canvas { Width = 400, Height = 400 };
        var zoomBorder = new ZoomBorder
        {
            Width = 800,
            Height = 600,
            EnableAnimations = true, // Enabled but...
            AnimationDuration = TimeSpan.Zero, // ...duration is zero
            Child = canvas
        };
        var window = new Window { Content = zoomBorder };
        window.Show();

        var initialZoomX = zoomBorder.ZoomX;

        // Act
        zoomBorder.ZoomTo(2.0, 100, 100);

        // Assert - Should still apply (zero duration means no animation)
        Assert.True(zoomBorder.ZoomX > initialZoomX);
    }

    [AvaloniaFact]
    public void EnableAnimations_AddsRenderTransformTransitionToChild()
    {
        var child = new Canvas { Width = 400, Height = 400 };
        var zoomBorder = new ZoomBorder
        {
            Width = 800,
            Height = 600,
            EnableAnimations = true,
            AnimationDuration = TimeSpan.FromMilliseconds(450),
            Child = child
        };
        var window = new Window { Content = zoomBorder };
        window.Show();

        var transition = Assert.Single(child.Transitions!.OfType<TransformOperationsTransition>());
        Assert.Equal(Visual.RenderTransformProperty, transition.Property);
        Assert.Equal(TimeSpan.FromMilliseconds(450), transition.Duration);
    }

    [AvaloniaFact]
    public void AnimationSettings_UpdateOwnedRenderTransformTransition()
    {
        var child = new Canvas { Width = 400, Height = 400 };
        var zoomBorder = new ZoomBorder
        {
            Width = 800,
            Height = 600,
            EnableAnimations = true,
            Child = child
        };
        var window = new Window { Content = zoomBorder };
        window.Show();

        zoomBorder.AnimationDuration = TimeSpan.FromMilliseconds(750);

        var transition = Assert.Single(child.Transitions!.OfType<TransformOperationsTransition>());
        Assert.Equal(TimeSpan.FromMilliseconds(750), transition.Duration);

        zoomBorder.EnableAnimations = false;

        Assert.Empty(child.Transitions!.OfType<TransformOperationsTransition>());
    }

    [AvaloniaFact]
    public void EnableAnimations_PreservesExistingRenderTransformTransition()
    {
        var existingTransition = new TransformOperationsTransition
        {
            Property = Visual.RenderTransformProperty,
            Duration = TimeSpan.FromSeconds(1)
        };
        var child = new Canvas
        {
            Width = 400,
            Height = 400,
            Transitions = new Transitions { existingTransition }
        };
        var zoomBorder = new ZoomBorder
        {
            Width = 800,
            Height = 600,
            EnableAnimations = true,
            AnimationDuration = TimeSpan.FromMilliseconds(300),
            Child = child
        };
        var window = new Window { Content = zoomBorder };
        window.Show();

        Assert.Same(existingTransition, Assert.Single(child.Transitions.OfType<TransformOperationsTransition>()));
        Assert.Equal(TimeSpan.FromSeconds(1), existingTransition.Duration);
    }
}

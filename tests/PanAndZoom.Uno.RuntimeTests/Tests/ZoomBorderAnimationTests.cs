// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using System.Diagnostics;
using Microsoft.UI.Xaml.Media.Animation;

namespace PanAndZoom.Uno.RuntimeTests.Tests;

/// <summary>
/// Tests for animation functionality.
/// </summary>
/// <remarks>
/// Avalonia animates the child render transform with a <c>TransformOperationsTransition</c>; on Uno the control
/// animates the child <see cref="MatrixTransform"/> itself, which is observable through
/// <see cref="ZoomBorder.IsAnimating"/> and <see cref="ZoomBorder.RenderMatrix"/>.
/// </remarks>
[TestClass]
[RunsOnUIThread]
public class ZoomBorderAnimationTests
{
    [TestCleanup]
    public async Task Cleanup()
    {
        InputHelper.Reset();
        await ZoomBorderTestHelper.UnloadAsync();
    }

    private static async Task<(ZoomBorder ZoomBorder, Canvas Canvas)> CreateCanvasAndLoadAsync(Action<ZoomBorder>? configure = null, Canvas? canvas = null)
    {
        canvas ??= new Canvas { Width = 400, Height = 400 };
        var zoomBorder = new ZoomBorder
        {
            Width = 800,
            Height = 600,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Child = canvas
        };
        configure?.Invoke(zoomBorder);
        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        // The initial auto fit may be animated when animations are enabled, let it settle.
        await WaitForAnimationAsync(zoomBorder, Stopwatch.StartNew());
        return (zoomBorder, canvas);
    }

    /// <summary>
    /// Waits for the running animation to complete and returns the time elapsed since <paramref name="stopwatch"/> started.
    /// </summary>
    private static async Task<TimeSpan> WaitForAnimationAsync(ZoomBorder zoomBorder, Stopwatch stopwatch)
    {
        Assert.IsTrue(await ZoomBorderTestHelper.WaitForAsync(() => !zoomBorder.IsAnimating, 5000), "The animation should complete");
        return stopwatch.Elapsed;
    }

    [TestMethod]
    public void EnableAnimations_DefaultValue_IsFalse()
    {
        var zoomBorder = new ZoomBorder();

        Assert.IsFalse(zoomBorder.EnableAnimations);
    }

    [TestMethod]
    public void EnableAnimations_CanBeSetToTrue()
    {
        var zoomBorder = new ZoomBorder();

        zoomBorder.EnableAnimations = true;

        Assert.IsTrue(zoomBorder.EnableAnimations);
    }

    [TestMethod]
    public void AnimationDuration_DefaultValue_Is300Milliseconds()
    {
        var zoomBorder = new ZoomBorder();

        Assert.AreEqual(TimeSpan.FromMilliseconds(300), zoomBorder.AnimationDuration);
    }

    [TestMethod]
    public void AnimationDuration_CanBeSetToCustomValue()
    {
        var zoomBorder = new ZoomBorder();

        zoomBorder.AnimationDuration = TimeSpan.FromMilliseconds(500);

        Assert.AreEqual(TimeSpan.FromMilliseconds(500), zoomBorder.AnimationDuration);
    }

    [TestMethod]
    public void Animation_AllPropertiesCanBeSetTogether()
    {
        var zoomBorder = new ZoomBorder
        {
            EnableAnimations = true,
            AnimationDuration = TimeSpan.FromMilliseconds(250)
        };

        Assert.IsTrue(zoomBorder.EnableAnimations);
        Assert.AreEqual(TimeSpan.FromMilliseconds(250), zoomBorder.AnimationDuration);
    }

    [TestMethod]
    public void AnimationDuration_CanBeSetToZero()
    {
        var zoomBorder = new ZoomBorder();

        zoomBorder.AnimationDuration = TimeSpan.Zero;

        Assert.AreEqual(TimeSpan.Zero, zoomBorder.AnimationDuration);
    }

    [TestMethod]
    public void AnimationDuration_CanBeSetToLongDuration()
    {
        var zoomBorder = new ZoomBorder();

        zoomBorder.AnimationDuration = TimeSpan.FromSeconds(2);

        Assert.AreEqual(TimeSpan.FromSeconds(2), zoomBorder.AnimationDuration);
    }

    // ===== Functional Animation Tests =====

    [TestMethod]
    public async Task ZoomTo_WithAnimationsEnabled_AppliesZoom()
    {
        var (zoomBorder, _) = await CreateCanvasAndLoadAsync(zb =>
        {
            zb.EnableAnimations = true;
            zb.AnimationDuration = TimeSpan.FromMilliseconds(100);
        });

        var initialZoomX = zoomBorder.ZoomX;

        // ZoomTo with animation enabled (not skipping transitions)
        zoomBorder.ZoomTo(2.0, 100, 100, skipTransitions: false);

        // Zoom value should be updated even with animations enabled
        Assert.IsTrue(zoomBorder.ZoomX > initialZoomX, "ZoomX should increase after ZoomTo");

        // Uno: the render transform is animated by the control and reaches the target matrix.
        Assert.IsTrue(zoomBorder.IsAnimating, "The render transform should be animated");
        await WaitForAnimationAsync(zoomBorder, Stopwatch.StartNew());
        Assert.AreEqual(zoomBorder.Matrix, ZoomBorderTestHelper.GetChildRenderMatrix(zoomBorder));
    }

    [TestMethod]
    public async Task ZoomTo_WithAnimationsDisabled_AppliesZoomImmediately()
    {
        // Animations disabled
        var (zoomBorder, _) = await CreateCanvasAndLoadAsync(zb => zb.EnableAnimations = false);

        var initialZoomX = zoomBorder.ZoomX;

        zoomBorder.ZoomTo(2.0, 100, 100);

        // Zoom should be applied immediately
        Assert.IsTrue(zoomBorder.ZoomX > initialZoomX);

        // Uno: no animation is started and the render transform is updated synchronously.
        Assert.IsFalse(zoomBorder.IsAnimating);
        Assert.AreEqual(zoomBorder.Matrix, ZoomBorderTestHelper.GetChildRenderMatrix(zoomBorder));
    }

    [TestMethod]
    public async Task SkipTransitions_True_DisablesTransitionsTemporarily()
    {
        var (zoomBorder, _) = await CreateCanvasAndLoadAsync(zb =>
        {
            zb.EnableAnimations = true;
            zb.AnimationDuration = TimeSpan.FromMilliseconds(100);
        });

        var initialZoomX = zoomBorder.ZoomX;

        // Use skipTransitions: true to bypass animations
        zoomBorder.ZoomTo(2.0, 100, 100, skipTransitions: true);

        // Zoom should be applied
        Assert.IsTrue(zoomBorder.ZoomX > initialZoomX);

        // Uno: the transition is skipped, so the render transform is updated synchronously.
        Assert.IsFalse(zoomBorder.IsAnimating);
        Assert.AreEqual(zoomBorder.Matrix, ZoomBorderTestHelper.GetChildRenderMatrix(zoomBorder));

        // ...and transitions are only disabled temporarily: the next change is animated again.
        zoomBorder.ZoomTo(1.5, 100, 100, skipTransitions: false);
        Assert.IsTrue(zoomBorder.IsAnimating);
        await WaitForAnimationAsync(zoomBorder, Stopwatch.StartNew());
    }

    [TestMethod]
    public async Task Pan_WithAnimationsEnabled_AppliesPan()
    {
        var (zoomBorder, _) = await CreateCanvasAndLoadAsync(zb =>
        {
            zb.EnableAnimations = true;
            zb.AnimationDuration = TimeSpan.FromMilliseconds(100);
            zb.EnablePan = true;
        });

        var initialOffsetX = zoomBorder.OffsetX;

        // PanDelta with animations (not skipping transitions)
        zoomBorder.PanDelta(50, 50, skipTransitions: false);

        // Offset should be updated
        Assert.AreNotEqual(initialOffsetX, zoomBorder.OffsetX);

        // Uno: the animated render transform reaches the panned matrix.
        await WaitForAnimationAsync(zoomBorder, Stopwatch.StartNew());
        Assert.AreEqual(zoomBorder.Matrix, ZoomBorderTestHelper.GetChildRenderMatrix(zoomBorder));
    }

    [TestMethod]
    public async Task ResetMatrix_WithAnimations_ResetsZoomAndPan()
    {
        var (zoomBorder, _) = await CreateCanvasAndLoadAsync(zb =>
        {
            zb.EnableAnimations = true;
            zb.AnimationDuration = TimeSpan.FromMilliseconds(100);
        });

        // First zoom and pan to a non-default state
        zoomBorder.ZoomTo(2.0, 100, 100, skipTransitions: true);
        zoomBorder.PanDelta(50, 50, skipTransitions: true);

        // Reset with animation
        zoomBorder.ResetMatrix(skipTransitions: false);

        // Matrix should be reset to identity
        Assert.AreEqual(1.0, zoomBorder.ZoomX, 1e-5);
        Assert.AreEqual(1.0, zoomBorder.ZoomY, 1e-5);

        // Uno: the animated render transform reaches the identity matrix.
        await WaitForAnimationAsync(zoomBorder, Stopwatch.StartNew());
        Assert.AreEqual(Matrix.Identity, ZoomBorderTestHelper.GetChildRenderMatrix(zoomBorder));
    }

    [TestMethod]
    public async Task AnimationDuration_Zero_TreatedAsNoAnimation()
    {
        var (zoomBorder, _) = await CreateCanvasAndLoadAsync(zb =>
        {
            zb.EnableAnimations = true; // Enabled but...
            zb.AnimationDuration = TimeSpan.Zero; // ...duration is zero
        });

        var initialZoomX = zoomBorder.ZoomX;

        zoomBorder.ZoomTo(2.0, 100, 100);

        // Should still apply (zero duration means no animation)
        Assert.IsTrue(zoomBorder.ZoomX > initialZoomX);

        // Uno: no animation is started.
        Assert.IsFalse(zoomBorder.IsAnimating);
        Assert.AreEqual(zoomBorder.Matrix, ZoomBorderTestHelper.GetChildRenderMatrix(zoomBorder));
    }

    [TestMethod]
    public async Task EnableAnimations_AddsRenderTransformTransitionToChild()
    {
        // Uno: there is no TransformOperationsTransition; the control owns a MatrixTransform on the child and
        // animates it for AnimationDuration.
        var (zoomBorder, child) = await CreateCanvasAndLoadAsync(zb =>
        {
            zb.EnableAnimations = true;
            zb.AnimationDuration = TimeSpan.FromMilliseconds(450);
        });

        var transform = child.RenderTransform as MatrixTransform;
        Assert.IsNotNull(transform, "The child render transform should be a MatrixTransform owned by the control");

        var stopwatch = Stopwatch.StartNew();
        zoomBorder.ZoomTo(2.0, 100, 100);

        Assert.IsTrue(zoomBorder.IsAnimating);
        Assert.AreNotEqual(zoomBorder.Matrix, transform!.Matrix, "The render transform should not jump to the target");

        var elapsed = await WaitForAnimationAsync(zoomBorder, stopwatch);

        Assert.IsTrue(elapsed >= TimeSpan.FromMilliseconds(450), $"The animation should last AnimationDuration (elapsed {elapsed.TotalMilliseconds} ms)");
        Assert.AreSame(transform, child.RenderTransform);
        Assert.AreEqual(zoomBorder.Matrix, transform.Matrix);
    }

    [TestMethod]
    public async Task AnimationSettings_UpdateOwnedRenderTransformTransition()
    {
        var (zoomBorder, child) = await CreateCanvasAndLoadAsync(zb => zb.EnableAnimations = true);

        zoomBorder.AnimationDuration = TimeSpan.FromMilliseconds(750);

        // Uno: the updated duration is used by the next animation of the owned render transform.
        var stopwatch = Stopwatch.StartNew();
        zoomBorder.ZoomTo(2.0, 100, 100);
        Assert.IsTrue(zoomBorder.IsAnimating);
        var elapsed = await WaitForAnimationAsync(zoomBorder, stopwatch);
        Assert.IsTrue(elapsed >= TimeSpan.FromMilliseconds(750), $"The animation should last the updated AnimationDuration (elapsed {elapsed.TotalMilliseconds} ms)");

        zoomBorder.EnableAnimations = false;

        // Uno: with animations disabled the owned render transform is updated without animation.
        zoomBorder.ZoomTo(1.5, 100, 100);
        Assert.IsFalse(zoomBorder.IsAnimating);
        Assert.AreEqual(zoomBorder.Matrix, ZoomBorderTestHelper.GetChildRenderMatrix(zoomBorder));
        Assert.IsInstanceOfType(child.RenderTransform, typeof(MatrixTransform));
    }

    [TestMethod]
    public async Task EnableAnimations_PreservesExistingRenderTransformTransition()
    {
        // Uno: the closest WinUI equivalent of user defined child transitions is UIElement.Transitions; the control
        // must not replace or modify them when it animates the child render transform.
        var existingTransition = new RepositionThemeTransition();
        var child = new Canvas
        {
            Width = 400,
            Height = 400,
            Transitions = new TransitionCollection { existingTransition }
        };
        var existingTransitions = child.Transitions;

        var (zoomBorder, _) = await CreateCanvasAndLoadAsync(zb =>
        {
            zb.EnableAnimations = true;
            zb.AnimationDuration = TimeSpan.FromMilliseconds(300);
        }, child);

        zoomBorder.ZoomTo(2.0, 100, 100);
        await WaitForAnimationAsync(zoomBorder, Stopwatch.StartNew());

        Assert.AreSame(existingTransitions, child.Transitions);
        Assert.AreEqual(1, child.Transitions.Count);
        Assert.AreSame(existingTransition, child.Transitions[0]);
    }
}

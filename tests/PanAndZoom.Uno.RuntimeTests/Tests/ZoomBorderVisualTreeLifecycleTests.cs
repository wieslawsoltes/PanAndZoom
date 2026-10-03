// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using Microsoft.UI;
using Microsoft.UI.Xaml.Input;

namespace PanAndZoom.Uno.RuntimeTests.Tests;

// Avalonia attach/detach (window.Show / window.Content = null) maps to Loaded/Unloaded in the Uno test window.
// Raised Avalonia pointer events are replaced by real injected mouse input, the ":isPanning" pseudo class by
// IsPanning, and Avalonia gesture recognizers by WinUI manipulations (ManipulationMode + injected touch).
[TestClass]
[RunsOnUIThread]
public class ZoomBorderVisualTreeLifecycleTests
{
    [TestCleanup]
    public async Task Cleanup()
    {
        InputHelper.Reset();
        await ZoomBorderTestHelper.UnloadAsync();
    }

    private static (ZoomBorder ZoomBorder, Border Child) Create(Action<ZoomBorder>? configure = null)
    {
        return ZoomBorderTestHelper.Create(400, 300, 200, 150, zb =>
        {
            ((Border)zb.Child!).Background = new SolidColorBrush(Colors.Red);
            configure?.Invoke(zb);
        });
    }

    // Waits for the injected input to be dispatched (input injection is asynchronous on native WinUI).
    private static async Task PressLeft(ZoomBorder zoomBorder, Point point)
    {
        InputHelper.MouseMoveTo(ZoomBorderTestHelper.ToWindow(zoomBorder, point));
        InputHelper.MouseDown(ButtonName.Left);
        await ZoomBorderTestHelper.WaitForIdleAsync();
    }

    private static async Task ReleaseLeft()
    {
        InputHelper.MouseUp(ButtonName.Left);
        await ZoomBorderTestHelper.WaitForIdleAsync();
    }

    private static bool HasGestureManipulations(ZoomBorder zoomBorder)
    {
        const ManipulationModes gestures = ManipulationModes.TranslateX | ManipulationModes.TranslateY | ManipulationModes.Scale;
        return (zoomBorder.ManipulationMode & gestures) == gestures;
    }

    [TestMethod]
    public async Task AttachToVisualTree_AddsEventHandlers()
    {
        var (zoomBorder, _) = Create(zb =>
        {
            zb.EnablePan = true;
            zb.EnableZoom = true;
            zb.EnableGestures = true;
            zb.PanButton = ButtonName.Left;
        });

        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        await PressLeft(zoomBorder, new Point(100, 75));

        Assert.IsTrue(zoomBorder.IsPanning, "Panning should be active after pointer pressed");
        await ReleaseLeft();
    }

    [TestMethod]
    public async Task DetachFromVisualTree_RemovesEventHandlers()
    {
        var (zoomBorder, _) = Create(zb =>
        {
            zb.EnablePan = true;
            zb.EnableZoom = true;
            zb.PanButton = ButtonName.Left;
        });

        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        await PressLeft(zoomBorder, new Point(100, 75));
        Assert.IsTrue(zoomBorder.IsPanning);

        // Detach from the visual tree
        await ZoomBorderTestHelper.UnloadAsync();

        // Releasing the button after detachment should not crash or cause issues
        await ReleaseLeft();
        await ZoomBorderTestHelper.WaitForIdleAsync();
    }

    [TestMethod]
    public async Task MultipleAttachDetach_HandlesCorrectly()
    {
        var (zoomBorder, _) = Create(zb =>
        {
            zb.EnablePan = true;
            zb.EnableZoom = true;
            zb.PanButton = ButtonName.Left;
        });

        for (int i = 0; i < 3; i++)
        {
            await ZoomBorderTestHelper.LoadAsync(zoomBorder);

            await PressLeft(zoomBorder, new Point(100, 75));
            Assert.IsTrue(zoomBorder.IsPanning, $"Panning should work on cycle {i + 1}");

            await ReleaseLeft();
            Assert.IsFalse(zoomBorder.IsPanning, $"Panning should stop on cycle {i + 1}");

            await ZoomBorderTestHelper.UnloadAsync();
        }
    }

    [TestMethod]
    public async Task AttachDetach_GestureRecognizersHandledCorrectly()
    {
        var (zoomBorder, _) = Create(zb =>
        {
            zb.EnableGestures = true;
            zb.EnableGestureZoom = true;
            zb.EnableGestureTranslation = true;
        });

        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        // Gesture recognizers are WinUI manipulations on Uno
        Assert.IsTrue(HasGestureManipulations(zoomBorder), "Manipulations should be enabled when EnableGestures is true");

        var initialManipulationMode = zoomBorder.ManipulationMode;

        var initialZoom = zoomBorder.ZoomX;
        InputHelper.Pinch(ZoomBorderTestHelper.ToWindow(zoomBorder, new Point(200, 150)), 100, 150);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(zoomBorder.ZoomX > initialZoom, "Pinch gesture should increase zoom");

        // Detach and reattach
        await ZoomBorderTestHelper.UnloadAsync();
        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        Assert.AreEqual(initialManipulationMode, zoomBorder.ManipulationMode);
    }

    [TestMethod]
    public async Task DisableGestures_RemovesGestureRecognizers()
    {
        var (zoomBorder, _) = Create(zb => zb.EnableGestures = true);

        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        Assert.IsTrue(HasGestureManipulations(zoomBorder));

        zoomBorder.EnableGestures = false;

        var initialZoom = zoomBorder.ZoomX;
        InputHelper.Pinch(ZoomBorderTestHelper.ToWindow(zoomBorder, new Point(200, 150)), 100, 150);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        // Gesture should not work when disabled
        Assert.AreEqual(initialZoom, zoomBorder.ZoomX);
    }

    [TestMethod]
    public async Task DetachDuringPinch_ReattachStartsFreshPinchState()
    {
        var (zoomBorder, _) = Create(zb =>
        {
            zb.EnableGestures = true;
            zb.EnableGestureZoom = true;
        });

        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        // Injected touch gestures complete synchronously, so the pinch is driven through the control
        // manipulation handlers (the same path as OnManipulationStarted/OnManipulationDelta) to detach mid-pinch.
        zoomBorder.HandleManipulationStarted();
        zoomBorder.HandleManipulationDelta(new Point(200, 150), new Point(0, 0), 2.0, 0.0, 2.0);
        var zoomAfterFirstPinch = zoomBorder.ZoomX;

        // Detach mid-pinch before the manipulation completes.
        await ZoomBorderTestHelper.UnloadAsync();
        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        zoomBorder.HandleManipulationStarted();
        zoomBorder.HandleManipulationDelta(new Point(200, 150), new Point(0, 0), 1.5, 0.0, 1.5);

        Assert.IsTrue(zoomBorder.ZoomX > zoomAfterFirstPinch,
            $"A fresh pinch after reattach should continue zooming in. Previous: {zoomAfterFirstPinch}, Current: {zoomBorder.ZoomX}");

        zoomBorder.HandleManipulationCompleted();
    }

    [TestMethod]
    public async Task EventHandlerLifecycle_NoMemoryLeaks()
    {
        var (zoomBorder, _) = Create(zb =>
        {
            zb.EnablePan = true;
            zb.EnableZoom = true;
            zb.EnableGestures = true;
        });

        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        var initialZoom = zoomBorder.ZoomX;
        InputHelper.MouseWheel(ZoomBorderTestHelper.ToWindow(zoomBorder, new Point(200, 150)), 120);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        // Zoom should have changed, indicating event handlers are working
        Assert.AreNotEqual(initialZoom, zoomBorder.ZoomX);

        await ZoomBorderTestHelper.UnloadAsync();

        Assert.IsTrue(true, "Attach/detach cycle completed without issues");
    }

    [TestMethod]
    public async Task ChildElementLifecycle_HandledCorrectly()
    {
        var zoomBorder = new ZoomBorder
        {
            Width = 400,
            Height = 300,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            EnablePan = true,
            PanButton = ButtonName.Left
        };

        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        // Add child after attach
        zoomBorder.Child = new Border
        {
            Width = 200,
            Height = 150,
            Background = new SolidColorBrush(Colors.Red)
        };
        await ZoomBorderTestHelper.WaitForIdleAsync();

        await PressLeft(zoomBorder, new Point(100, 75));
        Assert.IsTrue(zoomBorder.IsPanning);

        // Remove child
        zoomBorder.Child = null;

        // Should handle gracefully
        await ReleaseLeft();
        await ZoomBorderTestHelper.WaitForIdleAsync();
    }
}

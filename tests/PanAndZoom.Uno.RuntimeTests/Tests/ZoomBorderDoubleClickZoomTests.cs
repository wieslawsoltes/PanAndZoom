// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

namespace PanAndZoom.Uno.RuntimeTests.Tests;

/// <summary>
/// Port of the Avalonia <c>ZoomBorderDoubleClickZoomTests</c>. Tests raising <c>DoubleTapped</c> events in Avalonia
/// use a real injected mouse double click on Uno.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class ZoomBorderDoubleClickZoomTests
{
    [TestCleanup]
    public async Task Cleanup()
    {
        InputHelper.Reset();
        await ZoomBorderTestHelper.UnloadAsync();
    }

    private static async Task<ZoomBorder> CreateWithCanvasAsync(Action<ZoomBorder> configure, bool withChild = true)
    {
        return (await CreateRecordedWithCanvasAsync(configure, withChild)).ZoomBorder;
    }

    private static async Task<RoutedEventRecorder> CreateRecordedWithCanvasAsync(Action<ZoomBorder> configure, bool withChild = true)
    {
        var zoomBorder = new ZoomBorder
        {
            Width = 800,
            Height = 600,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };

        if (withChild)
        {
            zoomBorder.Child = new Canvas { Width = 400, Height = 400 };
        }

        configure(zoomBorder);
        var recorder = new RoutedEventRecorder(zoomBorder);
        await ZoomBorderTestHelper.LoadAsync(recorder.Host);
        InputHelper.Reset();
        return recorder;
    }

    [TestMethod]
    public void EnableDoubleClickZoom_DefaultValue_IsTrue()
    {
        var zoomBorder = new ZoomBorder();

        Assert.IsTrue(zoomBorder.EnableDoubleClickZoom);
    }

    [TestMethod]
    public void EnableDoubleClickZoom_CanBeSetToFalse()
    {
        var zoomBorder = new ZoomBorder();

        zoomBorder.EnableDoubleClickZoom = false;

        Assert.IsFalse(zoomBorder.EnableDoubleClickZoom);
    }

    [TestMethod]
    public void DoubleClickZoomMode_DefaultValue_IsZoomInOut()
    {
        var zoomBorder = new ZoomBorder();

        Assert.AreEqual(DoubleClickZoomMode.ZoomInOut, zoomBorder.DoubleClickZoomMode);
    }

    [TestMethod]
    public void DoubleClickZoomMode_CanBeSetToZoomIn()
    {
        var zoomBorder = new ZoomBorder();

        zoomBorder.DoubleClickZoomMode = DoubleClickZoomMode.ZoomIn;

        Assert.AreEqual(DoubleClickZoomMode.ZoomIn, zoomBorder.DoubleClickZoomMode);
    }

    [TestMethod]
    public void DoubleClickZoomMode_CanBeSetToZoomOut()
    {
        var zoomBorder = new ZoomBorder();

        zoomBorder.DoubleClickZoomMode = DoubleClickZoomMode.ZoomOut;

        Assert.AreEqual(DoubleClickZoomMode.ZoomOut, zoomBorder.DoubleClickZoomMode);
    }

    [TestMethod]
    public void DoubleClickZoomMode_CanBeSetToZoomToFit()
    {
        var zoomBorder = new ZoomBorder();

        zoomBorder.DoubleClickZoomMode = DoubleClickZoomMode.ZoomToFit;

        Assert.AreEqual(DoubleClickZoomMode.ZoomToFit, zoomBorder.DoubleClickZoomMode);
    }

    [TestMethod]
    public void DoubleClickZoomMode_CanBeSetToNone()
    {
        var zoomBorder = new ZoomBorder();

        zoomBorder.DoubleClickZoomMode = DoubleClickZoomMode.None;

        Assert.AreEqual(DoubleClickZoomMode.None, zoomBorder.DoubleClickZoomMode);
    }

    [TestMethod]
    public void DoubleClickZoomFactor_DefaultValue_IsTwo()
    {
        var zoomBorder = new ZoomBorder();

        Assert.AreEqual(2.0, zoomBorder.DoubleClickZoomFactor);
    }

    [TestMethod]
    public void DoubleClickZoomFactor_CanBeSetToCustomValue()
    {
        var zoomBorder = new ZoomBorder();

        zoomBorder.DoubleClickZoomFactor = 3.5;

        Assert.AreEqual(3.5, zoomBorder.DoubleClickZoomFactor);
    }

    [TestMethod]
    public void DoubleClickZoom_AllPropertiesCanBeSetTogether()
    {
        var zoomBorder = new ZoomBorder
        {
            EnableDoubleClickZoom = true,
            DoubleClickZoomMode = DoubleClickZoomMode.ZoomIn,
            DoubleClickZoomFactor = 2.5
        };

        Assert.IsTrue(zoomBorder.EnableDoubleClickZoom);
        Assert.AreEqual(DoubleClickZoomMode.ZoomIn, zoomBorder.DoubleClickZoomMode);
        Assert.AreEqual(2.5, zoomBorder.DoubleClickZoomFactor);
    }

    // ===== Functional Double-Click Zoom Tests =====
    // These validate the zoom operations that double-click triggers (programmatic, like the Avalonia tests).

    [TestMethod]
    public async Task ZoomIn_WithDoubleClickFactor_ActuallyZoomsIn()
    {
        var zoomBorder = await CreateWithCanvasAsync(zb =>
        {
            zb.EnableDoubleClickZoom = true;
            zb.DoubleClickZoomMode = DoubleClickZoomMode.ZoomIn;
            zb.DoubleClickZoomFactor = 2.0;
        });

        var initialZoomX = zoomBorder.ZoomX;

        // Simulate what double-click does in ZoomIn mode.
        zoomBorder.ZoomTo(zoomBorder.DoubleClickZoomFactor, 200, 200, skipTransitions: true);

        Assert.IsTrue(zoomBorder.ZoomX > initialZoomX, "ZoomX should increase after zoom in");
    }

    [TestMethod]
    public async Task ZoomOut_WithDoubleClickFactor_ActuallyZoomsOut()
    {
        var zoomBorder = await CreateWithCanvasAsync(zb =>
        {
            zb.EnableDoubleClickZoom = true;
            zb.DoubleClickZoomMode = DoubleClickZoomMode.ZoomOut;
            zb.DoubleClickZoomFactor = 2.0;
        });

        // First zoom in so we have something to zoom out from.
        zoomBorder.ZoomTo(2.0, 200, 200, skipTransitions: true);
        var initialZoomX = zoomBorder.ZoomX;

        // Simulate what double-click does in ZoomOut mode.
        zoomBorder.ZoomTo(1.0 / zoomBorder.DoubleClickZoomFactor, 200, 200, skipTransitions: true);

        Assert.IsTrue(zoomBorder.ZoomX < initialZoomX, "ZoomX should decrease after zoom out");
    }

    [TestMethod]
    public async Task DoubleClickZoom_WhenDisabled_ZoomToStillWorks()
    {
        var zoomBorder = await CreateWithCanvasAsync(zb =>
        {
            zb.EnableDoubleClickZoom = false; // Disabled
            zb.DoubleClickZoomMode = DoubleClickZoomMode.ZoomIn;
            zb.DoubleClickZoomFactor = 2.0;
        });

        var initialZoomX = zoomBorder.ZoomX;

        // ZoomTo should still work even when double-click is disabled.
        zoomBorder.ZoomTo(2.0, 200, 200, skipTransitions: true);

        Assert.IsTrue(zoomBorder.ZoomX > initialZoomX, "Programmatic ZoomTo should work even when double-click is disabled");
    }

    [TestMethod]
    public async Task DoubleClickZoomFactor_AppliedCorrectly()
    {
        var zoomBorder = await CreateWithCanvasAsync(zb => zb.DoubleClickZoomFactor = 3.0);

        var initialZoomX = zoomBorder.ZoomX;

        // Apply the double-click zoom factor.
        zoomBorder.ZoomTo(zoomBorder.DoubleClickZoomFactor, 200, 200, skipTransitions: true);

        Assert.IsTrue(zoomBorder.ZoomX > initialZoomX * 2.5, "ZoomX should approximately triple with factor 3.0");
    }

    [TestMethod]
    public async Task ZoomInOutMode_Toggle_WorksCorrectly()
    {
        var zoomBorder = await CreateWithCanvasAsync(zb =>
        {
            zb.EnableDoubleClickZoom = true;
            zb.DoubleClickZoomMode = DoubleClickZoomMode.ZoomInOut;
            zb.DoubleClickZoomFactor = 2.0;
        });

        var initialZoomX = zoomBorder.ZoomX;

        // First "click" should zoom in.
        zoomBorder.ZoomTo(zoomBorder.DoubleClickZoomFactor, 200, 200, skipTransitions: true);

        var afterFirstZoom = zoomBorder.ZoomX;

        Assert.IsTrue(afterFirstZoom > initialZoomX, "First zoom should increase zoom level");

        // Second "click" would typically reset in ZoomInOut mode.
        zoomBorder.ResetMatrix(skipTransitions: true);

        Assert.IsTrue(zoomBorder.ZoomX < afterFirstZoom, "Reset should decrease zoom level");
    }

    // ===== DoubleTapped Event Tests (real injected double click) =====

    [TestMethod]
    public async Task DoubleTapped_ZoomInMode_ZoomsIn()
    {
        var recorder = await CreateRecordedWithCanvasAsync(zb =>
        {
            zb.EnableDoubleClickZoom = true;
            zb.DoubleClickZoomMode = DoubleClickZoomMode.ZoomIn;
            zb.DoubleClickZoomFactor = 2.0;
        });
        var zoomBorder = recorder.ZoomBorder;

        var initialZoomX = zoomBorder.ZoomX;

        PointerTestHelpers.MouseDoubleClickAt(zoomBorder, new Point(200, 200));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(recorder.DoubleTappedHandled, "DoubleTapped should be handled in ZoomIn mode");
        Assert.IsTrue(zoomBorder.ZoomX > initialZoomX, "ZoomX should increase after double-tap in ZoomIn mode");
    }

    [TestMethod]
    public async Task DoubleTapped_ZoomOutMode_ZoomsOut()
    {
        var zoomBorder = await CreateWithCanvasAsync(zb =>
        {
            zb.EnableDoubleClickZoom = true;
            zb.DoubleClickZoomMode = DoubleClickZoomMode.ZoomOut;
            zb.DoubleClickZoomFactor = 2.0;
        });

        // First zoom in to have something to zoom out from.
        zoomBorder.ZoomTo(4.0, 200, 200, skipTransitions: true);
        var initialZoomX = zoomBorder.ZoomX;

        PointerTestHelpers.MouseDoubleClickAt(zoomBorder, new Point(200, 200));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        Assert.IsTrue(zoomBorder.ZoomX < initialZoomX, "ZoomX should decrease after double-tap in ZoomOut mode");
    }

    [TestMethod]
    public async Task DoubleTapped_ZoomInOutMode_Functionality_Test()
    {
        var zoomBorder = await CreateWithCanvasAsync(zb =>
        {
            zb.EnableDoubleClickZoom = true;
            zb.DoubleClickZoomMode = DoubleClickZoomMode.ZoomInOut;
            zb.DoubleClickZoomFactor = 2.0;
        });

        var initialZoomX = zoomBorder.ZoomX;

        // Simulate what the DoubleTapped handler does in ZoomInOut mode (programmatic, like the Avalonia test).
        zoomBorder.ZoomTo(zoomBorder.DoubleClickZoomFactor, 200, 200, skipTransitions: true);
        var afterFirstTap = zoomBorder.ZoomX;

        Assert.IsTrue(afterFirstTap > initialZoomX, "First double-tap should zoom in");

        // At higher zoom, should reset.
        zoomBorder.ResetMatrix(skipTransitions: true);

        Assert.IsTrue(zoomBorder.ZoomX < afterFirstTap, "Reset should decrease zoom level");
    }

    [TestMethod]
    public async Task DoubleTapped_ZoomToFitMode_FitsContent()
    {
        var zoomBorder = await CreateWithCanvasAsync(zb =>
        {
            zb.EnableDoubleClickZoom = true;
            zb.DoubleClickZoomMode = DoubleClickZoomMode.ZoomToFit;
        });

        // Zoom to a different level first.
        zoomBorder.ZoomTo(3.0, 200, 200, skipTransitions: true);
        var beforeZoom = zoomBorder.ZoomX;

        PointerTestHelpers.MouseDoubleClickAt(zoomBorder, new Point(200, 200));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        // Zoom should change (AutoFit was called).
        Assert.IsTrue(zoomBorder.ZoomX != beforeZoom || zoomBorder.OffsetX != 0 || zoomBorder.OffsetY != 0,
            "Double-tap ZoomToFit should auto-fit content");
    }

    [TestMethod]
    public async Task DoubleTapped_NoneMode_DoesNothing()
    {
        var recorder = await CreateRecordedWithCanvasAsync(zb =>
        {
            zb.EnableDoubleClickZoom = true;
            zb.DoubleClickZoomMode = DoubleClickZoomMode.None;
        });
        var zoomBorder = recorder.ZoomBorder;

        var initialZoomX = zoomBorder.ZoomX;
        var initialOffsetX = zoomBorder.OffsetX;
        var initialOffsetY = zoomBorder.OffsetY;

        PointerTestHelpers.MouseDoubleClickAt(zoomBorder, new Point(200, 200));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        // The double tap reached the control (the assertions below are not vacuous) but was not handled.
        Assert.AreEqual(1, recorder.DoubleTappedCount);
        Assert.IsFalse(recorder.DoubleTappedHandled);
        Assert.AreEqual(initialZoomX, zoomBorder.ZoomX);
        Assert.AreEqual(initialOffsetX, zoomBorder.OffsetX);
        Assert.AreEqual(initialOffsetY, zoomBorder.OffsetY);
    }

    [TestMethod]
    public async Task DoubleTapped_WhenDisabled_DoesNothing()
    {
        var recorder = await CreateRecordedWithCanvasAsync(zb =>
        {
            zb.EnableDoubleClickZoom = false; // Disabled
            zb.DoubleClickZoomMode = DoubleClickZoomMode.ZoomIn;
            zb.DoubleClickZoomFactor = 2.0;
        });
        var zoomBorder = recorder.ZoomBorder;

        var initialZoomX = zoomBorder.ZoomX;

        PointerTestHelpers.MouseDoubleClickAt(zoomBorder, new Point(200, 200));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        // The double tap reached the control (the assertions below are not vacuous) but was not handled.
        Assert.AreEqual(1, recorder.DoubleTappedCount);
        Assert.IsFalse(recorder.DoubleTappedHandled);
        Assert.AreEqual(initialZoomX, zoomBorder.ZoomX);
    }

    [TestMethod]
    public async Task DoubleTapped_WithNoChild_DoesNothing()
    {
        var recorder = await CreateRecordedWithCanvasAsync(zb =>
        {
            zb.EnableDoubleClickZoom = true;
            zb.DoubleClickZoomMode = DoubleClickZoomMode.ZoomIn;
            zb.DoubleClickZoomFactor = 2.0;
        }, withChild: false);
        var zoomBorder = recorder.ZoomBorder;

        var initialZoomX = zoomBorder.ZoomX;

        PointerTestHelpers.MouseDoubleClickAt(zoomBorder, new Point(200, 200));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        // The double tap reached the control (the assertions below are not vacuous) but was not handled.
        Assert.AreEqual(1, recorder.DoubleTappedCount);
        Assert.IsFalse(recorder.DoubleTappedHandled);
        // No crash, zoom unchanged (no child to reference).
        Assert.AreEqual(initialZoomX, zoomBorder.ZoomX);
    }
}

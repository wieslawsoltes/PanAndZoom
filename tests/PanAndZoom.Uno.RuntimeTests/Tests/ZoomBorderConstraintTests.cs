// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using Microsoft.UI;

namespace PanAndZoom.Uno.RuntimeTests.Tests;

[TestClass]
[RunsOnUIThread]
public class ZoomBorderConstraintTests
{
    [TestCleanup]
    public async Task Cleanup()
    {
        InputHelper.Reset();
        await ZoomBorderTestHelper.UnloadAsync();
    }

    [TestMethod]
    public async Task PointerWheel_ExceedsMaxZoom_ClampedToMaximum()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            EnableZoom = true,
            MaxZoomX = 2.0,
            MaxZoomY = 2.0
        };

        var childElement = new Border
        {
            Width = 200,
            Height = 150,
            Background = new SolidColorBrush(Colors.Red)
        };

        zoomBorder.Child = childElement;

        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        // Act - Try to zoom beyond maximum with large wheel delta
        // Real injected wheel input (10 notches = 1200 WinUI wheel delta).
        var wheelHandled = await ApiTestHelpers.MouseWheelAsync(zoomBorder, new Point(200, 150), 1200);

        // Assert
        Assert.IsTrue(zoomBorder.ZoomX <= 2.0, "ZoomX should not exceed maximum");
        Assert.IsTrue(zoomBorder.ZoomY <= 2.0, "ZoomY should not exceed maximum");
        Assert.IsTrue(wheelHandled, "Wheel event should be handled");
    }

    [TestMethod]
    public async Task PointerWheel_BelowMinZoom_ClampedToMinimum()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            EnableZoom = true,
            MinZoomX = 0.5,
            MinZoomY = 0.5
        };

        var childElement = new Border
        {
            Width = 200,
            Height = 150,
            Background = new SolidColorBrush(Colors.Red)
        };

        zoomBorder.Child = childElement;

        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        // Act - Try to zoom below minimum with large negative wheel delta
        // Real injected wheel input (-10 notches = -1200 WinUI wheel delta).
        var wheelHandled = await ApiTestHelpers.MouseWheelAsync(zoomBorder, new Point(200, 150), -1200);

        // Assert
        Assert.IsTrue(zoomBorder.ZoomX >= 0.5, "ZoomX should not go below minimum");
        Assert.IsTrue(zoomBorder.ZoomY >= 0.5, "ZoomY should not go below minimum");
        Assert.IsTrue(wheelHandled, "Wheel event should be handled");
    }

    [TestMethod]
    public async Task PinchGesture_ExceedsMaxZoom_ClampedToMaximum()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            EnableGestures = true,
            EnableGestureZoom = true,
            MaxZoomX = 3.0,
            MaxZoomY = 3.0
        };

        var childElement = new Border
        {
            Width = 200,
            Height = 150,
            Background = new SolidColorBrush(Colors.Red)
        };

        zoomBorder.Child = childElement;

        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        // First zoom close to maximum
        zoomBorder.ZoomTo(2.8, 100, 75);

        // Act - Try to zoom beyond maximum with pinch gesture
        // Avalonia raises a synthetic PinchEvent (scale 2.0); Uno uses a real two finger touch pinch
        // (distance 100 -> 200) and the "Pinch" GestureStarted event stands in for PinchEventArgs.Handled.
        var pinchHandled = false;
        zoomBorder.GestureStarted += (_, e) => pinchHandled |= e.GestureType == "Pinch";
        InputHelper.Pinch(ZoomBorderTestHelper.ToWindow(zoomBorder, new Point(200, 150)), 100, 200);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        // Assert
        Assert.IsTrue(zoomBorder.ZoomX <= 3.0, "ZoomX should not exceed maximum");
        Assert.IsTrue(zoomBorder.ZoomY <= 3.0, "ZoomY should not exceed maximum");
        Assert.IsTrue(pinchHandled, "Pinch event should be handled");
    }

    [TestMethod]
    public async Task PinchGesture_BelowMinZoom_ClampedToMinimum()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            EnableGestures = true,
            EnableGestureZoom = true,
            MinZoomX = 0.2,
            MinZoomY = 0.2
        };

        var childElement = new Border
        {
            Width = 200,
            Height = 150,
            Background = new SolidColorBrush(Colors.Red)
        };

        zoomBorder.Child = childElement;

        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        // First zoom close to minimum
        zoomBorder.ZoomTo(0.3, 100, 75);

        // Act - Try to zoom below minimum with pinch gesture
        // Avalonia raises a synthetic PinchEvent (scale 0.1); Uno uses a real two finger touch pinch
        // (distance 200 -> 20) and the "Pinch" GestureStarted event stands in for PinchEventArgs.Handled.
        var pinchHandled = false;
        zoomBorder.GestureStarted += (_, e) => pinchHandled |= e.GestureType == "Pinch";
        InputHelper.Pinch(ZoomBorderTestHelper.ToWindow(zoomBorder, new Point(200, 150)), 200, 20);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        // Assert
        Assert.IsTrue(zoomBorder.ZoomX >= 0.2, "ZoomX should not go below minimum");
        Assert.IsTrue(zoomBorder.ZoomY >= 0.2, "ZoomY should not go below minimum");
        Assert.IsTrue(pinchHandled, "Pinch event should be handled");
    }

    [TestMethod]
    public async Task Pan_WithOffsetLimits_RespectsBoundaries()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            EnablePan = true,
            PanButton = ButtonName.Left,
            MinOffsetX = -100,
            MaxOffsetX = 100,
            MinOffsetY = -75,
            MaxOffsetY = 75
        };

        var childElement = new Border
        {
            Width = 200,
            Height = 150,
            Background = new SolidColorBrush(Colors.Red)
        };

        zoomBorder.Child = childElement;

        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        // Set offset close to maximum
        zoomBorder.Pan(95, 70);

        // Act - Try to pan beyond limits
        // Real injected left button press at (200, 150) followed by a large move to (300, 200).
        InputHelper.Reset();
        InputHelper.MouseMoveTo(ZoomBorderTestHelper.ToWindow(zoomBorder, new Point(200, 150)));
        InputHelper.MouseDown(ButtonName.Left);

        // Simulate large movement that would exceed limits
        InputHelper.MouseMoveTo(ZoomBorderTestHelper.ToWindow(zoomBorder, new Point(300, 200)), 10);
        await ZoomBorderTestHelper.WaitForIdleAsync();

        // Assert
        Assert.IsTrue(zoomBorder.OffsetX <= 100, "OffsetX should not exceed maximum");
        Assert.IsTrue(zoomBorder.OffsetY <= 75, "OffsetY should not exceed maximum");
        Assert.IsTrue(zoomBorder.OffsetX >= -100, "OffsetX should not go below minimum");
        Assert.IsTrue(zoomBorder.OffsetY >= -75, "OffsetY should not go below minimum");
    }

    [TestMethod]
    public async Task ScrollGesture_WithOffsetLimits_RespectsBoundaries()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            EnableGestures = true,
            EnableGestureTranslation = true,
            MinOffsetX = -50,
            MaxOffsetX = 50,
            MinOffsetY = -50,
            MaxOffsetY = 50
        };

        var childElement = new Border
        {
            Width = 200,
            Height = 150,
            Background = new SolidColorBrush(Colors.Red)
        };

        zoomBorder.Child = childElement;

        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        // Set offset close to limits
        zoomBorder.Pan(45, 45);

        // Act - Try to scroll beyond limits
        // Avalonia raises a synthetic ScrollGesture (delta 100, 100); Uno uses a real one finger touch drag
        // (the finger moves opposite to the scroll delta) and the "Scroll" GestureStarted event stands in
        // for ScrollGestureEventArgs.Handled.
        var scrollHandled = false;
        zoomBorder.GestureStarted += (_, e) => scrollHandled |= e.GestureType == "Scroll";
        InputHelper.TouchDrag(
            ZoomBorderTestHelper.ToWindow(zoomBorder, new Point(250, 200)),
            ZoomBorderTestHelper.ToWindow(zoomBorder, new Point(150, 100)));
        await ZoomBorderTestHelper.WaitForIdleAsync();

        // Assert
        Assert.IsTrue(zoomBorder.OffsetX <= 50, "OffsetX should not exceed maximum");
        Assert.IsTrue(zoomBorder.OffsetY <= 50, "OffsetY should not exceed maximum");
        Assert.IsTrue(scrollHandled, "Scroll gesture should be handled");
    }

    [TestMethod]
    public async Task ZoomConstraints_DifferentXAndY_HandledIndependently()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            EnableZoom = true,
            MinZoomX = 0.5,
            MaxZoomX = 2.0,
            MinZoomY = 1.0,
            MaxZoomY = 4.0
        };

        var childElement = new Border
        {
            Width = 200,
            Height = 150,
            Background = new SolidColorBrush(Colors.Red)
        };

        zoomBorder.Child = childElement;

        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        // Act - Try to zoom beyond different limits for X and Y
        // Real injected wheel input (10 notches = 1200 WinUI wheel delta).
        await ApiTestHelpers.MouseWheelAsync(zoomBorder, new Point(200, 150), 1200);

        // Assert
        Assert.IsTrue(zoomBorder.ZoomX <= 2.0, "ZoomX should respect its maximum");
        Assert.IsTrue(zoomBorder.ZoomY <= 4.0, "ZoomY should respect its maximum");
        Assert.IsTrue(zoomBorder.ZoomX >= 0.5, "ZoomX should respect its minimum");
        Assert.IsTrue(zoomBorder.ZoomY >= 1.0, "ZoomY should respect its minimum");
    }

    [TestMethod]
    public async Task OffsetConstraints_DifferentXAndY_HandledIndependently()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            EnablePan = true,
            PanButton = ButtonName.Left,
            MinOffsetX = -200,
            MaxOffsetX = 200,
            MinOffsetY = -50,
            MaxOffsetY = 50
        };

        var childElement = new Border
        {
            Width = 200,
            Height = 150,
            Background = new SolidColorBrush(Colors.Red)
        };

        zoomBorder.Child = childElement;

        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        // Act - Set offsets that test different limits
        zoomBorder.Pan(250, 100); // Should be clamped to 200, 50

        // Assert
        Assert.IsTrue(zoomBorder.OffsetX <= 200, "OffsetX should respect its maximum");
        Assert.IsTrue(zoomBorder.OffsetY <= 50, "OffsetY should respect its maximum");

        // Test minimum limits
        zoomBorder.Pan(-250, -100); // Should be clamped to -200, -50

        Assert.IsTrue(zoomBorder.OffsetX >= -200, "OffsetX should respect its minimum");
        Assert.IsTrue(zoomBorder.OffsetY >= -50, "OffsetY should respect its minimum");
    }

    [TestMethod]
    public async Task ConstraintValidation_WithNegativeValues_HandlesCorrectly()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            EnableZoom = true,
            EnablePan = true,
            MinZoomX = 0.1,
            MaxZoomX = 10.0,
            MinZoomY = 0.1,
            MaxZoomY = 10.0,
            MinOffsetX = -1000,
            MaxOffsetX = 1000,
            MinOffsetY = -1000,
            MaxOffsetY = 1000
        };

        var childElement = new Border
        {
            Width = 200,
            Height = 150,
            Background = new SolidColorBrush(Colors.Red)
        };

        zoomBorder.Child = childElement;

        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        // Act - Test with extreme values
        zoomBorder.Zoom(0.05, 100, 75); // Below minimum zoom
        zoomBorder.Pan(-2000, -2000); // Below minimum offset

        // Assert
        Assert.IsTrue(zoomBorder.ZoomX >= 0.1, "ZoomX should be clamped to minimum");
        Assert.IsTrue(zoomBorder.ZoomY >= 0.1, "ZoomY should be clamped to minimum");
        Assert.IsTrue(zoomBorder.OffsetX >= -1000, "OffsetX should be clamped to minimum");
        Assert.IsTrue(zoomBorder.OffsetY >= -1000, "OffsetY should be clamped to minimum");

        // Test with values above maximum
        zoomBorder.Zoom(15.0, 100, 75); // Above maximum zoom
        zoomBorder.Pan(2000, 2000); // Above maximum offset

        Assert.IsTrue(zoomBorder.ZoomX <= 10.0, "ZoomX should be clamped to maximum");
        Assert.IsTrue(zoomBorder.ZoomY <= 10.0, "ZoomY should be clamped to maximum");
        Assert.IsTrue(zoomBorder.OffsetX <= 1000, "OffsetX should be clamped to maximum");
        Assert.IsTrue(zoomBorder.OffsetY <= 1000, "OffsetY should be clamped to maximum");
    }

    [TestMethod]
    public async Task ConstraintValidation_DuringInteraction_MaintainsConsistency()
    {
        // Arrange
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            EnableZoom = true,
            EnablePan = true,
            EnableGestures = true,
            PanButton = ButtonName.Left,
            MinZoomX = 0.5,
            MaxZoomX = 3.0,
            MinOffsetX = -100,
            MaxOffsetX = 100
        };

        var childElement = new Border
        {
            Width = 200,
            Height = 150,
            Background = new SolidColorBrush(Colors.Red)
        };

        zoomBorder.Child = childElement;

        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        // Act - Perform multiple interactions that could violate constraints
        for (int i = 0; i < 10; i++)
        {
            // Zoom (real injected wheel input, one notch in or out)
            await ApiTestHelpers.MouseWheelAsync(zoomBorder, new Point(200, 150), (i % 2 == 0 ? 1 : -1) * 120);

            // Pan (real one finger touch drag; the finger moves opposite to the Avalonia scroll gesture delta)
            var fingerDeltaX = i % 2 == 0 ? -20 : 20;
            InputHelper.TouchDrag(
                ZoomBorderTestHelper.ToWindow(zoomBorder, new Point(200, 150)),
                ZoomBorderTestHelper.ToWindow(zoomBorder, new Point(200 + fingerDeltaX, 150)));
            await ZoomBorderTestHelper.WaitForIdleAsync();

            // Assert constraints are maintained after each interaction
            Assert.IsTrue(zoomBorder.ZoomX >= 0.5 && zoomBorder.ZoomX <= 3.0,
                $"ZoomX constraint violated at iteration {i}: {zoomBorder.ZoomX}");
            Assert.IsTrue(zoomBorder.OffsetX >= -100 && zoomBorder.OffsetX <= 100,
                $"OffsetX constraint violated at iteration {i}: {zoomBorder.OffsetX}");
        }
    }

    [TestMethod]
    public async Task AutoCalculateMaxZoom_DoesNotCauseOffsetDrift()
    {
        // Arrange - Issue #124: MaxZoom and AutoCalculateMaxZoom unexpected behavior
        // When zoom reaches the upper bound, offset should not drift
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            EnableZoom = true,
            AutoCalculateMaxZoom = true,
            MaxZoomPixelSize = 2.0 // Max zoom is 2x
        };

        var childElement = new Border
        {
            Width = 200,
            Height = 150,
            Background = new SolidColorBrush(Colors.Red)
        };

        zoomBorder.Child = childElement;

        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        // Zoom to max first
        for (int i = 0; i < 10; i++)
        {
            zoomBorder.ZoomIn();
        }

        // Record initial offset at max zoom
        var initialOffsetX = zoomBorder.OffsetX;
        var initialOffsetY = zoomBorder.OffsetY;
        var initialZoomX = zoomBorder.ZoomX;
        var initialZoomY = zoomBorder.ZoomY;

        // Act - Try to zoom further beyond max multiple times
        for (int i = 0; i < 20; i++)
        {
            // Real injected wheel input (5 notches = 600 WinUI wheel delta).
            await ApiTestHelpers.MouseWheelAsync(zoomBorder, new Point(200, 150), 600);
        }

        // Assert - Offset should not have drifted when already at max zoom
        Assert.AreEqual(initialZoomX, zoomBorder.ZoomX, 1e-4); // Zoom should stay at max
        Assert.AreEqual(initialZoomY, zoomBorder.ZoomY, 1e-4);
        Assert.AreEqual(initialOffsetX, zoomBorder.OffsetX, 1e-4); // Offset should not drift
        Assert.AreEqual(initialOffsetY, zoomBorder.OffsetY, 1e-4);
    }

    [TestMethod]
    public async Task ManualMaxZoom_DoesNotCauseOffsetDrift()
    {
        // Arrange - When zoom reaches the manual upper bound, offset should not drift
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            EnableZoom = true,
            MaxZoomX = 2.0,
            MaxZoomY = 2.0
        };

        var childElement = new Border
        {
            Width = 200,
            Height = 150,
            Background = new SolidColorBrush(Colors.Red)
        };

        zoomBorder.Child = childElement;

        await ZoomBorderTestHelper.LoadAsync(zoomBorder);

        // Zoom to max first
        for (int i = 0; i < 10; i++)
        {
            zoomBorder.ZoomIn();
        }

        // Record initial offset at max zoom
        var initialOffsetX = zoomBorder.OffsetX;
        var initialOffsetY = zoomBorder.OffsetY;
        var initialZoomX = zoomBorder.ZoomX;
        var initialZoomY = zoomBorder.ZoomY;

        // Act - Try to zoom further beyond max multiple times
        for (int i = 0; i < 20; i++)
        {
            // Real injected wheel input (5 notches = 600 WinUI wheel delta).
            await ApiTestHelpers.MouseWheelAsync(zoomBorder, new Point(200, 150), 600);
        }

        // Assert - Offset should not have drifted when already at max zoom
        Assert.AreEqual(initialZoomX, zoomBorder.ZoomX, 1e-4); // Zoom should stay at max
        Assert.AreEqual(initialZoomY, zoomBorder.ZoomY, 1e-4);
        Assert.AreEqual(initialOffsetX, zoomBorder.OffsetX, 1e-4); // Offset should not drift
        Assert.AreEqual(initialOffsetY, zoomBorder.OffsetY, 1e-4);
    }

    [TestMethod]
    public async Task ZoomTo_WhenExceedingMaxLimit_ClampsRatioToPreventTranslationJump()
    {
        // Arrange - This tests the core fix: when ZoomTo would exceed max zoom,
        // the ratio should be clamped BEFORE applying ScaleAtPrepend to prevent
        // the translation jump bug.
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            EnableZoom = true,
            MaxZoomX = 2.0,
            MaxZoomY = 2.0,
            EnableConstrains = true,
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

        // Use the same center point for all zoom operations
        var zoomCenterX = 100.0;
        var zoomCenterY = 75.0;

        // Set initial zoom to 1.5 using absolute Zoom() method
        zoomBorder.Zoom(1.5, zoomCenterX, zoomCenterY, skipTransitions: true);

        // Verify we're at 1.5
        Assert.AreEqual(1.5, zoomBorder.ZoomX, 0.01);

        // Record state before the zoom that will exceed limits
        var initialOffsetX = zoomBorder.OffsetX;
        var initialOffsetY = zoomBorder.OffsetY;
        var initialZoom = zoomBorder.ZoomX;

        // Calculate where the zoom center point is in content space
        var contentPointX = (zoomCenterX - initialOffsetX) / initialZoom;
        var contentPointY = (zoomCenterY - initialOffsetY) / initialZoom;

        // Act - Try to zoom with ratio 1.5 (would go to 2.25, exceeds max 2.0)
        zoomBorder.ZoomTo(1.5, zoomCenterX, zoomCenterY, skipTransitions: true);

        // Assert - Zoom should be clamped to exactly 2.0
        Assert.AreEqual(2.0, zoomBorder.ZoomX, 1e-4);
        Assert.AreEqual(2.0, zoomBorder.ZoomY, 1e-4);

        // The content point under the zoom center should still map to the same screen position
        var finalOffsetX = zoomBorder.OffsetX;
        var finalOffsetY = zoomBorder.OffsetY;
        var finalZoom = zoomBorder.ZoomX;

        // Screen point = content point * zoom + offset
        var screenPointX = contentPointX * finalZoom + finalOffsetX;
        var screenPointY = contentPointY * finalZoom + finalOffsetY;

        // The screen point should be very close to the original zoom center
        Assert.AreEqual(zoomCenterX, screenPointX, 0.1); // Allow small floating point tolerance
        Assert.AreEqual(zoomCenterY, screenPointY, 0.1);
    }

    [TestMethod]
    public async Task ZoomTo_WhenExceedingMinLimit_ClampsRatioToPreventTranslationJump()
    {
        // Arrange - Same test but for minimum zoom limit
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            EnableZoom = true,
            MinZoomX = 0.5,
            MinZoomY = 0.5,
            EnableConstrains = true,
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

        var zoomCenterX = 100.0;
        var zoomCenterY = 75.0;

        // Set initial zoom to 0.7 using absolute Zoom() method
        zoomBorder.Zoom(0.7, zoomCenterX, zoomCenterY, skipTransitions: true);

        // Verify we're at 0.7
        Assert.AreEqual(0.7, zoomBorder.ZoomX, 0.01);

        var initialOffsetX = zoomBorder.OffsetX;
        var initialOffsetY = zoomBorder.OffsetY;
        var initialZoom = zoomBorder.ZoomX;

        var contentPointX = (zoomCenterX - initialOffsetX) / initialZoom;
        var contentPointY = (zoomCenterY - initialOffsetY) / initialZoom;

        // Act - Try to zoom with ratio 0.6 (would go to 0.42, below min 0.5)
        zoomBorder.ZoomTo(0.6, zoomCenterX, zoomCenterY, skipTransitions: true);

        // Assert - Zoom should be clamped to exactly 0.5
        Assert.AreEqual(0.5, zoomBorder.ZoomX, 1e-4);
        Assert.AreEqual(0.5, zoomBorder.ZoomY, 1e-4);

        var finalOffsetX = zoomBorder.OffsetX;
        var finalOffsetY = zoomBorder.OffsetY;
        var finalZoom = zoomBorder.ZoomX;

        var screenPointX = contentPointX * finalZoom + finalOffsetX;
        var screenPointY = contentPointY * finalZoom + finalOffsetY;

        Assert.AreEqual(zoomCenterX, screenPointX, 0.1);
        Assert.AreEqual(zoomCenterY, screenPointY, 0.1);
    }

    [TestMethod]
    public async Task Zoom_WhenExceedingMaxLimit_ClampsValueToPreventTranslationJump()
    {
        // Arrange - Test the Zoom() method (absolute zoom value) clamping
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            EnableZoom = true,
            MaxZoomX = 2.0,
            MaxZoomY = 2.0,
            EnableConstrains = true,
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

        var zoomCenterX = 150.0;
        var zoomCenterY = 100.0;

        // Act - Try to zoom to 3.0 (exceeds max 2.0)
        zoomBorder.Zoom(3.0, zoomCenterX, zoomCenterY, skipTransitions: true);

        // Assert - Zoom should be clamped to exactly 2.0
        Assert.AreEqual(2.0, zoomBorder.ZoomX, 1e-4);
        Assert.AreEqual(2.0, zoomBorder.ZoomY, 1e-4);

        // Verify the zoom center point maps correctly
        // For Zoom() with center (cx, cy), the formula is:
        // offset = center - (zoom * center) = center * (1 - zoom)
        var expectedOffsetX = zoomCenterX - (2.0 * zoomCenterX);
        var expectedOffsetY = zoomCenterY - (2.0 * zoomCenterY);

        Assert.AreEqual(expectedOffsetX, zoomBorder.OffsetX, 0.01);
        Assert.AreEqual(expectedOffsetY, zoomBorder.OffsetY, 0.01);
    }

    [TestMethod]
    public async Task ZoomTo_RepeatedZoomAtLimit_NoAccumulatedTranslationDrift()
    {
        // Arrange - Test that repeated zoom attempts at the limit don't cause drift
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            EnableZoom = true,
            MaxZoomX = 2.0,
            MaxZoomY = 2.0,
            EnableConstrains = true,
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

        // Use consistent zoom center for all operations
        var zoomCenterX = 100.0;
        var zoomCenterY = 75.0;

        // Get to exactly max zoom
        zoomBorder.Zoom(2.0, zoomCenterX, zoomCenterY, skipTransitions: true);

        var initialOffsetX = zoomBorder.OffsetX;
        var initialOffsetY = zoomBorder.OffsetY;

        // Act - Try to zoom in many times while already at max (using same center point)
        for (int i = 0; i < 50; i++)
        {
            zoomBorder.ZoomTo(1.2, zoomCenterX, zoomCenterY, skipTransitions: true);
        }

        // Assert - No drift should have occurred
        Assert.AreEqual(2.0, zoomBorder.ZoomX, 1e-4);
        Assert.AreEqual(2.0, zoomBorder.ZoomY, 1e-4);
        Assert.AreEqual(initialOffsetX, zoomBorder.OffsetX, 1e-4);
        Assert.AreEqual(initialOffsetY, zoomBorder.OffsetY, 1e-4);
    }

    [TestMethod]
    public async Task ZoomTo_ApproachingLimit_GradualTransitionWithoutJump()
    {
        // Arrange - Test smooth transition as we approach the limit
        // The point under the zoom center should stay stationary throughout
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            EnableZoom = true,
            MaxZoomX = 2.0,
            MaxZoomY = 2.0,
            EnableConstrains = true,
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

        // Use the same zoom center for all operations
        var zoomCenterX = 100.0;
        var zoomCenterY = 75.0;

        // Start at 1.0 (default), then zoom to 1.5x
        zoomBorder.ZoomTo(1.5, zoomCenterX, zoomCenterY, skipTransitions: true);
        Assert.AreEqual(1.5, zoomBorder.ZoomX, 0.01);

        // Track the content point under the zoom center
        var initialOffsetX = zoomBorder.OffsetX;
        var initialOffsetY = zoomBorder.OffsetY;
        var initialZoom = zoomBorder.ZoomX;
        var contentPointX = (zoomCenterX - initialOffsetX) / initialZoom;
        var contentPointY = (zoomCenterY - initialOffsetY) / initialZoom;

        // Act - Zoom in small increments, some will hit the limit
        for (int i = 0; i < 10; i++)
        {
            zoomBorder.ZoomTo(1.1, zoomCenterX, zoomCenterY, skipTransitions: true);

            var currentOffsetX = zoomBorder.OffsetX;
            var currentOffsetY = zoomBorder.OffsetY;
            var currentZoom = zoomBorder.ZoomX;

            // The content point under zoom center should map back to zoom center
            var currentScreenX = contentPointX * currentZoom + currentOffsetX;
            var currentScreenY = contentPointY * currentZoom + currentOffsetY;

            // Assert - The zoom center point should stay stationary (within tolerance)
            Assert.IsTrue(Math.Abs(currentScreenX - zoomCenterX) < 0.5,
                $"Iteration {i}: X drifted from {zoomCenterX} to {currentScreenX}");
            Assert.IsTrue(Math.Abs(currentScreenY - zoomCenterY) < 0.5,
                $"Iteration {i}: Y drifted from {zoomCenterY} to {currentScreenY}");
        }

        // Final zoom should be at max
        Assert.AreEqual(2.0, zoomBorder.ZoomX, 1e-4);
    }

    [TestMethod]
    public async Task Zoom_WhenConstrainsDisabled_IgnoresZoomLimits()
    {
        // Arrange - Zoom limits are set, but EnableConstrains is false,
        // so the Zoom() method should NOT clamp the value.
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            EnableZoom = true,
            MaxZoomX = 2.0,
            MaxZoomY = 2.0,
            MinZoomX = 0.5,
            MinZoomY = 0.5,
            EnableConstrains = false,
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

        // Act - Zoom to 3.0, which exceeds MaxZoom but constraints are disabled
        zoomBorder.Zoom(3.0, 100.0, 75.0, skipTransitions: true);

        // Assert - Should reach 3.0 since constraints are disabled
        Assert.AreEqual(3.0, zoomBorder.ZoomX, 1e-4);
        Assert.AreEqual(3.0, zoomBorder.ZoomY, 1e-4);
    }

    [TestMethod]
    public async Task ZoomTo_WhenConstrainsDisabled_IgnoresZoomLimits()
    {
        // Arrange - Zoom limits are set, but EnableConstrains is false,
        // so the ZoomTo() method should NOT clamp the ratio.
        var zoomBorder = new ZoomBorder
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 400,
            Height = 300,
            EnableZoom = true,
            MaxZoomX = 2.0,
            MaxZoomY = 2.0,
            MinZoomX = 0.5,
            MinZoomY = 0.5,
            EnableConstrains = false,
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

        // Set initial zoom to 1.5
        zoomBorder.Zoom(1.5, 100.0, 75.0, skipTransitions: true);

        // Act - ZoomTo with ratio 3.0 (would go to 4.5, exceeds max but constraints disabled)
        zoomBorder.ZoomTo(3.0, 100.0, 75.0, skipTransitions: true);

        // Assert - Should reach 4.5 since constraints are disabled
        Assert.AreEqual(4.5, zoomBorder.ZoomX, 1e-4);
        Assert.AreEqual(4.5, zoomBorder.ZoomY, 1e-4);
    }
}

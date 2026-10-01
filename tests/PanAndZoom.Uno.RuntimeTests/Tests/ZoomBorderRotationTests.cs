// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

namespace PanAndZoom.Uno.RuntimeTests.Tests;

/// <summary>
/// Tests for ZoomBorder rotation functionality.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class ZoomBorderRotationTests
{
    [TestCleanup]
    public async Task Cleanup()
    {
        InputHelper.Reset();
        await ZoomBorderTestHelper.UnloadAsync();
    }

    private static async Task<(ZoomBorder ZoomBorder, Canvas Canvas)> CreateCanvasAndLoadAsync(
        double width,
        double height,
        double canvasSize,
        Action<ZoomBorder>? configure = null)
    {
        var canvas = new Canvas { Width = canvasSize, Height = canvasSize };
        var zoomBorder = new ZoomBorder
        {
            Width = width,
            Height = height,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Child = canvas
        };
        configure?.Invoke(zoomBorder);
        await ZoomBorderTestHelper.LoadAsync(zoomBorder);
        return (zoomBorder, canvas);
    }

    /// <summary>
    /// Asserts that the child has the control owned <see cref="MatrixTransform"/> and that it contains the rotation.
    /// </summary>
    private static void AssertRotatedRenderTransform(ZoomBorder zoomBorder, Canvas canvas, double degrees)
    {
        // Uno: WinUI RenderTransform defaults to null, the control applies a MatrixTransform (pan, zoom and rotation).
        Assert.IsNotNull(canvas.RenderTransform);
        var transform = canvas.RenderTransform as MatrixTransform;
        Assert.IsNotNull(transform, "The child render transform should be a MatrixTransform");

        var matrix = transform!.Matrix;
        var angle = Math.Atan2(matrix.M12, matrix.M11) * 180.0 / Math.PI;
        var scale = Math.Sqrt(matrix.M11 * matrix.M11 + matrix.M12 * matrix.M12);
        Assert.AreEqual(degrees, angle, 1e-6, $"Rotation angle of {matrix}");
        Assert.AreEqual(zoomBorder.ZoomX, scale, 1e-6, $"Scale of {matrix}");
        Assert.AreEqual(zoomBorder.RenderMatrix, matrix);
    }

    [TestMethod]
    public void Rotation_DefaultValue_IsZero()
    {
        var zoomBorder = new ZoomBorder();

        Assert.AreEqual(0.0, zoomBorder.Rotation);
    }

    [TestMethod]
    public void MinRotation_DefaultValue_IsNegative180()
    {
        var zoomBorder = new ZoomBorder();

        Assert.AreEqual(-180.0, zoomBorder.MinRotation);
    }

    [TestMethod]
    public void MaxRotation_DefaultValue_Is180()
    {
        var zoomBorder = new ZoomBorder();

        Assert.AreEqual(180.0, zoomBorder.MaxRotation);
    }

    [TestMethod]
    public void EnableRotationSnapping_DefaultValue_IsFalse()
    {
        var zoomBorder = new ZoomBorder();

        Assert.IsFalse(zoomBorder.EnableRotationSnapping);
    }

    [TestMethod]
    public void RotationSnapAngle_DefaultValue_Is45()
    {
        var zoomBorder = new ZoomBorder();

        Assert.AreEqual(45.0, zoomBorder.RotationSnapAngle);
    }

    [TestMethod]
    public void Rotation_CanBeSet()
    {
        var zoomBorder = new ZoomBorder();

        zoomBorder.Rotation = 90.0;

        Assert.AreEqual(90.0, zoomBorder.Rotation);
    }

    [TestMethod]
    public void Rotate_IncreasesRotation()
    {
        var zoomBorder = new ZoomBorder
        {
            EnableGestureRotation = true
        };

        zoomBorder.Rotate(45.0);

        Assert.AreEqual(45.0, zoomBorder.Rotation);
    }

    [TestMethod]
    public void Rotate_WithSnapping_SnapsToNearestAngle()
    {
        var zoomBorder = new ZoomBorder
        {
            EnableGestureRotation = true,
            EnableRotationSnapping = true,
            RotationSnapAngle = 45.0
        };

        zoomBorder.Rotate(30.0); // Should snap to 45.0

        Assert.AreEqual(45.0, zoomBorder.Rotation);
    }

    [TestMethod]
    public void Rotate_RespectMinRotation()
    {
        var zoomBorder = new ZoomBorder
        {
            EnableGestureRotation = true,
            MinRotation = -90.0
        };

        zoomBorder.Rotate(-180.0); // Should clamp to -90.0

        Assert.AreEqual(-90.0, zoomBorder.Rotation);
    }

    [TestMethod]
    public void Rotate_RespectMaxRotation()
    {
        var zoomBorder = new ZoomBorder
        {
            EnableGestureRotation = true,
            MaxRotation = 90.0
        };

        zoomBorder.Rotate(180.0); // Should clamp to 90.0

        Assert.AreEqual(90.0, zoomBorder.Rotation);
    }

    [TestMethod]
    public void ResetRotation_SetsRotationToZero()
    {
        var zoomBorder = new ZoomBorder
        {
            Rotation = 90.0
        };

        zoomBorder.ResetRotation();

        Assert.AreEqual(0.0, zoomBorder.Rotation);
    }

    [TestMethod]
    public void SnapRotation_SnapsCurrentRotationToNearestAngle()
    {
        var zoomBorder = new ZoomBorder
        {
            Rotation = 32.0,
            EnableRotationSnapping = true,
            RotationSnapAngle = 45.0
        };

        zoomBorder.SnapRotation();

        Assert.AreEqual(45.0, zoomBorder.Rotation);
    }

    [TestMethod]
    public void Rotate_WhenGestureRotationDisabled_DoesNotChangeRotation()
    {
        var zoomBorder = new ZoomBorder
        {
            EnableGestureRotation = false
        };

        zoomBorder.Rotate(45.0);

        Assert.AreEqual(0.0, zoomBorder.Rotation);
    }

    [TestMethod]
    public async Task Rotate_AppliesTransformationToContent()
    {
        var (zoomBorder, canvas) = await CreateCanvasAndLoadAsync(800, 600, 400, zb => zb.EnableGestureRotation = true);

        // Rotate by 90 degrees
        zoomBorder.Rotate(90.0);

        // The rotation property should be updated
        Assert.AreEqual(90.0, zoomBorder.Rotation);

        // The content should have a RenderTransform applied
        AssertRotatedRenderTransform(zoomBorder, canvas, 90.0);
    }

    [TestMethod]
    public async Task ResetRotation_ClearsRotationTransformation()
    {
        var (zoomBorder, canvas) = await CreateCanvasAndLoadAsync(800, 600, 400, zb => zb.EnableGestureRotation = true);

        // First rotate
        zoomBorder.Rotate(45.0);
        Assert.AreEqual(45.0, zoomBorder.Rotation);

        // Reset rotation
        zoomBorder.ResetRotation();

        Assert.AreEqual(0.0, zoomBorder.Rotation);

        // Uno: the render transform no longer contains a rotation (it equals the pan and zoom matrix).
        Assert.AreEqual(zoomBorder.Matrix, ZoomBorderTestHelper.GetChildRenderMatrix(zoomBorder));
        Assert.AreEqual(0.0, ZoomBorderTestHelper.GetChildRenderMatrix(zoomBorder).M12, 1e-9);
    }

    [TestMethod]
    public async Task Rotation_WorksWithPanAndZoom()
    {
        var (zoomBorder, canvas) = await CreateCanvasAndLoadAsync(800, 600, 400, zb =>
        {
            zb.EnableGestureRotation = true;
            zb.EnablePan = true;
        });

        var initialZoomX = zoomBorder.ZoomX;
        var initialZoomY = zoomBorder.ZoomY;

        // Combine rotation with zoom (zoom by 2x ratio)
        zoomBorder.ZoomTo(2.0, 0, 0, skipTransitions: true);
        zoomBorder.Rotate(45.0);

        // Both operations should be applied
        Assert.AreEqual(45.0, zoomBorder.Rotation);
        // ZoomTo multiplies by the ratio, so zoom should be 2x the initial
        Assert.IsTrue(zoomBorder.ZoomX > initialZoomX, "ZoomX should increase after ZoomTo");
        Assert.IsTrue(zoomBorder.ZoomY > initialZoomY, "ZoomY should increase after ZoomTo");
        AssertRotatedRenderTransform(zoomBorder, canvas, 45.0);
    }

    [TestMethod]
    public async Task Rotation_90Degrees_AppliesCorrectTransformMatrix()
    {
        var (zoomBorder, canvas) = await CreateCanvasAndLoadAsync(400, 400, 200, zb => zb.EnableGestureRotation = true);

        // Rotate 90 degrees
        zoomBorder.Rotate(90.0);

        // Verify rotation was applied
        Assert.AreEqual(90.0, zoomBorder.Rotation);

        // The RenderTransform should be set and contain a 90 degree rotation
        Assert.IsNotNull(canvas.RenderTransform);
        AssertRotatedRenderTransform(zoomBorder, canvas, 90.0);
    }

    [TestMethod]
    public async Task RotateAt_RotatesAroundSpecificPoint()
    {
        var (zoomBorder, canvas) = await CreateCanvasAndLoadAsync(400, 400, 200, zb => zb.EnableGestureRotation = true);

        var center = new Point(100.0, 100.0);

        // Rotate 45 degrees around a specific point
        zoomBorder.RotateAt(45.0, center);

        // Verify rotation was applied
        Assert.AreEqual(45.0, zoomBorder.Rotation);
        AssertRotatedRenderTransform(zoomBorder, canvas, 45.0);
    }

    [TestMethod]
    public async Task Rotation_WithSnapping_SnapsToCorrectAngles()
    {
        var (zoomBorder, canvas) = await CreateCanvasAndLoadAsync(400, 400, 200, zb =>
        {
            zb.EnableGestureRotation = true;
            zb.EnableRotationSnapping = true;
            zb.RotationSnapAngle = 90.0; // Snap to 90 degree increments
        });

        // Rotate by an amount that should snap to 90
        zoomBorder.Rotate(60.0); // Between 0 and 90, closer to 90

        // Should snap to 90
        Assert.AreEqual(90.0, zoomBorder.Rotation);
        AssertRotatedRenderTransform(zoomBorder, canvas, 90.0);
    }

    [TestMethod]
    public async Task Rotation_Negative_AppliesCorrectly()
    {
        var (zoomBorder, canvas) = await CreateCanvasAndLoadAsync(400, 400, 200, zb => zb.EnableGestureRotation = true);

        // Rotate negative degrees
        zoomBorder.Rotate(-45.0);

        Assert.AreEqual(-45.0, zoomBorder.Rotation);
        AssertRotatedRenderTransform(zoomBorder, canvas, -45.0);
    }

    [TestMethod]
    public async Task ResetAll_ResetsRotationToo()
    {
        var (zoomBorder, _) = await CreateCanvasAndLoadAsync(400, 400, 200, zb => zb.EnableGestureRotation = true);

        // First rotate
        zoomBorder.Rotate(45.0);
        Assert.AreEqual(45.0, zoomBorder.Rotation);

        // Reset everything
        zoomBorder.ResetRotation();

        Assert.AreEqual(0.0, zoomBorder.Rotation);
    }
}

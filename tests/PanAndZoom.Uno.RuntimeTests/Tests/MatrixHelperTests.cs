// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
namespace PanAndZoom.Uno.RuntimeTests.Tests;

// WinUI Matrix is a 2D affine matrix: Avalonia M31/M32 are OffsetX/OffsetY and the third column
// (M13, M23, M33) is implicitly (0, 0, 1), so those assertions are expressed through the affine members.
[TestClass]
[RunsOnUIThread]
public class MatrixHelperTests
{
    [TestMethod]
    public void Translate_Returns_Matrix()
    {
        var target = MatrixHelper.Translate(20, 30);
        Assert.AreEqual(1.0, target.M11);
        Assert.AreEqual(0.0, target.M12);
        Assert.AreEqual(0.0, target.M21);
        Assert.AreEqual(1.0, target.M22);
        Assert.AreEqual(20.0, target.OffsetX);
        Assert.AreEqual(30.0, target.OffsetY);
    }

    [TestMethod]
    public void Scale_Returns_Matrix()
    {
        var target = MatrixHelper.Scale(2, 3);
        Assert.AreEqual(2.0, target.M11);
        Assert.AreEqual(0.0, target.M12);
        Assert.AreEqual(0.0, target.M21);
        Assert.AreEqual(3.0, target.M22);
        Assert.AreEqual(0.0, target.OffsetX);
        Assert.AreEqual(0.0, target.OffsetY);
    }

    [TestMethod]
    public void Transform_Point_Identity()
    {
        var inputPoint = new Point(0, 0);
        var testMatrix = new Matrix();
        var outputPoint = MatrixHelper.TransformPoint(testMatrix, inputPoint);

        Assert.AreEqual(0, inputPoint.X);
        Assert.AreEqual(0, inputPoint.Y);

        Assert.AreEqual(0, outputPoint.X);
        Assert.AreEqual(0, outputPoint.Y);
    }

    [TestMethod]
    public void Transform_Point_Positive()
    {
        var inputPoint = new Point(2, 1);
        var testMatrix = new Matrix(1.0, 0.0, 0.0, 2.0, 0.0, 0.0);
        var outputPoint = MatrixHelper.TransformPoint(testMatrix, inputPoint);

        Assert.AreEqual(2, inputPoint.X);
        Assert.AreEqual(1, inputPoint.Y);

        Assert.AreEqual(2, outputPoint.X);
        Assert.AreEqual(2, outputPoint.Y);
    }

    [TestMethod]
    public void ScaleAt_Same_Returns_Identity()
    {
        double scaleX = 1.0;
        double scaleY = 1.0;
        double centerX = 0.0;
        double centerY = 0.0;
        var outputMatrix = MatrixHelper.ScaleAt(scaleX, scaleY, centerX, centerY);
        Assert.IsTrue(outputMatrix.IsIdentity);
    }

    [TestMethod]
    public void ScaleAt_SameScale_PositiveCenter_Returns_Identity()
    {
        double scaleX = 1.0;
        double scaleY = 1.0;
        double centerX = 1.0;
        double centerY = 2.0;
        var outputMatrix = MatrixHelper.ScaleAt(scaleX, scaleY, centerX, centerY);
        Assert.IsTrue(outputMatrix.IsIdentity);
    }

    [TestMethod]
    public void ScaleAt_DoubleScale_PositiveCenter()
    {
        double scaleX = 2.0;
        double scaleY = 2.0;
        double centerX = 1.0;
        double centerY = 2.0;
        var outputMatrix = MatrixHelper.ScaleAt(scaleX, scaleY, centerX, centerY);

        Assert.AreEqual(2.0, outputMatrix.M11);
        Assert.AreEqual(0.0, outputMatrix.M12);
        Assert.AreEqual(0.0, outputMatrix.M21);
        Assert.AreEqual(2.0, outputMatrix.M22);
        Assert.AreEqual(-1.0, outputMatrix.OffsetX);
        Assert.AreEqual(-2.0, outputMatrix.OffsetY);
    }

    [TestMethod]
    public void ScaleAt_HalfScale_NegCenter()
    {
        double scaleX = 0.5;
        double scaleY = 0.5;
        double centerX = -1.0;
        double centerY = -2.0;
        var outputMatrix = MatrixHelper.ScaleAt(scaleX, scaleY, centerX, centerY);

        Assert.AreEqual(0.5, outputMatrix.M11);
        Assert.AreEqual(0.0, outputMatrix.M12);
        Assert.AreEqual(0.0, outputMatrix.M21);
        Assert.AreEqual(0.5, outputMatrix.M22);
        Assert.AreEqual(-0.5, outputMatrix.OffsetX);
        Assert.AreEqual(-1.0, outputMatrix.OffsetY);
    }

    [TestMethod]
    public void ScaleAndTranslate_Same_Returns_Identity()
    {
        double scaleX = 1.0;
        double scaleY = 1.0;
        double x = 0.0;
        double y = 0.0;
        var outputMatrix = MatrixHelper.ScaleAndTranslate(scaleX, scaleY, x, y);
        Assert.IsTrue(outputMatrix.IsIdentity);
    }

    [TestMethod]
    public void ScaleAndTranslate_DoubleScale_PositiveShift()
    {
        double scaleX = 2.0;
        double scaleY = 2.0;
        double x = 1.0;
        double y = 2.0;
        var outputMatrix = MatrixHelper.ScaleAndTranslate(scaleX, scaleY, x, y);

        Assert.AreEqual(2.0, outputMatrix.M11);
        Assert.AreEqual(0.0, outputMatrix.M12);
        Assert.AreEqual(0.0, outputMatrix.M21);
        Assert.AreEqual(2.0, outputMatrix.M22);
        Assert.AreEqual(1.0, outputMatrix.OffsetX);
        Assert.AreEqual(2.0, outputMatrix.OffsetY);
    }

    [TestMethod]
    public void ScaleAndTranslate_HalfScale_NegativeShift()
    {
        double scaleX = 0.5;
        double scaleY = 0.5;
        double x = -1.0;
        double y = -2.0;
        var outputMatrix = MatrixHelper.ScaleAndTranslate(scaleX, scaleY, x, y);

        Assert.AreEqual(0.5, outputMatrix.M11);
        Assert.AreEqual(0.0, outputMatrix.M12);
        Assert.AreEqual(0.0, outputMatrix.M21);
        Assert.AreEqual(0.5, outputMatrix.M22);
        Assert.AreEqual(-1.0, outputMatrix.OffsetX);
        Assert.AreEqual(-2.0, outputMatrix.OffsetY);
    }

    [TestMethod]
    public void ScaleAtPrepend_HalfScale_With_DoubleScale_Returns_Identity()
    {
        var halfScaleMatrix = MatrixHelper.Scale(0.5, 0.5);

        double scaleX = 2.0;
        double scaleY = 2.0;
        var outputMatrix = MatrixHelper.ScaleAtPrepend(halfScaleMatrix, scaleX, scaleY, 0, 0);

        Assert.IsTrue(outputMatrix.IsIdentity);
    }

    [TestMethod]
    public void Rotation_Zero_Returns_Identity()
    {
        double radians = 0.0;

        var outputMatrix = MatrixHelper.Rotation(radians);

        Assert.IsTrue(outputMatrix.IsIdentity);
    }

    [TestMethod]
    public void Rotation_90Degrees_Returns_CorrectMatrix()
    {
        double radians = Math.PI / 2.0;

        var outputMatrix = MatrixHelper.Rotation(radians);

        // For 90 degree rotation: cos=0, sin=1
        Assert.AreEqual(0.0, outputMatrix.M11, 1e-10);
        Assert.AreEqual(1.0, outputMatrix.M12, 1e-10);
        Assert.AreEqual(-1.0, outputMatrix.M21, 1e-10);
        Assert.AreEqual(0.0, outputMatrix.M22, 1e-10);
        Assert.AreEqual(0.0, outputMatrix.OffsetX);
        Assert.AreEqual(0.0, outputMatrix.OffsetY);
    }

    [TestMethod]
    public void Rotation_180Degrees_Returns_CorrectMatrix()
    {
        double radians = Math.PI;

        var outputMatrix = MatrixHelper.Rotation(radians);

        // For 180 degree rotation: cos=-1, sin=0
        Assert.AreEqual(-1.0, outputMatrix.M11, 1e-10);
        Assert.AreEqual(0.0, outputMatrix.M12, 1e-10);
        Assert.AreEqual(0.0, outputMatrix.M21, 1e-10);
        Assert.AreEqual(-1.0, outputMatrix.M22, 1e-10);
        Assert.AreEqual(0.0, outputMatrix.OffsetX);
        Assert.AreEqual(0.0, outputMatrix.OffsetY);
    }

    [TestMethod]
    public void Rotation_45Degrees_Returns_CorrectMatrix()
    {
        double radians = Math.PI / 4.0;
        double expected = Math.Sqrt(2) / 2.0;

        var outputMatrix = MatrixHelper.Rotation(radians);

        // For 45 degree rotation: cos=sin=sqrt(2)/2
        Assert.AreEqual(expected, outputMatrix.M11, 1e-10);
        Assert.AreEqual(expected, outputMatrix.M12, 1e-10);
        Assert.AreEqual(-expected, outputMatrix.M21, 1e-10);
        Assert.AreEqual(expected, outputMatrix.M22, 1e-10);
    }

    [TestMethod]
    public void Rotation_WithCenter_90Degrees_TransformsPointCorrectly()
    {
        // Rotate 90 degrees around center point (1, 1)
        double radians = Math.PI / 2.0;
        double centerX = 1.0;
        double centerY = 1.0;

        var outputMatrix = MatrixHelper.Rotation(radians, centerX, centerY);

        // Point (2, 1) is 1 unit right of the center; after 90 degrees it is 1 unit below the center
        var testPoint = new Point(2, 1);
        var transformedPoint = MatrixHelper.TransformPoint(outputMatrix, testPoint);

        Assert.AreEqual(1.0, transformedPoint.X, 1e-10);
        Assert.AreEqual(2.0, transformedPoint.Y, 1e-10);
    }

    [TestMethod]
    public void Rotation_WithCenter_PointAtCenterUnchanged()
    {
        double radians = Math.PI / 2.0;
        double centerX = 5.0;
        double centerY = 5.0;

        var outputMatrix = MatrixHelper.Rotation(radians, centerX, centerY);

        var testPoint = new Point(5, 5);
        var transformedPoint = MatrixHelper.TransformPoint(outputMatrix, testPoint);

        Assert.AreEqual(5.0, transformedPoint.X, 1e-10);
        Assert.AreEqual(5.0, transformedPoint.Y, 1e-10);
    }

    [TestMethod]
    public void Rotation_WithVectorCenter_90Degrees_TransformsPointCorrectly()
    {
        // WinUI has no Vector type: the Uno overload takes the center as a Point.
        double radians = Math.PI / 2.0;
        var center = new Point(1.0, 1.0);

        var outputMatrix = MatrixHelper.Rotation(radians, center);

        var testPoint = new Point(2, 1);
        var transformedPoint = MatrixHelper.TransformPoint(outputMatrix, testPoint);

        Assert.AreEqual(1.0, transformedPoint.X, 1e-10);
        Assert.AreEqual(2.0, transformedPoint.Y, 1e-10);
    }

    [TestMethod]
    public void Rotation_Negative90Degrees_Returns_CorrectMatrix()
    {
        double radians = -Math.PI / 2.0;

        var outputMatrix = MatrixHelper.Rotation(radians);

        // For -90 degree rotation: cos=0, sin=-1
        Assert.AreEqual(0.0, outputMatrix.M11, 1e-10);
        Assert.AreEqual(-1.0, outputMatrix.M12, 1e-10);
        Assert.AreEqual(1.0, outputMatrix.M21, 1e-10);
        Assert.AreEqual(0.0, outputMatrix.M22, 1e-10);
    }

    [TestMethod]
    public void Skew_Zero_Returns_Identity()
    {
        var outputMatrix = MatrixHelper.Skew(0f, 0f);

        Assert.AreEqual(1.0, outputMatrix.M11);
        Assert.AreEqual(0.0, outputMatrix.M12, 1e-10);
        Assert.AreEqual(0.0, outputMatrix.M21, 1e-10);
        Assert.AreEqual(1.0, outputMatrix.M22);
        Assert.AreEqual(0.0, outputMatrix.OffsetX);
        Assert.AreEqual(0.0, outputMatrix.OffsetY);
    }

    [TestMethod]
    public void Skew_PositiveAngleX_ReturnsCorrectMatrix()
    {
        float angleX = (float)(Math.PI / 4.0);
        float angleY = 0f;

        var outputMatrix = MatrixHelper.Skew(angleX, angleY);

        // For 45 degree skew: tan(45) = 1
        Assert.AreEqual(1.0, outputMatrix.M11);
        Assert.AreEqual(1.0, outputMatrix.M12, 1e-5);
        Assert.AreEqual(0.0, outputMatrix.M21, 1e-10);
        Assert.AreEqual(1.0, outputMatrix.M22);
    }

    [TestMethod]
    public void Skew_PositiveAngleY_ReturnsCorrectMatrix()
    {
        float angleX = 0f;
        float angleY = (float)(Math.PI / 4.0);

        var outputMatrix = MatrixHelper.Skew(angleX, angleY);

        Assert.AreEqual(1.0, outputMatrix.M11);
        Assert.AreEqual(0.0, outputMatrix.M12, 1e-10);
        Assert.AreEqual(1.0, outputMatrix.M21, 1e-5);
        Assert.AreEqual(1.0, outputMatrix.M22);
    }

    [TestMethod]
    public void Skew_BothAngles_ReturnsCorrectMatrix()
    {
        float angle = (float)(Math.PI / 6.0);

        var outputMatrix = MatrixHelper.Skew(angle, angle);

        // tan(30) ~ 0.577
        double expectedTan = Math.Tan(Math.PI / 6.0);
        Assert.AreEqual(1.0, outputMatrix.M11);
        Assert.AreEqual(expectedTan, outputMatrix.M12, 1e-5);
        Assert.AreEqual(expectedTan, outputMatrix.M21, 1e-5);
        Assert.AreEqual(1.0, outputMatrix.M22);
    }

    [TestMethod]
    public void Skew_NegativeAngles_ReturnsCorrectMatrix()
    {
        float angleX = (float)(-Math.PI / 4.0);
        float angleY = 0f;

        var outputMatrix = MatrixHelper.Skew(angleX, angleY);

        // tan(-45) = -1
        Assert.AreEqual(1.0, outputMatrix.M11);
        Assert.AreEqual(-1.0, outputMatrix.M12, 1e-5);
        Assert.AreEqual(0.0, outputMatrix.M21, 1e-10);
        Assert.AreEqual(1.0, outputMatrix.M22);
    }
}

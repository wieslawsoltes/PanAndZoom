using Xunit;

namespace Avalonia.Controls.PanAndZoom.UnitTests;

public class MatrixHelperTests
{
    [Fact]
    public void Translate_Returns_Matrix()
    {
        var target = MatrixHelper.Translate(20, 30);
        Assert.Equal(1.0, target.M11);
        Assert.Equal(0.0, target.M12);
        Assert.Equal(0.0, target.M21);
        Assert.Equal(1.0, target.M22);
        Assert.Equal(20.0, target.M31);
        Assert.Equal(30.0, target.M32);
    }

    [Fact]
    public void Scale_Returns_Matrix()
    {
        var target = MatrixHelper.Scale(2, 3);
        Assert.Equal(2.0, target.M11);
        Assert.Equal(0.0, target.M12);
        Assert.Equal(0.0, target.M21);
        Assert.Equal(3.0, target.M22);
        Assert.Equal(0.0, target.M31);
        Assert.Equal(0.0, target.M32);
    }

    [Fact]
    public void Transform_Point_Identity()
    {
        var inputPoint = new Point(0, 0);
        var testMatrix = new Matrix();
        var outputPoint = MatrixHelper.TransformPoint(testMatrix, inputPoint);

        Assert.Equal(0, inputPoint.X);
        Assert.Equal(0, inputPoint.Y);

        Assert.Equal(0, outputPoint.X);
        Assert.Equal(0, outputPoint.Y);
    }

    [Fact]
    public void Transform_Point_Positive()
    {
        var inputPoint = new Point(2, 1);
        var testMatrix = new Matrix(1.0, 0.0, 0.0, 2.0, 0.0, 0.0);
        var outputPoint = MatrixHelper.TransformPoint(testMatrix, inputPoint);

        Assert.Equal(2, inputPoint.X);
        Assert.Equal(1, inputPoint.Y);

        Assert.Equal(2, outputPoint.X);
        Assert.Equal(2, outputPoint.Y);
    }

    [Fact]
    public void ScaleAt_Same_Returns_Identity()
    {
        double scaleX = 1.0;
        double scaleY = 1.0;
        double centerX = 0.0;
        double centerY = 0.0;
        var outputMatrix = MatrixHelper.ScaleAt(scaleX, scaleY, centerX, centerY);
        Assert.True(outputMatrix.IsIdentity);
    }

    [Fact]
    public void ScaleAt_SameScale_PositiveCenter_Returns_Identity()
    {
        double scaleX = 1.0;
        double scaleY = 1.0;
        double centerX = 1.0;
        double centerY = 2.0;
        var outputMatrix = MatrixHelper.ScaleAt(scaleX, scaleY, centerX, centerY);
        Assert.True(outputMatrix.IsIdentity);
    }

    [Fact]
    public void ScaleAt_DoubleScale_PositiveCenter()
    {
        double scaleX = 2.0;
        double scaleY = 2.0;
        double centerX = 1.0;
        double centerY = 2.0;
        var outputMatrix = MatrixHelper.ScaleAt(scaleX, scaleY, centerX, centerY);

        Assert.Equal(2.0, outputMatrix.M11);
        Assert.Equal(0.0, outputMatrix.M12);
        Assert.Equal(0.0, outputMatrix.M13);
        Assert.Equal(0.0, outputMatrix.M21);
        Assert.Equal(2.0, outputMatrix.M22);
        Assert.Equal(0.0, outputMatrix.M23);
        Assert.Equal(-1.0, outputMatrix.M31);
        Assert.Equal(-2.0, outputMatrix.M32);
        Assert.Equal(1.0, outputMatrix.M33);
    }

    [Fact]
    public void ScaleAt_HalfScale_NegCenter()
    {
        double scaleX = 0.5;
        double scaleY = 0.5;
        double centerX = -1.0;
        double centerY = -2.0;
        var outputMatrix = MatrixHelper.ScaleAt(scaleX, scaleY, centerX, centerY);

        Assert.Equal(0.5, outputMatrix.M11);
        Assert.Equal(0.0, outputMatrix.M12);
        Assert.Equal(0.0, outputMatrix.M13);
        Assert.Equal(0.0, outputMatrix.M21);
        Assert.Equal(0.5, outputMatrix.M22);
        Assert.Equal(0.0, outputMatrix.M23);
        Assert.Equal(-0.5, outputMatrix.M31);
        Assert.Equal(-1.0, outputMatrix.M32);
        Assert.Equal(1.0, outputMatrix.M33);
    }

    [Fact]
    public void ScaleAndTranslate_Same_Returns_Identity()
    {
        double scaleX = 1.0;
        double scaleY = 1.0;
        double x = 0.0;
        double y = 0.0;
        var outputMatrix = MatrixHelper.ScaleAndTranslate(scaleX, scaleY, x, y);
        Assert.True(outputMatrix.IsIdentity);
    }

    [Fact]
    public void ScaleAndTranslate_DoubleScale_PositiveShift()
    {
        double scaleX = 2.0;
        double scaleY = 2.0;
        double x = 1.0;
        double y = 2.0;
        var outputMatrix = MatrixHelper.ScaleAndTranslate(scaleX, scaleY, x, y);

        Assert.Equal(2.0, outputMatrix.M11);
        Assert.Equal(0.0, outputMatrix.M12);
        Assert.Equal(0.0, outputMatrix.M13);
        Assert.Equal(0.0, outputMatrix.M21);
        Assert.Equal(2.0, outputMatrix.M22);
        Assert.Equal(0.0, outputMatrix.M23);
        Assert.Equal(1.0, outputMatrix.M31);
        Assert.Equal(2.0, outputMatrix.M32);
        Assert.Equal(1.0, outputMatrix.M33);
    }

    [Fact]
    public void ScaleAndTranslate_HalfScale_NegativeShift()
    {
        double scaleX = 0.5;
        double scaleY = 0.5;
        double x = -1.0;
        double y = -2.0;
        var outputMatrix = MatrixHelper.ScaleAndTranslate(scaleX, scaleY, x, y);

        Assert.Equal(0.5, outputMatrix.M11);
        Assert.Equal(0.0, outputMatrix.M12);
        Assert.Equal(0.0, outputMatrix.M13);
        Assert.Equal(0.0, outputMatrix.M21);
        Assert.Equal(0.5, outputMatrix.M22);
        Assert.Equal(0.0, outputMatrix.M23);
        Assert.Equal(-1.0, outputMatrix.M31);
        Assert.Equal(-2.0, outputMatrix.M32);
        Assert.Equal(1.0, outputMatrix.M33);
    }

    [Fact]
    public void ScaleAtPrepend_HalfScale_With_DoubleScale_Returns_Identity()
    {
        var halfScaleMatrix = MatrixHelper.Scale(0.5, 0.5);

        double scaleX = 2.0;
        double scaleY = 2.0;
        var outputMatrix = MatrixHelper.ScaleAtPrepend(halfScaleMatrix, scaleX, scaleY, 0, 0);

        Assert.True(outputMatrix.IsIdentity);
    }
}

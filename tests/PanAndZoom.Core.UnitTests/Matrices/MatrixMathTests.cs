// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
namespace PanAndZoom.Core.UnitTests.Matrices;


public class MatrixMathTests
{
    [Fact]
    public void Translate_Creates_Translation()
    {
        Assert.Equal(new CoreMatrix(1, 0, 0, 1, 10, -20), MatrixMath.Translate(10, -20));
        Assert.Equal(new CorePoint(11, -18), MatrixMath.Translate(10, -20).Transform(new CorePoint(1, 2)));
    }

    [Fact]
    public void TranslatePrepend_Applies_Translation_Before_Matrix()
    {
        var matrix = MatrixMath.Scale(2, 3);

        var result = MatrixMath.TranslatePrepend(matrix, 5, 7);

        Assert.Equal(MatrixMath.Translate(5, 7) * matrix, result);
        Assert.Equal(new CoreMatrix(2, 0, 0, 3, 10, 21), result);
    }

    [Fact]
    public void Scale_Creates_Scale()
    {
        Assert.Equal(new CoreMatrix(2, 0, 0, 3, 0, 0), MatrixMath.Scale(2, 3));
    }

    [Fact]
    public void ScaleAt_Keeps_Center_Fixed()
    {
        var matrix = MatrixMath.ScaleAt(2, 3, 10, 20);

        Assert.Equal(new CoreMatrix(2, 0, 0, 3, -10, -40), matrix);
        Assert.Equal(new CorePoint(10, 20), matrix.Transform(new CorePoint(10, 20)));
        Assert.Equal(new CorePoint(12, 23), matrix.Transform(new CorePoint(11, 21)));
    }

    [Fact]
    public void ScaleAtPrepend_Applies_Scale_Before_Matrix()
    {
        var matrix = MatrixMath.ScaleAndTranslate(2, 2, 10, 10);

        var result = MatrixMath.ScaleAtPrepend(matrix, 3, 3, 5, 5);

        Assert.Equal(MatrixMath.ScaleAt(3, 3, 5, 5) * matrix, result);
        // The content point (5,5) keeps its viewport position.
        Assert.Equal(matrix.Transform(new CorePoint(5, 5)), result.Transform(new CorePoint(5, 5)));
        Assert.Equal(new CoreMatrix(6, 0, 0, 6, -10, -10), result);
    }

    [Fact]
    public void ScaleAndTranslate_Creates_Scale_With_Offset()
    {
        Assert.Equal(new CoreMatrix(2, 0, 0, 3, 4, 5), MatrixMath.ScaleAndTranslate(2, 3, 4, 5));
    }

    [Fact]
    public void Skew_Uses_Tangent_Of_Angles()
    {
        var matrix = MatrixMath.Skew((float)(Math.PI / 4), 0f);

        AssertEx.Equal(new CoreMatrix(1, 1, 0, 1, 0, 0), matrix, 1e-6);

        var matrixY = MatrixMath.Skew(0f, (float)(Math.PI / 4));
        AssertEx.Equal(new CoreMatrix(1, 0, 1, 1, 0, 0), matrixY, 1e-6);
    }

    [Fact]
    public void Rotation_Creates_Rotation_Matrix()
    {
        var angle = 0.3;
        var matrix = MatrixMath.Rotation(angle);

        AssertEx.Equal(new CoreMatrix(Math.Cos(angle), Math.Sin(angle), -Math.Sin(angle), Math.Cos(angle), 0, 0), matrix);
    }

    [Fact]
    public void Rotation_90_Degrees_Maps_X_Axis_To_Y_Axis()
    {
        var matrix = MatrixMath.Rotation(Math.PI / 2);

        AssertEx.Equal(new CorePoint(0, 1), matrix.Transform(new CorePoint(1, 0)));
        AssertEx.Equal(new CorePoint(-1, 0), matrix.Transform(new CorePoint(0, 1)));
    }

    [Fact]
    public void Rotation_Around_Center_Keeps_Center_Fixed()
    {
        var matrix = MatrixMath.Rotation(Math.PI / 2, 100, 50);

        AssertEx.Equal(new CorePoint(100, 50), matrix.Transform(new CorePoint(100, 50)));
        AssertEx.Equal(new CorePoint(100, 60), matrix.Transform(new CorePoint(110, 50)));
        AssertEx.Equal(new CorePoint(90, 50), matrix.Transform(new CorePoint(100, 60)));
    }

    [Fact]
    public void Rotation_Around_Center_Equals_Translate_Rotate_Translate()
    {
        var expected = MatrixMath.Translate(-3, -4) * MatrixMath.Rotation(0.7) * MatrixMath.Translate(3, 4);

        AssertEx.Equal(expected, MatrixMath.Rotation(0.7, 3, 4));
    }

    [Fact]
    public void TransformPoint_Matches_Matrix_Transform()
    {
        var matrix = new CoreMatrix(1.5, -0.5, 0.25, 2, 10, -10);
        var point = new CorePoint(3, 4);

        Assert.Equal(matrix.Transform(point), MatrixMath.TransformPoint(matrix, point));
        AssertEx.Equal(new CorePoint(3 * 1.5 + 4 * 0.25 + 10, 3 * -0.5 + 4 * 2 - 10), MatrixMath.TransformPoint(matrix, point));
    }

    [Fact]
    public void TryDecompose_Identity()
    {
        Assert.True(MatrixMath.TryDecompose(CoreMatrix.Identity, out var sx, out var sy, out var skew, out var angle, out var tx, out var ty));

        Assert.Equal(1, sx);
        Assert.Equal(1, sy);
        Assert.Equal(0, skew);
        Assert.Equal(0, angle);
        Assert.Equal(0, tx);
        Assert.Equal(0, ty);
    }

    [Fact]
    public void TryDecompose_Scale_And_Translate()
    {
        Assert.True(MatrixMath.TryDecompose(MatrixMath.ScaleAndTranslate(2, 3, 10, 20), out var sx, out var sy, out var skew, out var angle, out var tx, out var ty));

        AssertEx.Equal(2, sx);
        AssertEx.Equal(3, sy);
        AssertEx.Equal(0, skew);
        AssertEx.Equal(0, angle);
        AssertEx.Equal(10, tx);
        AssertEx.Equal(20, ty);
    }

    [Fact]
    public void TryDecompose_Rotation()
    {
        Assert.True(MatrixMath.TryDecompose(MatrixMath.Rotation(0.8), out var sx, out var sy, out var skew, out var angle, out _, out _));

        AssertEx.Equal(1, sx);
        AssertEx.Equal(1, sy);
        AssertEx.Equal(0, skew);
        AssertEx.Equal(0.8, angle);
    }

    [Fact]
    public void TryDecompose_Returns_False_For_Singular_Matrix()
    {
        Assert.False(MatrixMath.TryDecompose(new CoreMatrix(0, 0, 0, 0, 5, 6), out var sx, out var sy, out var skew, out var angle, out var tx, out var ty));

        Assert.Equal(0, sx);
        Assert.Equal(0, sy);
        Assert.Equal(0, skew);
        Assert.Equal(0, angle);
        Assert.Equal(5, tx);
        Assert.Equal(6, ty);

        Assert.False(MatrixMath.TryDecompose(new CoreMatrix(1, 2, 2, 4, 0, 0), out _, out _, out _, out _, out _, out _));
    }

    [Theory]
    [InlineData(2.0, 3.0, 0.0, 0.0, 10.0, 20.0)]
    [InlineData(2.0, 3.0, 0.0, 0.7, 10.0, 20.0)]
    [InlineData(1.5, 0.5, 0.4, -1.2, -5.0, 7.0)]
    [InlineData(0.25, 4.0, -0.3, 2.5, 0.0, 0.0)]
    public void Compose_TryDecompose_Round_Trip_Positive_Determinant(double scaleX, double scaleY, double skew, double angle, double tx, double ty)
    {
        var matrix = MatrixMath.Compose(scaleX, scaleY, skew, angle, tx, ty);

        Assert.True(MatrixMath.TryDecompose(matrix, out var sx, out var sy, out var k, out var r, out var x, out var y));

        AssertEx.Equal(scaleX, sx);
        AssertEx.Equal(scaleY, sy);
        AssertEx.Equal(skew, k);
        AssertEx.Equal(angle, r);
        AssertEx.Equal(tx, x);
        AssertEx.Equal(ty, y);
        AssertEx.Equal(matrix, MatrixMath.Compose(sx, sy, k, r, x, y));
    }

    [Theory]
    [InlineData(-1.0, 1.0, 0.0, 0.0)]
    [InlineData(-2.0, 3.0, 0.0, 0.7)]
    public void Decompose_Compose_Round_Trip_Negative_Determinant_Without_Skew(double scaleX, double scaleY, double skew, double angle)
    {
        var matrix = MatrixMath.Compose(scaleX, scaleY, skew, angle, 3, 4);
        Assert.True(matrix.GetDeterminant() < 0);

        Assert.True(MatrixMath.TryDecompose(matrix, out var sx, out var sy, out var k, out var r, out var x, out var y));

        Assert.True(sx < 0);
        AssertEx.Equal(matrix, MatrixMath.Compose(sx, sy, k, r, x, y));
    }

    [Fact]
    public void Decompose_Compose_Round_Trip_Mirror_Matrix()
    {
        var matrix = MatrixMath.Scale(1, -1);

        Assert.True(MatrixMath.TryDecompose(matrix, out var sx, out var sy, out var k, out var r, out var x, out var y));

        AssertEx.Equal(matrix, MatrixMath.Compose(sx, sy, k, r, x, y));
    }

    [Fact]
    public void Decompose_Compose_Round_Trip_Negative_Determinant_With_Rotation_And_Skew()
    {
        var matrix = MatrixMath.Compose(-2, 3, 0.5, 0.7, 3, 4);
        Assert.True(matrix.GetDeterminant() < 0);

        Assert.True(MatrixMath.TryDecompose(matrix, out var sx, out var sy, out var k, out var r, out var x, out var y));

        AssertEx.Equal(matrix, MatrixMath.Compose(sx, sy, k, r, x, y));
    }

    [Fact]
    public void Interpolate_Between_Identical_Mirrored_Skewed_Matrices_Is_Constant()
    {
        var matrix = new CoreMatrix(-1, 0, 0.5, 1, 0, 0);

        AssertEx.Equal(matrix, MatrixMath.Interpolate(matrix, matrix, 0.5));
    }

    [Fact]
    public void Compose_Builds_Scale_Skew_Rotation_Matrix()
    {
        var angle = 0.4;
        var cos = Math.Cos(angle);
        var sin = Math.Sin(angle);

        var matrix = MatrixMath.Compose(2, 3, 0.5, angle, 7, 8);

        AssertEx.Equal(new CoreMatrix(2 * cos, 2 * sin, 3 * (0.5 * cos - sin), 3 * (0.5 * sin + cos), 7, 8), matrix);
    }

    [Fact]
    public void Compose_Without_Skew_Or_Rotation_Equals_ScaleAndTranslate()
    {
        Assert.Equal(MatrixMath.ScaleAndTranslate(2, 3, 4, 5), MatrixMath.Compose(2, 3, 0, 0, 4, 5));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-0.5)]
    public void Interpolate_At_Or_Before_Start_Returns_From(double progress)
    {
        var from = new CoreMatrix(1, 2, 3, 4, 5, 6);
        var to = MatrixMath.Scale(3, 3);

        Assert.Equal(from, MatrixMath.Interpolate(from, to, progress));
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(1.5)]
    public void Interpolate_At_Or_After_End_Returns_To(double progress)
    {
        var from = MatrixMath.Scale(3, 3);
        var to = new CoreMatrix(1, 2, 3, 4, 5, 6);

        Assert.Equal(to, MatrixMath.Interpolate(from, to, progress));
    }

    [Fact]
    public void Interpolate_Midpoint_Of_Scale_And_Translation()
    {
        var from = MatrixMath.ScaleAndTranslate(1, 1, 0, 0);
        var to = MatrixMath.ScaleAndTranslate(3, 5, 10, -20);

        var result = MatrixMath.Interpolate(from, to, 0.5);

        AssertEx.Equal(MatrixMath.ScaleAndTranslate(2, 3, 5, -10), result);
    }

    [Fact]
    public void Interpolate_Quarter_Of_Scale_And_Translation()
    {
        var from = MatrixMath.ScaleAndTranslate(1, 1, 0, 0);
        var to = MatrixMath.ScaleAndTranslate(5, 5, 100, 200);

        var result = MatrixMath.Interpolate(from, to, 0.25);

        AssertEx.Equal(MatrixMath.ScaleAndTranslate(2, 2, 25, 50), result);
    }

    [Fact]
    public void Interpolate_Rotation_Interpolates_Angle()
    {
        var from = MatrixMath.Rotation(0);
        var to = MatrixMath.Rotation(Math.PI / 2);

        AssertEx.Equal(MatrixMath.Rotation(Math.PI / 4), MatrixMath.Interpolate(from, to, 0.5));
    }

    [Fact]
    public void Interpolate_Rotation_Uses_Shortest_Path()
    {
        var degrees = Math.PI / 180.0;
        var from = MatrixMath.Rotation(170 * degrees);
        var to = MatrixMath.Rotation(-170 * degrees);

        var result = MatrixMath.Interpolate(from, to, 0.5);

        // The shortest path goes through 180 degrees, not through 0 degrees.
        AssertEx.Equal(MatrixMath.Rotation(Math.PI), result);
        Assert.True(result.M11 < -0.99);
    }

    [Fact]
    public void Interpolate_Rotation_Uses_Shortest_Path_Negative_Direction()
    {
        var degrees = Math.PI / 180.0;
        var from = MatrixMath.Rotation(-170 * degrees);
        var to = MatrixMath.Rotation(170 * degrees);

        var result = MatrixMath.Interpolate(from, to, 0.25);

        AssertEx.Equal(MatrixMath.Rotation(-175 * degrees), result);
    }

    [Fact]
    public void Interpolate_Falls_Back_To_Linear_For_Non_Invertible_From()
    {
        var from = new CoreMatrix(0, 0, 0, 0, 0, 0);
        var to = new CoreMatrix(2, 0, 0, 2, 10, 20);

        var result = MatrixMath.Interpolate(from, to, 0.25);

        AssertEx.Equal(new CoreMatrix(0.5, 0, 0, 0.5, 2.5, 5), result);
    }

    [Fact]
    public void Interpolate_Falls_Back_To_Linear_For_Non_Invertible_To()
    {
        var from = new CoreMatrix(1, 2, 3, 4, 5, 6);
        var to = new CoreMatrix(1, 2, 2, 4, 1, 2);

        var result = MatrixMath.Interpolate(from, to, 0.5);

        AssertEx.Equal(new CoreMatrix(1, 2, 2.5, 4, 3, 4), result);
    }

    [Fact]
    public void Interpolate_Between_Equal_Matrices_Returns_Same_Matrix()
    {
        var matrix = MatrixMath.Compose(2, 1.5, 0.2, 0.6, 3, 4);

        AssertEx.Equal(matrix, MatrixMath.Interpolate(matrix, matrix, 0.37));
    }
}

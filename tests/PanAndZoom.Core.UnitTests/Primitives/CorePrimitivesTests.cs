// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
namespace PanAndZoom.Core.UnitTests.Primitives;

public class CorePointTests
{
    [Fact]
    public void Constructor_Sets_Coordinates()
    {
        var point = new CorePoint(1.5, -2.5);

        Assert.Equal(1.5, point.X);
        Assert.Equal(-2.5, point.Y);
    }

    [Fact]
    public void Default_Is_Origin()
    {
        var point = default(CorePoint);

        Assert.Equal(0, point.X);
        Assert.Equal(0, point.Y);
        Assert.Equal(new CorePoint(0, 0), point);
    }

    [Fact]
    public void Transform_Uses_Matrix_Transform()
    {
        var matrix = new CoreMatrix(2, 0, 0, 3, 10, 20);
        var point = new CorePoint(4, 5);

        Assert.Equal(new CorePoint(18, 35), point.Transform(matrix));
        Assert.Equal(matrix.Transform(point), point.Transform(matrix));
    }

    [Fact]
    public void Equality_Is_Value_Based()
    {
        Assert.Equal(new CorePoint(1, 2), new CorePoint(1, 2));
        Assert.NotEqual(new CorePoint(1, 2), new CorePoint(2, 1));
        Assert.True(new CorePoint(1, 2) == new CorePoint(1, 2));
    }
}

public class CoreSizeTests
{
    [Fact]
    public void Constructor_Sets_Dimensions()
    {
        var size = new CoreSize(10, 20);

        Assert.Equal(10, size.Width);
        Assert.Equal(20, size.Height);
    }

    [Fact]
    public void Equality_Is_Value_Based()
    {
        Assert.Equal(new CoreSize(10, 20), new CoreSize(10, 20));
        Assert.NotEqual(new CoreSize(10, 20), new CoreSize(20, 10));
        Assert.Equal(default, new CoreSize(0, 0));
    }
}

public class CoreVectorTests
{
    [Fact]
    public void Constructor_Sets_Components()
    {
        var vector = new CoreVector(-3, 7);

        Assert.Equal(-3, vector.X);
        Assert.Equal(7, vector.Y);
    }

    [Theory]
    [InlineData(3, 4, 5)]
    [InlineData(0, 0, 0)]
    [InlineData(-6, -8, 10)]
    [InlineData(0, -2, 2)]
    public void Length_Is_Euclidean(double x, double y, double expected)
    {
        Assert.Equal(expected, new CoreVector(x, y).Length, 12);
    }
}

public class CoreThicknessTests
{
    [Fact]
    public void Constructor_Sets_All_Sides()
    {
        var thickness = new CoreThickness(1, 2, 3, 4);

        Assert.Equal(1, thickness.Left);
        Assert.Equal(2, thickness.Top);
        Assert.Equal(3, thickness.Right);
        Assert.Equal(4, thickness.Bottom);
    }

    [Fact]
    public void Uniform_Constructor_Sets_All_Sides_To_Same_Value()
    {
        var thickness = new CoreThickness(5);

        Assert.Equal(new CoreThickness(5, 5, 5, 5), thickness);
    }
}

public class CoreRectTests
{
    [Fact]
    public void Constructor_Sets_Position_And_Size()
    {
        var rect = new CoreRect(1, 2, 3, 4);

        Assert.Equal(1, rect.X);
        Assert.Equal(2, rect.Y);
        Assert.Equal(3, rect.Width);
        Assert.Equal(4, rect.Height);
    }

    [Fact]
    public void Edge_And_Corner_Properties_Are_Derived()
    {
        var rect = new CoreRect(1, 2, 3, 4);

        Assert.Equal(1, rect.Left);
        Assert.Equal(2, rect.Top);
        Assert.Equal(4, rect.Right);
        Assert.Equal(6, rect.Bottom);
        Assert.Equal(new CorePoint(1, 2), rect.TopLeft);
        Assert.Equal(new CorePoint(4, 2), rect.TopRight);
        Assert.Equal(new CorePoint(4, 6), rect.BottomRight);
        Assert.Equal(new CorePoint(1, 6), rect.BottomLeft);
        Assert.Equal(new CorePoint(1, 2), rect.Position);
        Assert.Equal(new CoreSize(3, 4), rect.Size);
    }

    [Fact]
    public void Position_Size_Constructor()
    {
        var rect = new CoreRect(new CorePoint(5, 6), new CoreSize(7, 8));

        Assert.Equal(new CoreRect(5, 6, 7, 8), rect);
    }

    [Fact]
    public void TopLeft_BottomRight_Constructor()
    {
        var rect = new CoreRect(new CorePoint(5, 6), new CorePoint(15, 26));

        Assert.Equal(new CoreRect(5, 6, 10, 20), rect);
    }

    [Theory]
    [InlineData(1, 2, true)] // top left corner (inclusive)
    [InlineData(4, 6, true)] // bottom right corner (inclusive)
    [InlineData(2.5, 4, true)] // inside
    [InlineData(4.0001, 4, false)] // right of rect
    [InlineData(0.9999, 4, false)] // left of rect
    [InlineData(2, 1.9999, false)] // above rect
    [InlineData(2, 6.0001, false)] // below rect
    public void Contains_Point_Is_Edge_Inclusive(double x, double y, bool expected)
    {
        var rect = new CoreRect(1, 2, 3, 4);

        Assert.Equal(expected, rect.Contains(new CorePoint(x, y)));
    }

    [Fact]
    public void Contains_Rect_Requires_Full_Containment()
    {
        var rect = new CoreRect(0, 0, 100, 100);

        Assert.True(rect.Contains(new CoreRect(10, 10, 20, 20)));
        Assert.True(rect.Contains(rect));
        Assert.False(rect.Contains(new CoreRect(90, 90, 20, 20)));
        Assert.False(rect.Contains(new CoreRect(-1, 0, 10, 10)));
        Assert.False(rect.Contains(new CoreRect(-10, -10, 200, 200)));
    }

    [Fact]
    public void Intersects_Detects_Overlap()
    {
        var rect = new CoreRect(0, 0, 100, 100);

        Assert.True(rect.Intersects(new CoreRect(50, 50, 100, 100)));
        Assert.True(rect.Intersects(new CoreRect(10, 10, 10, 10)));
        Assert.True(rect.Intersects(new CoreRect(-10, -10, 200, 200)));
        Assert.False(rect.Intersects(new CoreRect(200, 200, 10, 10)));
    }

    [Fact]
    public void Intersects_Is_False_For_Touching_Edges()
    {
        var rect = new CoreRect(0, 0, 100, 100);

        Assert.False(rect.Intersects(new CoreRect(100, 0, 10, 10)));
        Assert.False(rect.Intersects(new CoreRect(0, 100, 10, 10)));
        Assert.False(rect.Intersects(new CoreRect(-10, 0, 10, 10)));
        Assert.False(rect.Intersects(new CoreRect(0, -10, 10, 10)));
    }

    [Fact]
    public void Intersect_Returns_Overlapping_Region()
    {
        var rect = new CoreRect(0, 0, 100, 100);

        Assert.Equal(new CoreRect(50, 60, 50, 40), rect.Intersect(new CoreRect(50, 60, 100, 100)));
        Assert.Equal(new CoreRect(10, 10, 10, 10), rect.Intersect(new CoreRect(10, 10, 10, 10)));
        Assert.Equal(rect, rect.Intersect(new CoreRect(-10, -10, 200, 200)));
    }

    [Fact]
    public void Intersect_Returns_Default_When_Disjoint_Or_Touching()
    {
        var rect = new CoreRect(0, 0, 100, 100);

        Assert.Equal(default, rect.Intersect(new CoreRect(200, 200, 10, 10)));
        Assert.Equal(default, rect.Intersect(new CoreRect(100, 0, 10, 10)));
    }

    [Fact]
    public void TransformToAABB_Identity_Returns_Same_Rect()
    {
        var rect = new CoreRect(1, 2, 3, 4);

        Assert.Equal(rect, rect.TransformToAABB(CoreMatrix.Identity));
    }

    [Fact]
    public void TransformToAABB_Scale_And_Translate()
    {
        var rect = new CoreRect(1, 1, 2, 2);

        var result = rect.TransformToAABB(new CoreMatrix(2, 0, 0, 3, 5, 7));

        Assert.Equal(new CoreRect(7, 10, 4, 6), result);
    }

    [Fact]
    public void TransformToAABB_Negative_Scale_Normalizes_Bounds()
    {
        var rect = new CoreRect(0, 0, 10, 20);

        var result = rect.TransformToAABB(MatrixMath.Scale(-1, -2));

        AssertEx.Equal(new CoreRect(-10, -40, 10, 40), result);
    }

    [Fact]
    public void TransformToAABB_Rotation_Returns_Bounding_Box()
    {
        var rect = new CoreRect(0, 0, 10, 20);

        var result = rect.TransformToAABB(MatrixMath.Rotation(Math.PI / 2));

        AssertEx.Equal(new CoreRect(-20, 0, 20, 10), result);
    }

    [Fact]
    public void TransformToAABB_45_Degree_Rotation_Grows_Box()
    {
        var rect = new CoreRect(-1, -1, 2, 2);

        var result = rect.TransformToAABB(MatrixMath.Rotation(Math.PI / 4));

        var half = Math.Sqrt(2);
        AssertEx.Equal(new CoreRect(-half, -half, 2 * half, 2 * half), result);
    }
}

public class CoreMatrixTests
{
    [Fact]
    public void Identity_Has_Expected_Elements()
    {
        var identity = CoreMatrix.Identity;

        Assert.Equal(new CoreMatrix(1, 0, 0, 1, 0, 0), identity);
        Assert.True(identity.IsIdentity);
        Assert.True(identity.HasInverse);
        Assert.Equal(1, identity.GetDeterminant());
    }

    [Fact]
    public void Default_Is_Not_Identity_And_Not_Invertible()
    {
        var matrix = default(CoreMatrix);

        Assert.False(matrix.IsIdentity);
        Assert.False(matrix.HasInverse);
    }

    [Fact]
    public void IsIdentity_Is_False_For_Translation()
    {
        Assert.False(new CoreMatrix(1, 0, 0, 1, 1, 0).IsIdentity);
    }

    [Fact]
    public void Constructor_Sets_Elements()
    {
        var matrix = new CoreMatrix(1, 2, 3, 4, 5, 6);

        Assert.Equal(1, matrix.M11);
        Assert.Equal(2, matrix.M12);
        Assert.Equal(3, matrix.M21);
        Assert.Equal(4, matrix.M22);
        Assert.Equal(5, matrix.M31);
        Assert.Equal(6, matrix.M32);
    }

    [Fact]
    public void Transform_Uses_Row_Vector_Convention()
    {
        var matrix = new CoreMatrix(1, 2, 3, 4, 5, 6);

        // [x y 1] * M = (x*M11 + y*M21 + M31, x*M12 + y*M22 + M32)
        Assert.Equal(new CorePoint(1 * 1 + 2 * 3 + 5, 1 * 2 + 2 * 4 + 6), matrix.Transform(new CorePoint(1, 2)));
    }

    [Fact]
    public void Multiply_Applies_Left_Operand_First()
    {
        var translate = MatrixMath.Translate(10, 0);
        var scale = MatrixMath.Scale(2, 2);

        var translateThenScale = translate * scale;
        var scaleThenTranslate = scale * translate;

        Assert.Equal(new CoreMatrix(2, 0, 0, 2, 20, 0), translateThenScale);
        Assert.Equal(new CoreMatrix(2, 0, 0, 2, 10, 0), scaleThenTranslate);
        Assert.Equal(new CorePoint(22, 0), translateThenScale.Transform(new CorePoint(1, 0)));
        Assert.Equal(new CorePoint(12, 0), scaleThenTranslate.Transform(new CorePoint(1, 0)));
    }

    [Fact]
    public void Multiply_Matches_Sequential_Transform()
    {
        var a = new CoreMatrix(1.5, 0.25, -0.5, 2, 3, -4);
        var b = new CoreMatrix(0.5, -1, 2, 0.75, -6, 8);
        var point = new CorePoint(3, -7);

        AssertEx.Equal(b.Transform(a.Transform(point)), (a * b).Transform(point));
    }

    [Fact]
    public void Multiply_By_Identity_Returns_Same_Matrix()
    {
        var matrix = new CoreMatrix(1, 2, 3, 4, 5, 6);

        Assert.Equal(matrix, matrix * CoreMatrix.Identity);
        Assert.Equal(matrix, CoreMatrix.Identity * matrix);
    }

    [Fact]
    public void GetDeterminant_Returns_M11M22_Minus_M12M21()
    {
        Assert.Equal(1 * 4 - 2 * 3, new CoreMatrix(1, 2, 3, 4, 5, 6).GetDeterminant());
    }

    [Fact]
    public void HasInverse_Is_False_For_Singular_Matrices()
    {
        Assert.False(MatrixMath.Scale(0, 1).HasInverse);
        Assert.False(new CoreMatrix(1, 2, 2, 4, 0, 0).HasInverse);
        Assert.False(new CoreMatrix(1e-8, 0, 0, 1e-8, 0, 0).HasInverse);
        Assert.True(new CoreMatrix(1e-7, 0, 0, 1e-7, 0, 0).HasInverse);
    }

    [Fact]
    public void TryInvert_Returns_Inverse()
    {
        var matrix = new CoreMatrix(2, 1, -1, 3, 10, -20);

        Assert.True(matrix.TryInvert(out var inverted));
        AssertEx.Equal(CoreMatrix.Identity, matrix * inverted);
        AssertEx.Equal(CoreMatrix.Identity, inverted * matrix);

        var point = new CorePoint(7, -3);
        AssertEx.Equal(point, inverted.Transform(matrix.Transform(point)));
    }

    [Fact]
    public void TryInvert_Returns_False_And_Default_For_Singular_Matrix()
    {
        var matrix = new CoreMatrix(1, 2, 2, 4, 5, 6);

        Assert.False(matrix.TryInvert(out var inverted));
        Assert.Equal(default, inverted);
    }

    [Fact]
    public void Invert_Returns_Inverse_Of_Scale_And_Translate()
    {
        var matrix = MatrixMath.ScaleAndTranslate(2, 4, 10, 20);

        var inverted = matrix.Invert();

        AssertEx.Equal(new CoreMatrix(0.5, 0, 0, 0.25, -5, -5), inverted);
    }

    [Fact]
    public void Invert_Throws_For_Singular_Matrix()
    {
        var matrix = MatrixMath.Scale(0, 0);

        Assert.Throws<InvalidOperationException>(() => matrix.Invert());
    }
}

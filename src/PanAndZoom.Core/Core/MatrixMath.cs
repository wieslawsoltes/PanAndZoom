// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using static System.Math;

namespace PanAndZoom.Core;

/// <summary>
/// Factory and helper methods for <see cref="CoreMatrix"/>.
/// </summary>
public static class MatrixMath
{
    /// <summary>
    /// Creates a translation matrix using the specified offsets.
    /// </summary>
    /// <param name="offsetX">X-coordinate offset.</param>
    /// <param name="offsetY">Y-coordinate offset.</param>
    /// <returns>The created translation matrix.</returns>
    public static CoreMatrix Translate(double offsetX, double offsetY)
    {
        return new CoreMatrix(1.0, 0.0, 0.0, 1.0, offsetX, offsetY);
    }

    /// <summary>
    /// Prepends a translation to the provided matrix.
    /// </summary>
    /// <param name="matrix">The matrix to prepend translation.</param>
    /// <param name="offsetX">X-coordinate offset.</param>
    /// <param name="offsetY">Y-coordinate offset.</param>
    /// <returns>The resulting matrix.</returns>
    public static CoreMatrix TranslatePrepend(CoreMatrix matrix, double offsetX, double offsetY)
    {
        return Translate(offsetX, offsetY) * matrix;
    }

    /// <summary>
    /// Creates a matrix that scales along the x-axis and y-axis.
    /// </summary>
    /// <param name="scaleX">Scaling factor that is applied along the x-axis.</param>
    /// <param name="scaleY">Scaling factor that is applied along the y-axis.</param>
    /// <returns>The created scaling matrix.</returns>
    public static CoreMatrix Scale(double scaleX, double scaleY)
    {
        return new CoreMatrix(scaleX, 0, 0, scaleY, 0.0, 0.0);
    }

    /// <summary>
    /// Creates a matrix that is scaling from a specified center.
    /// </summary>
    /// <param name="scaleX">Scaling factor that is applied along the x-axis.</param>
    /// <param name="scaleY">Scaling factor that is applied along the y-axis.</param>
    /// <param name="centerX">The center X-coordinate of the scaling.</param>
    /// <param name="centerY">The center Y-coordinate of the scaling.</param>
    /// <returns>The created scaling matrix.</returns>
    public static CoreMatrix ScaleAt(double scaleX, double scaleY, double centerX, double centerY)
    {
        return new CoreMatrix(scaleX, 0, 0, scaleY, centerX - (scaleX * centerX), centerY - (scaleY * centerY));
    }

    /// <summary>
    /// Prepends a scale around a center point to the provided matrix.
    /// </summary>
    /// <param name="matrix">The matrix to prepend scale.</param>
    /// <param name="scaleX">Scaling factor that is applied along the x-axis.</param>
    /// <param name="scaleY">Scaling factor that is applied along the y-axis.</param>
    /// <param name="centerX">The center X-coordinate of the scaling.</param>
    /// <param name="centerY">The center Y-coordinate of the scaling.</param>
    /// <returns>The resulting matrix.</returns>
    public static CoreMatrix ScaleAtPrepend(CoreMatrix matrix, double scaleX, double scaleY, double centerX, double centerY)
    {
        return ScaleAt(scaleX, scaleY, centerX, centerY) * matrix;
    }

    /// <summary>
    /// Creates a translation and scale matrix.
    /// </summary>
    /// <param name="scaleX">Scaling factor that is applied along the x-axis.</param>
    /// <param name="scaleY">Scaling factor that is applied along the y-axis.</param>
    /// <param name="offsetX">X-coordinate offset.</param>
    /// <param name="offsetY">Y-coordinate offset.</param>
    /// <returns>The created translation and scale matrix.</returns>
    public static CoreMatrix ScaleAndTranslate(double scaleX, double scaleY, double offsetX, double offsetY)
    {
        return new CoreMatrix(scaleX, 0.0, 0.0, scaleY, offsetX, offsetY);
    }

    /// <summary>
    /// Creates a skew matrix.
    /// </summary>
    /// <param name="angleX">Angle of skew along the X-axis in radians.</param>
    /// <param name="angleY">Angle of skew along the Y-axis in radians.</param>
    /// <returns>The created skew matrix.</returns>
    public static CoreMatrix Skew(float angleX, float angleY)
    {
        return new CoreMatrix(1.0, Tan(angleX), Tan(angleY), 1.0, 0.0, 0.0);
    }

    /// <summary>
    /// Creates a matrix that rotates.
    /// </summary>
    /// <param name="radians">Angle of rotation in radians.</param>
    /// <returns>The created rotation matrix.</returns>
    public static CoreMatrix Rotation(double radians)
    {
        var cos = Cos(radians);
        var sin = Sin(radians);
        return new CoreMatrix(cos, sin, -sin, cos, 0, 0);
    }

    /// <summary>
    /// Creates a matrix that rotates about a specified center.
    /// </summary>
    /// <param name="angle">Angle of rotation in radians.</param>
    /// <param name="centerX">The center X-coordinate of the rotation.</param>
    /// <param name="centerY">The center Y-coordinate of the rotation.</param>
    /// <returns>The created rotation matrix.</returns>
    public static CoreMatrix Rotation(double angle, double centerX, double centerY)
    {
        return Translate(-centerX, -centerY) * Rotation(angle) * Translate(centerX, centerY);
    }

    /// <summary>
    /// Transforms a point by the matrix.
    /// </summary>
    /// <param name="matrix">The matrix to use as a transformation matrix.</param>
    /// <param name="point">The original point to apply the transformation.</param>
    /// <returns>The result of the transformation for the input point.</returns>
    public static CorePoint TransformPoint(CoreMatrix matrix, CorePoint point)
    {
        return new CorePoint(
            (point.X * matrix.M11) + (point.Y * matrix.M21) + matrix.M31,
            (point.X * matrix.M12) + (point.Y * matrix.M22) + matrix.M32);
    }

    /// <summary>
    /// Decomposes an affine matrix into scale, skew, rotation and translation components.
    /// </summary>
    /// <param name="matrix">The matrix to decompose.</param>
    /// <param name="scaleX">The x-axis scale.</param>
    /// <param name="scaleY">The y-axis scale.</param>
    /// <param name="skew">The skew factor.</param>
    /// <param name="angle">The rotation angle in radians.</param>
    /// <param name="translateX">The x-axis translation.</param>
    /// <param name="translateY">The y-axis translation.</param>
    /// <returns>True when the matrix could be decomposed (it is invertible).</returns>
    public static bool TryDecompose(
        CoreMatrix matrix,
        out double scaleX,
        out double scaleY,
        out double skew,
        out double angle,
        out double translateX,
        out double translateY)
    {
        translateX = matrix.M31;
        translateY = matrix.M32;

        var determinant = matrix.GetDeterminant();
        scaleX = Sqrt(matrix.M11 * matrix.M11 + matrix.M12 * matrix.M12);

        if (Abs(determinant) < 2.220446049250313E-15 || scaleX == 0)
        {
            scaleY = 0;
            skew = 0;
            angle = 0;
            return false;
        }

        var a = matrix.M11 / scaleX;
        var b = matrix.M12 / scaleX;

        // Mirrored matrices (negative determinant) are represented with a negative x scale.
        // The flip must happen before the skew is extracted so the skew keeps its sign.
        if (determinant < 0)
        {
            scaleX = -scaleX;
            a = -a;
            b = -b;
        }

        skew = a * matrix.M21 + b * matrix.M22;
        var c = matrix.M21 - a * skew;
        var d = matrix.M22 - b * skew;

        scaleY = Sqrt(c * c + d * d);
        skew /= scaleY;

        angle = Atan2(b, a);
        return true;
    }

    /// <summary>
    /// Composes an affine matrix from scale, skew, rotation and translation components.
    /// </summary>
    /// <param name="scaleX">The x-axis scale.</param>
    /// <param name="scaleY">The y-axis scale.</param>
    /// <param name="skew">The skew factor.</param>
    /// <param name="angle">The rotation angle in radians.</param>
    /// <param name="translateX">The x-axis translation.</param>
    /// <param name="translateY">The y-axis translation.</param>
    /// <returns>The composed matrix.</returns>
    public static CoreMatrix Compose(double scaleX, double scaleY, double skew, double angle, double translateX, double translateY)
    {
        var cos = Cos(angle);
        var sin = Sin(angle);
        return new CoreMatrix(
            scaleX * cos,
            scaleX * sin,
            scaleY * (skew * cos - sin),
            scaleY * (skew * sin + cos),
            translateX,
            translateY);
    }

    /// <summary>
    /// Interpolates between two affine matrices by interpolating their decomposed components.
    /// </summary>
    /// <param name="from">The start matrix.</param>
    /// <param name="to">The end matrix.</param>
    /// <param name="progress">The progress in the range [0, 1].</param>
    /// <returns>The interpolated matrix.</returns>
    public static CoreMatrix Interpolate(CoreMatrix from, CoreMatrix to, double progress)
    {
        if (progress <= 0.0)
        {
            return from;
        }

        if (progress >= 1.0)
        {
            return to;
        }

        if (!TryDecompose(from, out var sx1, out var sy1, out var k1, out var r1, out var tx1, out var ty1)
            || !TryDecompose(to, out var sx2, out var sy2, out var k2, out var r2, out var tx2, out var ty2))
        {
            return new CoreMatrix(
                Lerp(from.M11, to.M11, progress),
                Lerp(from.M12, to.M12, progress),
                Lerp(from.M21, to.M21, progress),
                Lerp(from.M22, to.M22, progress),
                Lerp(from.M31, to.M31, progress),
                Lerp(from.M32, to.M32, progress));
        }

        // Rotate along the shortest path.
        var deltaAngle = r2 - r1;
        while (deltaAngle > PI) deltaAngle -= 2 * PI;
        while (deltaAngle < -PI) deltaAngle += 2 * PI;

        return Compose(
            Lerp(sx1, sx2, progress),
            Lerp(sy1, sy2, progress),
            Lerp(k1, k2, progress),
            r1 + deltaAngle * progress,
            Lerp(tx1, tx2, progress),
            Lerp(ty1, ty2, progress));

        static double Lerp(double a, double b, double t) => a + (b - a) * t;
    }
}

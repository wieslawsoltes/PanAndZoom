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
}

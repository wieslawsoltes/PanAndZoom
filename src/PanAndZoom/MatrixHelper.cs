// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

namespace Avalonia.Controls.PanAndZoom;

/// <summary>
/// Avalonia Matrix helper methods.
/// </summary>
/// <remarks>
/// Thin Avalonia wrapper over the UI framework independent <see cref="MatrixMath"/> helpers.
/// </remarks>
public static class MatrixHelper
{
    /// <summary>
    /// Creates a translation matrix using the specified offsets.
    /// </summary>
    /// <param name="offsetX">X-coordinate offset.</param>
    /// <param name="offsetY">Y-coordinate offset.</param>
    /// <returns>The created translation matrix.</returns>
    public static Matrix Translate(double offsetX, double offsetY)
    {
        return MatrixMath.Translate(offsetX, offsetY).ToAvalonia();
    }

    /// <summary>
    /// Prepends a translation around the center of provided matrix.
    /// </summary>
    /// <param name="matrix">The matrix to prepend translation.</param>
    /// <param name="offsetX">X-coordinate offset.</param>
    /// <param name="offsetY">Y-coordinate offset.</param>
    /// <returns>The created translation matrix.</returns>
    public static Matrix TranslatePrepend(Matrix matrix, double offsetX, double offsetY)
    {
        return Translate(offsetX, offsetY) * matrix;
    }

    /// <summary>
    /// Creates a matrix that scales along the x-axis and y-axis.
    /// </summary>
    /// <param name="scaleX">Scaling factor that is applied along the x-axis.</param>
    /// <param name="scaleY">Scaling factor that is applied along the y-axis.</param>
    /// <returns>The created scaling matrix.</returns>
    public static Matrix Scale(double scaleX, double scaleY)
    {
        return MatrixMath.Scale(scaleX, scaleY).ToAvalonia();
    }

    /// <summary>
    /// Creates a matrix that is scaling from a specified center.
    /// </summary>
    /// <param name="scaleX">Scaling factor that is applied along the x-axis.</param>
    /// <param name="scaleY">Scaling factor that is applied along the y-axis.</param>
    /// <param name="centerX">The center X-coordinate of the scaling.</param>
    /// <param name="centerY">The center Y-coordinate of the scaling.</param>
    /// <returns>The created scaling matrix.</returns>
    public static Matrix ScaleAt(double scaleX, double scaleY, double centerX, double centerY)
    {
        return MatrixMath.ScaleAt(scaleX, scaleY, centerX, centerY).ToAvalonia();
    }

    /// <summary>
    /// Prepends a scale around the center of provided matrix.
    /// </summary>
    /// <param name="matrix">The matrix to prepend scale.</param>
    /// <param name="scaleX">Scaling factor that is applied along the x-axis.</param>
    /// <param name="scaleY">Scaling factor that is applied along the y-axis.</param>
    /// <param name="centerX">The center X-coordinate of the scaling.</param>
    /// <param name="centerY">The center Y-coordinate of the scaling.</param>
    /// <returns>The created scaling matrix.</returns>
    public static Matrix ScaleAtPrepend(Matrix matrix, double scaleX, double scaleY, double centerX, double centerY)
    {
        return ScaleAt(scaleX, scaleY, centerX, centerY) * matrix;
    }

    /// <summary>
    /// Creates a translation and scale matrix using the specified offsets and scales along the x-axis and y-axis.
    /// </summary>
    /// <param name="scaleX">Scaling factor that is applied along the x-axis.</param>
    /// <param name="scaleY">Scaling factor that is applied along the y-axis.</param>
    /// <param name="offsetX">X-coordinate offset.</param>
    /// <param name="offsetY">Y-coordinate offset.</param>
    /// <returns>The created translation and scale matrix.</returns>
    public static Matrix ScaleAndTranslate(double scaleX, double scaleY, double offsetX, double offsetY)
    {
        return MatrixMath.ScaleAndTranslate(scaleX, scaleY, offsetX, offsetY).ToAvalonia();
    }

    /// <summary>
    /// Creates a skew matrix.
    /// </summary>
    /// <param name="angleX">Angle of skew along the X-axis in radians.</param>
    /// <param name="angleY">Angle of skew along the Y-axis in radians.</param>
    /// <returns>When the method completes, contains the created skew matrix.</returns>
    public static Matrix Skew(float angleX, float angleY)
    {
        return MatrixMath.Skew(angleX, angleY).ToAvalonia();
    }

    /// <summary>
    /// Creates a matrix that rotates.
    /// </summary>
    /// <param name="radians">Angle of rotation in radians. Angles are measured clockwise when looking along the rotation axis.</param>
    /// <returns>The created rotation matrix.</returns>
    public static Matrix Rotation(double radians)
    {
        return MatrixMath.Rotation(radians).ToAvalonia();
    }

    /// <summary>
    /// Creates a matrix that rotates about a specified center.
    /// </summary>
    /// <param name="angle">Angle of rotation in radians.</param>
    /// <param name="centerX">The center X-coordinate of the rotation.</param>
    /// <param name="centerY">The center Y-coordinate of the rotation.</param>
    /// <returns>The created rotation matrix.</returns>
    public static Matrix Rotation(double angle, double centerX, double centerY)
    {
        return Translate(-centerX, -centerY) * Rotation(angle) * Translate(centerX, centerY);
    }

    /// <summary>
    /// Creates a matrix that rotates about a specified center.
    /// </summary>
    /// <param name="angle">Angle of rotation in radians.</param>
    /// <param name="center">The center of the rotation.</param>
    /// <returns>The created rotation matrix.</returns>
    public static Matrix Rotation(double angle, Vector center)
    {
        return Translate(-center.X, -center.Y) * Rotation(angle) * Translate(center.X, center.Y);
    }

    /// <summary>
    /// Transforms a point by this matrix.
    /// </summary>
    /// <param name="matrix">The matrix to use as a transformation matrix.</param>
    /// <param name="point">>The original point to apply the transformation.</param>
    /// <returns>The result of the transformation for the input point.</returns>
    public static Point TransformPoint(Matrix matrix, Point point)
    {
        return MatrixMath.TransformPoint(matrix.ToCore(), point.ToCore()).ToAvalonia();
    }
}

// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
namespace PanAndZoom;

/// <summary>
/// WinUI <see cref="Matrix"/> helper methods.
/// </summary>
/// <remarks>
/// Thin WinUI wrapper over the UI framework independent <see cref="MatrixMath"/> helpers.
/// WinUI matrices use the same row-vector convention as Avalonia: <c>Multiply(a, b)</c> applies <c>a</c> first.
/// </remarks>
public static class MatrixHelper
{
    /// <summary>
    /// Multiplies two matrices (applies <paramref name="value1"/> first and <paramref name="value2"/> second).
    /// </summary>
    /// <param name="value1">The first matrix.</param>
    /// <param name="value2">The second matrix.</param>
    /// <returns>The product matrix.</returns>
    public static Matrix Multiply(Matrix value1, Matrix value2)
    {
        return (value1.ToCore() * value2.ToCore()).ToUno();
    }

    /// <summary>
    /// Tries to invert a matrix.
    /// </summary>
    /// <param name="matrix">The matrix.</param>
    /// <param name="inverted">The inverted matrix.</param>
    /// <returns>True when the matrix was inverted.</returns>
    public static bool TryInvert(Matrix matrix, out Matrix inverted)
    {
        var result = matrix.ToCore().TryInvert(out var coreInverted);
        inverted = coreInverted.ToUno();
        return result;
    }

    /// <summary>
    /// Creates a translation matrix using the specified offsets.
    /// </summary>
    /// <param name="offsetX">X-coordinate offset.</param>
    /// <param name="offsetY">Y-coordinate offset.</param>
    /// <returns>The created translation matrix.</returns>
    public static Matrix Translate(double offsetX, double offsetY)
    {
        return MatrixMath.Translate(offsetX, offsetY).ToUno();
    }

    /// <summary>
    /// Prepends a translation to the provided matrix.
    /// </summary>
    /// <param name="matrix">The matrix to prepend translation.</param>
    /// <param name="offsetX">X-coordinate offset.</param>
    /// <param name="offsetY">Y-coordinate offset.</param>
    /// <returns>The resulting matrix.</returns>
    public static Matrix TranslatePrepend(Matrix matrix, double offsetX, double offsetY)
    {
        return MatrixMath.TranslatePrepend(matrix.ToCore(), offsetX, offsetY).ToUno();
    }

    /// <summary>
    /// Creates a matrix that scales along the x-axis and y-axis.
    /// </summary>
    /// <param name="scaleX">Scaling factor that is applied along the x-axis.</param>
    /// <param name="scaleY">Scaling factor that is applied along the y-axis.</param>
    /// <returns>The created scaling matrix.</returns>
    public static Matrix Scale(double scaleX, double scaleY)
    {
        return MatrixMath.Scale(scaleX, scaleY).ToUno();
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
        return MatrixMath.ScaleAt(scaleX, scaleY, centerX, centerY).ToUno();
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
    public static Matrix ScaleAtPrepend(Matrix matrix, double scaleX, double scaleY, double centerX, double centerY)
    {
        return MatrixMath.ScaleAtPrepend(matrix.ToCore(), scaleX, scaleY, centerX, centerY).ToUno();
    }

    /// <summary>
    /// Creates a translation and scale matrix.
    /// </summary>
    /// <param name="scaleX">Scaling factor that is applied along the x-axis.</param>
    /// <param name="scaleY">Scaling factor that is applied along the y-axis.</param>
    /// <param name="offsetX">X-coordinate offset.</param>
    /// <param name="offsetY">Y-coordinate offset.</param>
    /// <returns>The created translation and scale matrix.</returns>
    public static Matrix ScaleAndTranslate(double scaleX, double scaleY, double offsetX, double offsetY)
    {
        return MatrixMath.ScaleAndTranslate(scaleX, scaleY, offsetX, offsetY).ToUno();
    }

    /// <summary>
    /// Creates a skew matrix.
    /// </summary>
    /// <param name="angleX">Angle of skew along the X-axis in radians.</param>
    /// <param name="angleY">Angle of skew along the Y-axis in radians.</param>
    /// <returns>The created skew matrix.</returns>
    public static Matrix Skew(float angleX, float angleY)
    {
        return MatrixMath.Skew(angleX, angleY).ToUno();
    }

    /// <summary>
    /// Creates a matrix that rotates.
    /// </summary>
    /// <param name="radians">Angle of rotation in radians.</param>
    /// <returns>The created rotation matrix.</returns>
    public static Matrix Rotation(double radians)
    {
        return MatrixMath.Rotation(radians).ToUno();
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
        return MatrixMath.Rotation(angle, centerX, centerY).ToUno();
    }

    /// <summary>
    /// Creates a matrix that rotates about a specified center.
    /// </summary>
    /// <param name="angle">Angle of rotation in radians.</param>
    /// <param name="center">The center of the rotation.</param>
    /// <returns>The created rotation matrix.</returns>
    public static Matrix Rotation(double angle, Point center)
    {
        return MatrixMath.Rotation(angle, center.X, center.Y).ToUno();
    }

    /// <summary>
    /// Transforms a point by the matrix.
    /// </summary>
    /// <param name="matrix">The matrix to use as a transformation matrix.</param>
    /// <param name="point">The original point to apply the transformation.</param>
    /// <returns>The result of the transformation for the input point.</returns>
    public static Point TransformPoint(Matrix matrix, Point point)
    {
        return MatrixMath.TransformPoint(matrix.ToCore(), point.ToCore()).ToUno();
    }
}

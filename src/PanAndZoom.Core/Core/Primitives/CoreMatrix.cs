// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using System;

namespace PanAndZoom.Core;

/// <summary>
/// A UI framework independent 2D affine transformation matrix.
/// </summary>
/// <remarks>
/// Uses the row-vector convention: a point is transformed as <c>[x y 1] * M</c>, so
/// <c>a * b</c> applies <c>a</c> first and <c>b</c> second.
/// </remarks>
/// <param name="M11">The first element of the first row (scale X).</param>
/// <param name="M12">The second element of the first row (skew Y).</param>
/// <param name="M21">The first element of the second row (skew X).</param>
/// <param name="M22">The second element of the second row (scale Y).</param>
/// <param name="M31">The first element of the third row (offset X).</param>
/// <param name="M32">The second element of the third row (offset Y).</param>
public readonly record struct CoreMatrix(double M11, double M12, double M21, double M22, double M31, double M32)
{
    private const double ZeroTolerance = 2.220446049250313E-15;

    /// <summary>
    /// Gets the identity matrix.
    /// </summary>
    public static CoreMatrix Identity { get; } = new(1.0, 0.0, 0.0, 1.0, 0.0, 0.0);

    /// <summary>
    /// Gets a value indicating whether the matrix is the identity matrix.
    /// </summary>
    public bool IsIdentity => this == Identity;

    /// <summary>
    /// Gets a value indicating whether the matrix can be inverted.
    /// </summary>
    public bool HasInverse => Math.Abs(GetDeterminant()) >= ZeroTolerance;

    /// <summary>
    /// Multiplies two matrices (applies <paramref name="value1"/> first and <paramref name="value2"/> second).
    /// </summary>
    /// <param name="value1">The first matrix.</param>
    /// <param name="value2">The second matrix.</param>
    /// <returns>The product matrix.</returns>
    public static CoreMatrix operator *(CoreMatrix value1, CoreMatrix value2)
    {
        return new CoreMatrix(
            value1.M11 * value2.M11 + value1.M12 * value2.M21,
            value1.M11 * value2.M12 + value1.M12 * value2.M22,
            value1.M21 * value2.M11 + value1.M22 * value2.M21,
            value1.M21 * value2.M12 + value1.M22 * value2.M22,
            value1.M31 * value2.M11 + value1.M32 * value2.M21 + value2.M31,
            value1.M31 * value2.M12 + value1.M32 * value2.M22 + value2.M32);
    }

    /// <summary>
    /// Calculates the determinant of the matrix.
    /// </summary>
    /// <returns>The determinant.</returns>
    public double GetDeterminant() => M11 * M22 - M12 * M21;

    /// <summary>
    /// Attempts to invert the matrix.
    /// </summary>
    /// <param name="inverted">The inverted matrix, or the default value when the matrix is not invertible.</param>
    /// <returns>True if the matrix was inverted.</returns>
    public bool TryInvert(out CoreMatrix inverted)
    {
        var determinant = GetDeterminant();
        if (Math.Abs(determinant) < ZeroTolerance)
        {
            inverted = default;
            return false;
        }

        var inverse = 1.0 / determinant;
        inverted = new CoreMatrix(
            M22 * inverse,
            -M12 * inverse,
            -M21 * inverse,
            M11 * inverse,
            (M21 * M32 - M31 * M22) * inverse,
            (M31 * M12 - M11 * M32) * inverse);
        return true;
    }

    /// <summary>
    /// Inverts the matrix.
    /// </summary>
    /// <returns>The inverted matrix.</returns>
    /// <exception cref="InvalidOperationException">The matrix is not invertible.</exception>
    public CoreMatrix Invert()
    {
        if (!TryInvert(out var inverted))
        {
            throw new InvalidOperationException("Transform is not invertible.");
        }

        return inverted;
    }

    /// <summary>
    /// Transforms a point by the matrix.
    /// </summary>
    /// <param name="point">The point.</param>
    /// <returns>The transformed point.</returns>
    public CorePoint Transform(CorePoint point)
    {
        return new CorePoint(
            point.X * M11 + point.Y * M21 + M31,
            point.X * M12 + point.Y * M22 + M32);
    }
}

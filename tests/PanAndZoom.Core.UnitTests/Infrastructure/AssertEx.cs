// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
namespace PanAndZoom.Core.UnitTests.Infrastructure;

public static class AssertEx
{
    public const double Tolerance = 1e-9;

    public static void Equal(double expected, double actual, double tolerance = Tolerance)
    {
        Assert.True(
            Math.Abs(expected - actual) <= tolerance || expected.Equals(actual),
            $"Expected {expected:R} but was {actual:R} (tolerance {tolerance}).");
    }

    public static void Equal(CoreMatrix expected, CoreMatrix actual, double tolerance = Tolerance)
    {
        Assert.True(
            Near(expected.M11, actual.M11, tolerance)
            && Near(expected.M12, actual.M12, tolerance)
            && Near(expected.M21, actual.M21, tolerance)
            && Near(expected.M22, actual.M22, tolerance)
            && Near(expected.M31, actual.M31, tolerance)
            && Near(expected.M32, actual.M32, tolerance),
            $"Expected {expected} but was {actual}.");
    }

    public static void Equal(CorePoint expected, CorePoint actual, double tolerance = Tolerance)
    {
        Assert.True(
            Near(expected.X, actual.X, tolerance) && Near(expected.Y, actual.Y, tolerance),
            $"Expected {expected} but was {actual}.");
    }

    public static void Equal(CoreVector expected, CoreVector actual, double tolerance = Tolerance)
    {
        Assert.True(
            Near(expected.X, actual.X, tolerance) && Near(expected.Y, actual.Y, tolerance),
            $"Expected {expected} but was {actual}.");
    }

    public static void Equal(CoreSize expected, CoreSize actual, double tolerance = Tolerance)
    {
        Assert.True(
            Near(expected.Width, actual.Width, tolerance) && Near(expected.Height, actual.Height, tolerance),
            $"Expected {expected} but was {actual}.");
    }

    public static void Equal(CoreRect expected, CoreRect actual, double tolerance = Tolerance)
    {
        Assert.True(
            Near(expected.X, actual.X, tolerance)
            && Near(expected.Y, actual.Y, tolerance)
            && Near(expected.Width, actual.Width, tolerance)
            && Near(expected.Height, actual.Height, tolerance),
            $"Expected {expected} but was {actual}.");
    }

    /// <summary>
    /// Asserts the engine exposes the expected uniform zoom and offsets.
    /// </summary>
    public static void View(PanAndZoomEngine engine, double zoom, double offsetX, double offsetY, double tolerance = Tolerance)
    {
        Equal(zoom, engine.ZoomX, tolerance);
        Equal(zoom, engine.ZoomY, tolerance);
        Equal(offsetX, engine.OffsetX, tolerance);
        Equal(offsetY, engine.OffsetY, tolerance);
        Equal(MatrixMath.ScaleAndTranslate(zoom, zoom, offsetX, offsetY), engine.Matrix, tolerance);
    }

    private static bool Near(double a, double b, double tolerance)
    {
        return a.Equals(b) || Math.Abs(a - b) <= tolerance;
    }
}

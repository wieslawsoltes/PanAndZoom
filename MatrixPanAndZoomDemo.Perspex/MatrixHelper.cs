using Perspex;
using System;

namespace MatrixPanAndZoomDemo.Perspex
{
    public static class MatrixHelper
    {
        public static readonly Matrix Identity = new Matrix(1.0, 0.0, 0.0, 1.0, 0.0, 0.0);

        public static Matrix Translate(double offsetX, double offsetY)
        {
            return new Matrix(1.0, 0.0, 0.0, 1.0, offsetX, offsetY);
        }

        public static Matrix TranslatePrepend(Matrix matrix, double offsetX, double offsetY)
        {
            return Translate(offsetX, offsetY) * matrix;
        }

        public static Matrix Scale(double scaleX, double scaleY)
        {
            return new Matrix(scaleX, 0, 0, scaleY, 0.0, 0.0);
        }

        public static Matrix ScaleAt(double scaleX, double scaleY, double centerX, double centerY)
        {
            return new Matrix(scaleX, 0, 0, scaleY, centerX - scaleX * centerX, centerY - scaleY * centerY);
        }

        public static Matrix ScaleAtPrepend(Matrix matrix, double scaleX, double scaleY, double centerX, double centerY)
        {
            return ScaleAt(scaleX, scaleY, centerX, centerY) * matrix;
        }

        public static Matrix Rotation(double radians)
        {
            double cos = Math.Cos(radians);
            double sin = Math.Sin(radians);
            return new Matrix(cos, sin, -sin, cos, 0, 0);
        }

        public static Matrix Rotation(double angle, double centerX, double centerY)
        {
            return Translate(-centerX, -centerY) * Rotation(angle) * Translate(centerX, centerY);
        }

        public static Matrix Rotation(double angle, Vector center)
        {
            return Translate(-center.X, -center.Y) * Rotation(angle) * Translate(center.X, center.Y);
        }

        public static Point TransformPoint(Matrix matrix, Point point)
        {
            return new Point(
                (point.X * matrix.M11) + (point.Y * matrix.M21) + matrix.M31,
                (point.X * matrix.M12) + (point.Y * matrix.M22) + matrix.M32);
        }
    }
}

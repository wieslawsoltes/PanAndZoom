// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using Avalonia.Data;
using Avalonia.Media;

namespace Avalonia.Controls
{
    /// <summary>
    /// Defines a content decorator that can stretch and scale a single child to fill the available space.
    /// </summary>
    public class ViewBox : Decorator
    {
        /// <summary>
        /// Gets or sets stretch mode.
        /// </summary>
        public Stretch Stretch
        {
            get => GetValue(StretchProperty);
            set => SetValue(StretchProperty, value);
        }

        /// <summary>
        /// Identifies the <seealso cref="Stretch"/> avalonia property.
        /// </summary>
        public static AvaloniaProperty<Stretch> StretchProperty =
            AvaloniaProperty.Register<ViewBox, Stretch>(nameof(Stretch), Stretch.Uniform, false, BindingMode.TwoWay);

        static ViewBox()
        {
            AffectsArrange(StretchProperty);
        }

        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size finalSize)
        {
            var size = base.ArrangeOverride(finalSize);
            if (Child != null && Child.IsMeasureValid)
            {
                ArrangeOverride(Child, size, Child.Bounds);
            }
            return size;
        }

        private void ArrangeOverride(IControl child, Size finalSize, Rect childBounds)
        {
            Matrix matrix = Matrix.Identity;
            double scaleX = finalSize.Width / childBounds.Width;
            double scaleY = finalSize.Height / childBounds.Height;
            double centerX = childBounds.Width / 2.0;
            double centerY = childBounds.Height / 2.0;

            switch (Stretch)
            {
                case Stretch.Fill:
                    {
                        matrix = ScaleAt(scaleX, scaleY, centerX, centerY);
                    }
                    break;
                case Stretch.Uniform:
                    {
                        double scale = Math.Min(scaleX, scaleY);
                        matrix = ScaleAt(scale, scale, centerX, centerY);
                    }
                    break;
                case Stretch.UniformToFill:
                    {
                        double scale = Math.Max(scaleX, scaleY);
                        matrix = ScaleAt(scale, scale, centerX, centerY);
                    }
                    break;
            }

            if (child.RenderTransform is MatrixTransform transform)
            {
                transform.Matrix = matrix;
            }
            else
            {
                child.RenderTransformOrigin = new RelativePoint(new Point(0, 0), RelativeUnit.Relative);
                child.RenderTransform = new MatrixTransform(matrix);
            }

            child.InvalidateVisual();
        }

        Matrix ScaleAt(double scaleX, double scaleY, double centerX, double centerY)
        {
            return new Matrix(scaleX, 0, 0, scaleY, centerX - (scaleX * centerX), centerY - (scaleY * centerY));
        }
    }
}

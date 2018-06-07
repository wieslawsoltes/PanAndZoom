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
        private IControl _element;
        private Matrix _matrix;
        private double _zoomX = 1.0;
        private double _zoomY = 1.0;
        private double _offsetX = 0.0;
        private double _offsetY = 0.0;

        /// <summary>
        /// Gets available stretch modes.
        /// </summary>
        public static Stretch[] Stretches => (Stretch[])Enum.GetValues(typeof(Stretch));

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

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewBox"/> class.
        /// </summary>
        public ViewBox()
            : base()
        {
            Defaults();
            AttachedToVisualTree += PanAndZoom_AttachedToVisualTree;
            DetachedFromVisualTree += PanAndZoom_DetachedFromVisualTree;
            this.GetObservable(ChildProperty).Subscribe(ChildChanged);
        }

        private void Defaults()
        {
            _matrix = Matrix.Identity;
        }

        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size finalSize)
        {
            var size = base.ArrangeOverride(finalSize);

            if (_element != null && _element.IsMeasureValid)
            {
                AutoFit(size.Width, size.Height, _element.Bounds.Width, _element.Bounds.Height);
            }

            return size;
        }

        private void PanAndZoom_AttachedToVisualTree(object sender, VisualTreeAttachmentEventArgs e)
        {
            ChildChanged(base.Child);
        }

        private void PanAndZoom_DetachedFromVisualTree(object sender, VisualTreeAttachmentEventArgs e)
        {
            DetachElement();
        }

        private void ChildChanged(IControl element)
        {
            if (element != null && element != _element && _element != null)
            {
                DetachElement();
            }

            if (element != null && element != _element)
            {
                AttachElement(element);
            }
        }

        private void AttachElement(IControl element)
        {
            if (element != null)
            {
                Defaults();
                _element = element;
            }
        }

        private void DetachElement()
        {
            if (_element != null)
            {
                _element.RenderTransform = null;
                _element = null;
                Defaults();
            }
        }

        private void Invalidate()
        {
            if (_element != null)
            {
                double oldZoomX = _zoomX;
                double oldZoomY = _zoomY;
                double oldOffsetX = _offsetX;
                double oldOffsetY = _offsetY;
                _zoomX = _matrix.M11;
                _zoomY = _matrix.M22;
                _offsetX = _matrix.M31;
                _offsetY = _matrix.M32;
                _element.RenderTransformOrigin = new RelativePoint(new Point(0, 0), RelativeUnit.Relative);
                _element.RenderTransform = new MatrixTransform(_matrix);
                _element.InvalidateVisual();
            }
        }

        private static Matrix ScaleAt(double scaleX, double scaleY, double centerX, double centerY)
        {
            return new Matrix(scaleX, 0, 0, scaleY, centerX - (scaleX * centerX), centerY - (scaleY * centerY));
        }

        private Matrix GetMatrix(double panelWidth, double panelHeight, double elementWidth, double elementHeight, Stretch mode)
        {
            double zx = panelWidth / elementWidth;
            double zy = panelHeight / elementHeight;
            double cx = elementWidth / 2.0;
            double cy = elementHeight / 2.0;
            double zoom = 1.0;
            switch (mode)
            {
                case Stretch.Fill:
                    return ScaleAt(zx, zy, cx, cy);
                case Stretch.Uniform:
                    zoom = Math.Min(zx, zy);
                    return ScaleAt(zoom, zoom, cx, cy);
                case Stretch.UniformToFill:
                    zoom = Math.Max(zx, zy);
                    return ScaleAt(zoom, zoom, cx, cy);
            }
            return Matrix.Identity;
        }

        private void Fill(double panelWidth, double panelHeight, double elementWidth, double elementHeight)
        {
            if (_element != null)
            {
                _matrix = GetMatrix(panelWidth, panelHeight, elementWidth, elementHeight, Stretch.Fill);
                Invalidate();
            }
        }

        private void Uniform(double panelWidth, double panelHeight, double elementWidth, double elementHeight)
        {
            if (_element != null)
            {
                _matrix = GetMatrix(panelWidth, panelHeight, elementWidth, elementHeight, Stretch.Uniform);
                Invalidate();
            }
        }

        private void UniformToFill(double panelWidth, double panelHeight, double elementWidth, double elementHeight)
        {
            if (_element != null)
            {
                _matrix = GetMatrix(panelWidth, panelHeight, elementWidth, elementHeight, Stretch.UniformToFill);
                Invalidate();
            }
        }

        private void AutoFit(double panelWidth, double panelHeight, double elementWidth, double elementHeight)
        {
            if (_element != null)
            {
                switch (Stretch)
                {
                    case Stretch.Fill:
                        Fill(panelWidth, panelHeight, elementWidth, elementHeight);
                        break;
                    case Stretch.Uniform:
                        Uniform(panelWidth, panelHeight, elementWidth, elementHeight);
                        break;
                    case Stretch.UniformToFill:
                        UniformToFill(panelWidth, panelHeight, elementWidth, elementHeight);
                        break;
                }
                Invalidate();
            }
        }

        private static Point TransformPoint(Matrix matrix, Point point)
        {
            return new Point(
                (point.X * matrix.M11) + (point.Y * matrix.M21) + matrix.M31,
                (point.X * matrix.M12) + (point.Y * matrix.M22) + matrix.M32);
        }

        private Point FixInvalidPointPosition(Point point)
        {
            return TransformPoint(_matrix.Invert(), point);
        }
    }
}

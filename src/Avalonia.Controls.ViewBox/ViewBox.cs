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
            AttachedToVisualTree += (sender, e) => ChildChanged(base.Child);
            DetachedFromVisualTree += (sender, e) => DetachElement(); ;
            this.GetObservable(ChildProperty).Subscribe(ChildChanged);
        }

        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size finalSize)
        {
            var size = base.ArrangeOverride(finalSize);
            if (_element != null && _element.IsMeasureValid)
            {
                Invalidate(size.Width, size.Height, _element.Bounds.Width, _element.Bounds.Height);
            }
            return size;
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
                _element = element;
            }
        }

        private void DetachElement()
        {
            if (_element != null)
            {
                _element.RenderTransform = null;
                _element = null;
            }
        }

        private void Invalidate(double panelWidth, double panelHeight, double elementWidth, double elementHeight)
        {
            if (_element != null)
            {
                Matrix matrix = Matrix.Identity;
                double zx = panelWidth / elementWidth;
                double zy = panelHeight / elementHeight;
                double cx = elementWidth / 2.0;
                double cy = elementHeight / 2.0;
                double zoom = 1.0;
                switch (Stretch)
                {
                    case Stretch.Fill:
                        matrix = ScaleAt(zx, zy, cx, cy);
                        break;
                    case Stretch.Uniform:
                        zoom = Math.Min(zx, zy);
                        matrix = ScaleAt(zoom, zoom, cx, cy);
                        break;
                    case Stretch.UniformToFill:
                        zoom = Math.Max(zx, zy);
                        matrix = ScaleAt(zoom, zoom, cx, cy);
                        break;
                }
                _element.RenderTransformOrigin = new RelativePoint(new Point(0, 0), RelativeUnit.Relative);
                _element.RenderTransform = new MatrixTransform(matrix);
                _element.InvalidateVisual();
            }

            Matrix ScaleAt(double scaleX, double scaleY, double centerX, double centerY)
            {
                return new Matrix(scaleX, 0, 0, scaleY, centerX - (scaleX * centerX), centerY - (scaleY * centerY));
            };
        }
    }
}

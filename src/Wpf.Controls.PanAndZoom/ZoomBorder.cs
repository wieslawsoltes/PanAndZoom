// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static System.Math;
using PanAndZoom;
using System.Diagnostics;

namespace Wpf.Controls.PanAndZoom
{
    /// <summary>
    /// Pan and zoom control for WPF.
    /// </summary>
    public class ZoomBorder : Border, IPanAndZoom
    {
        private UIElement _element;
        private Point _pan;
        private Point _previous;
        private Matrix _matrix;
        private bool _isPanning;
        private static StretchMode[] _autoFitModes = (StretchMode[])Enum.GetValues(typeof(StretchMode));

        /// <summary>
        /// Gets available stretch modes.
        /// </summary>
        public static StretchMode[] StretchModes => _autoFitModes;

        /// <inheritdoc/>
        public Action<double, double, double, double> InvalidatedChild { get; set; }

        /// <inheritdoc/>
        public double ZoomSpeed
        {
            get { return (double)GetValue(ZoomSpeedProperty); }
            set { SetValue(ZoomSpeedProperty, value); }
        }

        /// <inheritdoc/>
        public StretchMode Stretch
        {
            get { return (StretchMode)GetValue(StretchProperty); }
            set { SetValue(StretchProperty, value); }
        }

        /// <inheritdoc/>
        public bool EnableInput
        {
            get { return (bool)GetValue(EnableInputProperty); }
            set { SetValue(EnableInputProperty, value); }
        }

        /// <summary>
        /// Identifies the <seealso cref="ZoomSpeed"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ZoomSpeedProperty =
            DependencyProperty.Register(
                nameof(ZoomSpeed),
                typeof(double),
                typeof(ZoomBorder),
                new FrameworkPropertyMetadata(1.2, FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        /// Identifies the <seealso cref="Stretch"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StretchProperty =
            DependencyProperty.Register(
                nameof(Stretch),
                typeof(StretchMode),
                typeof(ZoomBorder),
                new FrameworkPropertyMetadata(StretchMode.Uniform, FrameworkPropertyMetadataOptions.AffectsArrange));

        /// <summary>
        /// Identifies the <seealso cref="EnableInput"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EnableInputProperty =
            DependencyProperty.Register(
                nameof(EnableInput),
                typeof(bool),
                typeof(ZoomBorder),
                new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Gets or sets single child of a <see cref="ZoomBorder"/> control.
        /// </summary>
        public override UIElement Child
        {
            get { return base.Child; }
            set
            {
                base.Child = value;
                ChildChanged(value);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZoomBorder"/> class.
        /// </summary>
        public ZoomBorder()
            : base()
        {
            Defaults();

            ZoomSpeed = 1.2;
            Stretch = StretchMode.None;

            Focusable = true;
            Background = Brushes.Transparent;

            Loaded += PanAndZoom_Loaded;
            Unloaded += PanAndZoom_Unloaded;
        }

        private void Defaults()
        {
            _isPanning = false;
            _matrix = MatrixHelper.Identity;
        }

        /// <summary>
        /// Arranges the control's child.
        /// </summary>
        /// <param name="finalSize">The size allocated to the control.</param>
        /// <returns>The space taken.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            var size = base.ArrangeOverride(finalSize);

            if (_element != null && _element.IsMeasureValid)
            {
                AutoFit(
                    size.Width,
                    size.Height,
                    _element.RenderSize.Width,
                    _element.RenderSize.Height);
            }

            return size;
        }

        private void PanAndZoom_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine($"Loaded: {this.Name}");
            ChildChanged(base.Child);
        }

        private void PanAndZoom_Unloaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine($"Unloaded: {this.Name}");
            DetachElement();
        }

        private void Border_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (EnableInput)
            {
                Wheel(e);
            }
        }

        private void Border_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (EnableInput)
            {
                Pressed(e);
            }
        }

        private void Border_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (EnableInput)
            {
                Released(e);
            }
        }

        private void Border_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (EnableInput)
            {
                Moved(e);
            }
        }

        private void ChildChanged(UIElement element)
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

        private void AttachElement(UIElement element)
        {
            if (element != null)
            {
                Defaults();
                _element = element;
                this.Focus();
                this.PreviewMouseWheel += Border_PreviewMouseWheel;
                this.PreviewMouseRightButtonDown += Border_PreviewMouseRightButtonDown;
                this.PreviewMouseRightButtonUp += Border_PreviewMouseRightButtonUp;
                this.PreviewMouseMove += Border_PreviewMouseMove;
            }
        }

        private void DetachElement()
        {
            if (_element != null)
            {
                this.PreviewMouseWheel -= Border_PreviewMouseWheel;
                this.PreviewMouseRightButtonDown -= Border_PreviewMouseRightButtonDown;
                this.PreviewMouseRightButtonUp -= Border_PreviewMouseRightButtonUp;
                this.PreviewMouseMove -= Border_PreviewMouseMove;
                _element.RenderTransform = null;
                _element = null;
                Defaults();
            }
        }

        private void Wheel(MouseWheelEventArgs e)
        {
            if (_element != null && Mouse.Captured == null)
            {
                Point point = e.GetPosition(_element);
                ZoomDeltaTo((double)e.Delta, point.X, point.Y);
            }
        }

        private void Pressed(MouseButtonEventArgs e)
        {
            if (_element != null && Mouse.Captured == null && _isPanning == false)
            {
                Point point = e.GetPosition(_element);
                StartPan(point.X, point.Y);
                _element.CaptureMouse();
                _isPanning = true;
            }
        }

        private void Released(MouseButtonEventArgs e)
        {
            if (_element != null && _element.IsMouseCaptured == true && _isPanning == true)
            {
                _element.ReleaseMouseCapture();
                _isPanning = false;
            }
        }

        private void Moved(MouseEventArgs e)
        {
            if (_element != null && _element.IsMouseCaptured == true && _isPanning == true)
            {
                Point point = e.GetPosition(_element);
                PanTo(point.X, point.Y);
            }
        }

        /// <inheritdoc/>
        public void Invalidate()
        {
            if (_element != null)
            {
                Debug.WriteLine($"Zoom: {_matrix.M11} {_matrix.M12} Offset: {_matrix.OffsetX} {_matrix.OffsetY}");
                this.InvalidatedChild?.Invoke(_matrix.M11, _matrix.M12, _matrix.OffsetX, _matrix.OffsetY);
                _element.RenderTransformOrigin = new Point(0, 0);
                _element.RenderTransform = new MatrixTransform(_matrix);
                _element.InvalidateVisual();
            }
        }

        /// <inheritdoc/>
        public void ZoomTo(double zoom, double x, double y)
        {
            _matrix = MatrixHelper.ScaleAtPrepend(_matrix, zoom, zoom, x, y);
            Invalidate();
        }

        /// <inheritdoc/>
        public void ZoomDeltaTo(double delta, double x, double y)
        {
            ZoomTo(delta > 0 ? ZoomSpeed : 1 / ZoomSpeed, x, y);
        }

        /// <inheritdoc/>
        public void StartPan(double x, double y)
        {
            _pan = new Point();
            _previous = new Point(x, y);
        }

        /// <inheritdoc/>
        public void PanTo(double x, double y)
        {
            double dx = x - _previous.X;
            double dy = y - _previous.Y;
            Point delta = new Point(dx, dy);
            _previous = new Point(x, y);
            _pan = new Point(_pan.X + delta.X, _pan.Y + delta.Y);
            _matrix = MatrixHelper.TranslatePrepend(_matrix, _pan.X, _pan.Y);
            Invalidate();
        }

        private Matrix GetMatrix(double panelWidth, double panelHeight, double elementWidth, double elementHeight, StretchMode mode)
        {
            double zx = panelWidth / elementWidth;
            double zy = panelHeight / elementHeight;
            double cx = elementWidth < panelWidth ? elementWidth / 2.0 : 0.0;
            double cy = elementHeight < panelHeight ? elementHeight / 2.0 : 0.0;
            double zoom = 1.0;
            switch (mode)
            {
                case StretchMode.Fill:
                    {
                        if (elementWidth > panelWidth && elementHeight > panelHeight)
                        {
                            cx = (panelWidth - (elementWidth * zx)) / 2.0;
                            cy = (panelHeight - (elementHeight * zy)) / 2.0;
                            var matrix = MatrixHelper.ScaleAt(zx, zy, 0.0, 0.0);
                            matrix.OffsetX = cx;
                            matrix.OffsetY = cy;
                            return matrix;
                        }
                        else
                        {
                            return MatrixHelper.ScaleAt(zx, zy, cx, cy);
                        }
                    }
                case StretchMode.Uniform:
                    {
                        zoom = Min(zx, zy);
                        if (elementWidth > panelWidth && elementHeight > panelHeight)
                        {
                            cx = (panelWidth - (elementWidth * zoom)) / 2.0;
                            cy = (panelHeight - (elementHeight * zoom)) / 2.0;
                            var matrix = MatrixHelper.ScaleAt(zoom, zoom, 0.0, 0.0);
                            matrix.OffsetX = cx;
                            matrix.OffsetY = cy;
                            return matrix;
                        }
                        else
                        {
                            return MatrixHelper.ScaleAt(zoom, zoom, cx, cy);
                        }
                    }
                case StretchMode.UniformToFill:
                    {
                        zoom = Max(zx, zy);
                        if (elementWidth > panelWidth && elementHeight > panelHeight)
                        {
                            cx = (panelWidth - (elementWidth * zoom)) / 2.0;
                            cy = (panelHeight - (elementHeight * zoom)) / 2.0;
                            var matrix = MatrixHelper.ScaleAt(zoom, zoom, 0.0, 0.0);
                            matrix.OffsetX = cx;
                            matrix.OffsetY = cy;
                            return matrix;
                        }
                        else
                        {
                            return MatrixHelper.ScaleAt(zoom, zoom, cx, cy);
                        }
                    }
            }
            return MatrixHelper.Identity;
        }

        /// <inheritdoc/>
        public void Fill(double panelWidth, double panelHeight, double elementWidth, double elementHeight)
        {
            Debug.WriteLine($"Fill: {panelWidth}x{panelHeight} {elementWidth}x{elementHeight}");
            if (_element != null)
            {
                _matrix = GetMatrix(panelWidth, panelHeight, elementWidth, elementHeight, StretchMode.Fill);
                Invalidate();
            }
        }

        /// <inheritdoc/>
        public void Uniform(double panelWidth, double panelHeight, double elementWidth, double elementHeight)
        {
            Debug.WriteLine($"Uniform: {panelWidth}x{panelHeight} {elementWidth}x{elementHeight}");
            if (_element != null)
            {
                _matrix = GetMatrix(panelWidth, panelHeight, elementWidth, elementHeight, StretchMode.Uniform);
                Invalidate();
            }
        }

        /// <inheritdoc/>
        public void UniformToFill(double panelWidth, double panelHeight, double elementWidth, double elementHeight)
        {
            Debug.WriteLine($"UniformToFill: {panelWidth}x{panelHeight} {elementWidth}x{elementHeight}");
            if (_element != null)
            {
                _matrix = GetMatrix(panelWidth, panelHeight, elementWidth, elementHeight, StretchMode.UniformToFill);
                Invalidate();
            }
        }

        /// <inheritdoc/>
        public void AutoFit(double panelWidth, double panelHeight, double elementWidth, double elementHeight)
        {
            Debug.WriteLine($"AutoFit: {panelWidth}x{panelHeight} {elementWidth}x{elementHeight}");
            if (_element != null)
            {
                switch (Stretch)
                {
                    case StretchMode.Fill:
                        Fill(panelWidth, panelHeight, elementWidth, elementHeight);
                        break;
                    case StretchMode.Uniform:
                        Uniform(panelWidth, panelHeight, elementWidth, elementHeight);
                        break;
                    case StretchMode.UniformToFill:
                        UniformToFill(panelWidth, panelHeight, elementWidth, elementHeight);
                        break;
                }
                Invalidate();
            }
        }

        /// <inheritdoc/>
        public void ToggleStretchMode()
        {
            switch (Stretch)
            {
                case StretchMode.None:
                    Stretch = StretchMode.Fill;
                    break;
                case StretchMode.Fill:
                    Stretch = StretchMode.Uniform;
                    break;
                case StretchMode.Uniform:
                    Stretch = StretchMode.UniformToFill;
                    break;
                case StretchMode.UniformToFill:
                    Stretch = StretchMode.None;
                    break;
            }
        }

        /// <inheritdoc/>
        public void Reset()
        {
            _matrix = MatrixHelper.Identity;
            Invalidate();
        }

        /// <inheritdoc/>
        public void Fill()
        {
            Fill(this.RenderSize.Width, this.RenderSize.Height, _element.RenderSize.Width, _element.RenderSize.Height);
        }

        /// <inheritdoc/>
        public void Uniform()
        {
            Uniform(this.RenderSize.Width, this.RenderSize.Height, _element.RenderSize.Width, _element.RenderSize.Height);
        }

        /// <inheritdoc/>
        public void UniformToFill()
        {
            UniformToFill(this.RenderSize.Width, this.RenderSize.Height, _element.RenderSize.Width, _element.RenderSize.Height);
        }

        /// <inheritdoc/>
        public void AutoFit()
        {
            if (_element != null)
            {
                AutoFit(this.RenderSize.Width, this.RenderSize.Height, _element.RenderSize.Width, _element.RenderSize.Height);
            }
        }
    }
}

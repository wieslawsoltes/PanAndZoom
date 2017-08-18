// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static System.Math;
using PanAndZoom;

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
            _isPanning = false;
            _matrix = MatrixHelper.Identity;

            ZoomSpeed = 1.2;
            Stretch = StretchMode.None;

            Focusable = true;
            Background = Brushes.Transparent;

            Loaded += PanAndZoom_Loaded;
            Unloaded += PanAndZoom_Unloaded;
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
                    _element.DesiredSize.Width,
                    _element.DesiredSize.Height);
            }

            return size;
        }

        private void PanAndZoom_Loaded(object sender, RoutedEventArgs e)
        {
            ChildChanged(base.Child);
        }

        private void PanAndZoom_Unloaded(object sender, RoutedEventArgs e)
        {
            Unload();
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
                Unload();
            }

            if (element != null && element != _element)
            {
                Initialize(element);
            }
        }

        private void Initialize(UIElement element)
        {
            if (element != null)
            {
                _element = element;
                this.Focus();
                this.PreviewMouseWheel += Border_PreviewMouseWheel;
                this.PreviewMouseRightButtonDown += Border_PreviewMouseRightButtonDown;
                this.PreviewMouseRightButtonUp += Border_PreviewMouseRightButtonUp;
                this.PreviewMouseMove += Border_PreviewMouseMove;
            }
        }

        private void Unload()
        {
            if (_element != null)
            {
                this.PreviewMouseWheel -= Border_PreviewMouseWheel;
                this.PreviewMouseRightButtonDown -= Border_PreviewMouseRightButtonDown;
                this.PreviewMouseRightButtonUp -= Border_PreviewMouseRightButtonUp;
                this.PreviewMouseMove -= Border_PreviewMouseMove;
                _element.RenderTransform = null;
                _element = null;
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

        /// <inheritdoc/>
        public void Fill(double panelWidth, double panelHeight, double elementWidth, double elementHeight)
        {
            if (_element != null)
            {
                double zx = panelWidth / elementWidth;
                double zy = panelHeight / elementHeight;
                double cx = elementWidth / 2.0;
                double cy = elementHeight / 2.0;
                _matrix = MatrixHelper.ScaleAt(zx, zy, cx, cy);
                Invalidate();
            }
        }

        /// <inheritdoc/>
        public void Uniform(double panelWidth, double panelHeight, double elementWidth, double elementHeight)
        {
            if (_element != null)
            {
                double zx = panelWidth / elementWidth;
                double zy = panelHeight / elementHeight;
                double zoom = Min(zx, zy);
                double cx = elementWidth / 2.0;
                double cy = elementHeight / 2.0;
                _matrix = MatrixHelper.ScaleAt(zoom, zoom, cx, cy);
                Invalidate();
            }
        }

        /// <inheritdoc/>
        public void UniformToFill(double panelWidth, double panelHeight, double elementWidth, double elementHeight)
        {
            if (_element != null)
            {
                double zx = panelWidth / elementWidth;
                double zy = panelHeight / elementHeight;
                double zoom = Max(zx, zy);
                double cx = elementWidth / 2.0;
                double cy = elementHeight / 2.0;
                _matrix = MatrixHelper.ScaleAt(zoom, zoom, cx, cy);
                Invalidate();
            }
        }

        /// <inheritdoc/>
        public void AutoFit(double panelWidth, double panelHeight, double elementWidth, double elementHeight)
        {
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
            Fill(
                this.ActualWidth,
                this.ActualHeight,
                _element.RenderSize.Width,
                _element.RenderSize.Height);
        }

        /// <inheritdoc/>
        public void Uniform()
        {
            Uniform(
                this.ActualWidth,
                this.ActualHeight,
                _element.RenderSize.Width,
                _element.RenderSize.Height);
        }

        /// <inheritdoc/>
        public void UniformToFill()
        {
            UniformToFill(
                this.ActualWidth,
                this.ActualHeight,
                _element.RenderSize.Width,
                _element.RenderSize.Height);
        }

        /// <inheritdoc/>
        public void AutoFit()
        {
            if (_element != null)
            {
                AutoFit(
                    this.DesiredSize.Width,
                    this.DesiredSize.Height,
                    _element.RenderSize.Width,
                    _element.RenderSize.Height);
            }
        }
    }
}

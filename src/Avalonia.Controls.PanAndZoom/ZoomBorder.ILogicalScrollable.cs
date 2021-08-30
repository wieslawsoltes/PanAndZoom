using System;
using Avalonia.Controls.Primitives;
using Avalonia.Input;

namespace Avalonia.Controls.PanAndZoom
{
    /// <summary>
    /// Pan and zoom control for Avalonia.
    /// </summary>
    public partial class ZoomBorder : ILogicalScrollable
    { 
        /// <inheritdoc/>
        Size IScrollable.Extent => _extent;

        /// <inheritdoc/>
        Vector IScrollable.Offset
        {
            get => _offset;
            set
            {
                if (!_isInvalidating)
                {
                    var (x, y) = _offset;
                    _offset = value;
                    var dx = x - _offset.X;
                    var dy = y - _offset.Y;
                    Log($"[Offset] offset: {_offset}, dx: {dx}, dy: {dy}");
                    PanDelta(dx, dy, false, !this.IsPointerOver);
                }
            }
        }

        /// <inheritdoc/>
        Size IScrollable.Viewport => _viewport;

        bool ILogicalScrollable.CanHorizontallyScroll
        {
            get => _canHorizontallyScroll;
            set
            {
                _canHorizontallyScroll = value;
                InvalidateMeasure();
            }
        }

        bool ILogicalScrollable.CanVerticallyScroll
        {
            get => _canVerticallyScroll;
            set
            {
                _canVerticallyScroll = value;
                InvalidateMeasure();
            }
        }

        bool ILogicalScrollable.IsLogicalScrollEnabled => true;

        event EventHandler ILogicalScrollable.ScrollInvalidated
        {
            add => _scrollInvalidated += value;
            remove => _scrollInvalidated -= value;
        }

        Size ILogicalScrollable.ScrollSize => new Size(1, 1);

        Size ILogicalScrollable.PageScrollSize => new Size(10, 10);

        bool ILogicalScrollable.BringIntoView(IControl target, Rect targetRect)
        {
            return false;
        }

        IControl? ILogicalScrollable.GetControlInDirection(NavigationDirection direction, IControl from)
        {
            return null;
        }

        void ILogicalScrollable.RaiseScrollInvalidated(EventArgs e)
        {
            _scrollInvalidated?.Invoke(this, e);
        }

        private void InvalidateScrollable()
        {
            if (this is not ILogicalScrollable scrollable)
            {
                return;
            }

            if (_element == null)
            {
                return;
            }

            ZoomHelper.CalculateScrollable(_element.Bounds, _matrix, out var extent, out var viewport, out var offset);

            _extent = extent;
            _offset = offset;
            _viewport = viewport;

            scrollable.RaiseScrollInvalidated(EventArgs.Empty);
        }
    }
}

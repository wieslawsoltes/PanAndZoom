// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;

namespace PanAndZoom
{
    /// <summary>
    /// Defines pan and zoom contract.
    /// </summary>
    public interface IPanAndZoom
    {
        /// <summary>
        /// Gets or sets invalidate action for border child element.
        /// </summary>
        /// <remarks>
        /// First parameter is zoom ratio for x axis.
        /// Second parameter is zoom ratio for y axis.
        /// Third parameter is pan offset for x axis.
        /// Fourth parameter is pan offset for y axis.
        /// </remarks>
        Action<double, double, double, double> InvalidatedChild { get; set; }

        /// <summary>
        /// Gets or sets pan input button.
        /// </summary>
        ButtonName PanButton { get; set; }

        /// <summary>
        /// Gets or sets zoom speed ratio.
        /// </summary>
        double ZoomSpeed { get; set; }

        /// <summary>
        /// Gets or sets stretch mode.
        /// </summary>
        StretchMode Stretch { get; set; }

        /// <summary>
        /// Gets the zoom ratio for x axis.
        /// </summary>
        double ZoomX { get; }

        /// <summary>
        /// Gets the zoom ratio for y axis.
        /// </summary>
        double ZoomY { get; }

        /// <summary>
        /// Gets the pan offset for x axis.
        /// </summary>
        double OffsetX { get; }

        /// <summary>
        /// Gets the pan offset for y axis.
        /// </summary>
        double OffsetY { get; }

        /// <summary>
        /// Gets or sets flag indicating whether zoom ratio and pan offset constrains are applied.
        /// </summary>
        bool EnableConstrains { get; set; }

        /// <summary>
        /// Gets or sets minimum zoom ratio for x axis.
        /// </summary>
        double MinZoomX { get; set; }

        /// <summary>
        /// Gets or sets maximum zoom ratio for x axis.
        /// </summary>
        double MaxZoomX { get; set; }

        /// <summary>
        /// Gets or sets minimum zoom ratio for y axis.
        /// </summary>
        double MinZoomY { get; set; }

        /// <summary>
        /// Gets or sets maximum zoom ratio for y axis.
        /// </summary>
        double MaxZoomY { get; set; }

        /// <summary>
        /// Gets or sets minimum offset for x axis.
        /// </summary>
        double MinOffsetX { get; set; }

        /// <summary>
        /// Gets or sets maximum offset for x axis.
        /// </summary>
        double MaxOffsetX { get; set; }

        /// <summary>
        /// Gets or sets minimum offset for y axis.
        /// </summary>
        double MinOffsetY { get; set; }

        /// <summary>
        /// Gets or sets maximum offset for y axis.
        /// </summary>
        double MaxOffsetY { get; set; }

        /// <summary>
        /// Gets or sets flag indicating whether input events are processed.
        /// </summary>
        bool EnableInput { get; set; }

        /// <summary>
        /// Gets or sets flag indicating whether zoom gesture is enabled.
        /// </summary>
        bool EnableGestureZoom { get; set; }

        /// <summary>
        /// Gets or sets flag indicating whether rotation gesture is enabled.
        /// </summary>
        bool EnableGestureRotation { get; set; }

        /// <summary>
        /// Gets or sets flag indicating whether translation (pan) gesture is enabled.
        /// </summary>
        bool EnableGestureTranslation { get; set; }

        /// <summary>
        /// Invalidate child element.
        /// </summary>
        void Invalidate();

        /// <summary>
        /// Zoom to provided zoom ratio and provided center point.
        /// </summary>
        /// <param name="zoom">The zoom ratio.</param>
        /// <param name="x">The center point x axis coordinate.</param>
        /// <param name="y">The center point y axis coordinate.</param>
        void ZoomTo(double zoom, double x, double y);

        /// <summary>
        /// Zoom to provided zoom delta ratio and provided center point.
        /// </summary>
        /// <param name="delta">The zoom delta ratio.</param>
        /// <param name="x">The center point x axis coordinate.</param>
        /// <param name="y">The center point y axis coordinate.</param>
        void ZoomDeltaTo(double delta, double x, double y);

        /// <summary>
        /// Set pan origin.
        /// </summary>
        /// <param name="x">The origin point x axis coordinate.</param>
        /// <param name="y">The origin point y axis coordinate.</param>
        void StartPan(double x, double y);

        /// <summary>
        /// Pan control to provided target point.
        /// </summary>
        /// <param name="x">The target point x axis coordinate.</param>
        /// <param name="y">The target point y axis coordinate.</param>
        void PanTo(double x, double y);

        /// <summary>
        /// Zoom and pan to fill panel.
        /// </summary>
        /// <param name="panelWidth">The panel width.</param>
        /// <param name="panelHeight">The panel height.</param>
        /// <param name="elementWidth">The element width.</param>
        /// <param name="elementHeight">The element height.</param>
        void Fill(double panelWidth, double panelHeight, double elementWidth, double elementHeight);

        /// <summary>
        /// Zoom and pan to panel extents while maintaining aspect ratio.
        /// </summary>
        /// <param name="panelWidth">The panel width.</param>
        /// <param name="panelHeight">The panel height.</param>
        /// <param name="elementWidth">The element width.</param>
        /// <param name="elementHeight">The element height.</param>
        void Uniform(double panelWidth, double panelHeight, double elementWidth, double elementHeight);

        /// <summary>
        /// Zoom and pan to panel extents while maintaining aspect ratio. If aspect of panel is different panel is filled.
        /// </summary>
        /// <param name="panelWidth">The panel width.</param>
        /// <param name="panelHeight">The panel height.</param>
        /// <param name="elementWidth">The element width.</param>
        /// <param name="elementHeight">The element height.</param>
        void UniformToFill(double panelWidth, double panelHeight, double elementWidth, double elementHeight);

        /// <summary>
        /// Zoom and pan child element inside panel using stretch mode.
        /// </summary>
        /// <param name="panelWidth">The panel width.</param>
        /// <param name="panelHeight">The panel height.</param>
        /// <param name="elementWidth">The element width.</param>
        /// <param name="elementHeight">The element height.</param>
        void AutoFit(double panelWidth, double panelHeight, double elementWidth, double elementHeight);

        /// <summary>
        /// Set next stretch mode.
        /// </summary>
        void ToggleStretchMode();

        /// <summary>
        /// Reset pan and zoom matrix.
        /// </summary>
        void Reset();

        /// <summary>
        /// Zoom and pan to fill panel.
        /// </summary>
        void Fill();

        /// <summary>
        /// Zoom and pan to panel extents while maintaining aspect ratio.
        /// </summary>
        void Uniform();

        /// <summary>
        /// Zoom and pan to panel extents while maintaining aspect ratio. If aspect of panel is different panel is filled.
        /// </summary>
        void UniformToFill();

        /// <summary>
        /// Zoom and pan child element inside panel using stretch mode.
        /// </summary>
        void AutoFit();
    }
}

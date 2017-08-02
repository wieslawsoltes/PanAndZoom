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
        Action<double, double, double, double> InvalidatedChild { get; set; }

        /// <summary>
        /// Gets or sets zoom speed ratio.
        /// </summary>
        double ZoomSpeed { get; set; }

        /// <summary>
        /// Gets or sets auto-fit mode.
        /// </summary>
        AutoFitMode AutoFitMode { get; set; }

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
        /// Zoom and pan to panel extents while maintaining aspect ratio.
        /// </summary>
        /// <param name="panelWidth">The panel width.</param>
        /// <param name="panelHeight">The panel height.</param>
        /// <param name="elementWidth">The element width.</param>
        /// <param name="elementHeight">The element height.</param>
        public void Extent(double panelWidth, double panelHeight, double elementWidth, double elementHeight);

        /// <summary>
        /// Zoom and pan to fill panel.
        /// </summary>
        /// <param name="panelWidth">The panel width.</param>
        /// <param name="panelHeight">The panel height.</param>
        /// <param name="elementWidth">The element width.</param>
        /// <param name="elementHeight">The element height.</param>
        public void Fill(double panelWidth, double panelHeight, double elementWidth, double elementHeight);

        /// <summary>
        /// Zoom and pan child elemnt inside panel using auto-fit mode.
        /// </summary>
        /// <param name="panelWidth">The panel width.</param>
        /// <param name="panelHeight">The panel height.</param>
        /// <param name="elementWidth">The element width.</param>
        /// <param name="elementHeight">The element height.</param>
        public void AutoFit(double panelWidth, double panelHeight, double elementWidth, double elementHeight);

        /// <summary>
        /// Set next auto-fit mode.
        /// </summary>
        void ToggleAutoFitMode();

        /// <summary>
        /// Reset pan and zoom matrix.
        /// </summary>
        void Reset();

        /// <summary>
        /// Zoom and pan to panel extents while maintaining aspect ratio.
        /// </summary>
        void Extent();

        /// <summary>
        /// Zoom and pan to fill panel.
        /// </summary>
        void Fill();

        /// <summary>
        /// Zoom and pan child elemnt inside panel using auto-fit mode.
        /// </summary>
        void AutoFit();
    }
}

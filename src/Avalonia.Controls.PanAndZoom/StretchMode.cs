/*
 * PanAndZoom A PanAndZoom control for Avalonia.
 * Copyright (C) 2023  Wiesław Šoltés
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, either version 3 of the
 * License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */
namespace Avalonia.Controls.PanAndZoom;

/// <summary>
/// Describes how content is resized to fill its allocated space.
/// </summary>
public enum StretchMode
{
    /// <summary>
    /// The content preserves its original size.
    /// </summary>
    None,

    /// <summary>
    /// The content is resized to fill the destination dimensions. The aspect ratio is not preserved.
    /// </summary>
    Fill,

    /// <summary>
    /// The content is resized to fit in the destination dimensions while it preserves its native aspect ratio.
    /// </summary>
    Uniform,

    /// <summary>
    /// The content is resized to fill the destination dimensions while it preserves its native aspect ratio. If the aspect ratio of the destination rectangle differs from the source, the source content is clipped to fit in the destination dimensions.
    /// </summary>
    UniformToFill
}

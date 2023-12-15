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
using Xunit;

namespace Avalonia.Controls.PanAndZoom.UnitTests;

public class MatrixHelperTests
{
    [Fact]
    public void Translate_Returns_Matrix()
    {
        var target = MatrixHelper.Translate(20, 30);
        Assert.Equal(1.0, target.M11);
        Assert.Equal(0.0, target.M12);
        Assert.Equal(0.0, target.M21);
        Assert.Equal(1.0, target.M22);
        Assert.Equal(20.0, target.M31);
        Assert.Equal(30.0, target.M32);
    }
 
    [Fact]
    public void Scale_Returns_Matrix()
    {
        var target = MatrixHelper.Scale(2, 3);
        Assert.Equal(2.0, target.M11);
        Assert.Equal(0.0, target.M12);
        Assert.Equal(0.0, target.M21);
        Assert.Equal(3.0, target.M22);
        Assert.Equal(0.0, target.M31);
        Assert.Equal(0.0, target.M32);
    }
}

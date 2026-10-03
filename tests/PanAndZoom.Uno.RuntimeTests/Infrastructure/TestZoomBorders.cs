// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
namespace PanAndZoom.Uno.RuntimeTests.Infrastructure;

// ZoomBorder subclasses used by the tests. They are public top-level types because native WinUI only
// applies the ZoomBorder default style to subclasses it has XAML type metadata for, and the WinUI test
// app generates that metadata from tests/PanAndZoom.WinUI.RuntimeTests/TestTypes.xaml.

/// <summary>
/// ZoomBorder subclass with custom content bounds and transform validation (used by ZoomBorderContentBoundsTests).
/// </summary>
public sealed class CustomBoundsZoomBorder : ZoomBorder
{
    public Rect CustomBounds { get; set; }

    public double MaximumAcceptedZoom { get; set; } = double.PositiveInfinity;

    protected override Rect GetContentBounds() => CustomBounds;

    protected override bool ValidateTransform(Matrix newMatrix) => newMatrix.M11 <= MaximumAcceptedZoom;

    public void RefreshCustomBounds() => InvalidateContentBounds();
}

/// <summary>
/// Testable ZoomBorder subclass for testing protected virtual methods.
/// </summary>
public sealed class TestableZoomBorder : ZoomBorder
{
    public bool OnResizedCalled { get; private set; }

    public Size? LastOldSize { get; private set; }

    public Size? LastNewSize { get; private set; }

    protected override void OnResized(Size oldSize, Size newSize)
    {
        OnResizedCalled = true;
        LastOldSize = oldSize;
        LastNewSize = newSize;
        base.OnResized(oldSize, newSize);
    }
}

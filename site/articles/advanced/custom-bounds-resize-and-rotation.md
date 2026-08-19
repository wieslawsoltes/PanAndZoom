---
title: "Custom Bounds, Resize, and Rotation"
---

# Custom Bounds, Resize, and Rotation

If the built-in behavior is close but not exact, `ZoomBorder` exposes virtual hooks for custom policy.

## Extensibility Points

- `GetContentBounds()`: return the effective bounds in child content coordinates; these bounds constrain panning and define scrollbar extent when `BoundsMode="Custom"`
- `ValidateTransform(Matrix newMatrix)`: accept or veto the final proposed transform
- `InvalidateContentBounds()`: reapply constraints and refresh scrollbar state after dynamic custom bounds change
- `OnResized(Size oldSize, Size newSize)`: apply custom resize policy

## Large Panning Area With Scrollbars

`Custom` uses a finite content-coordinate rectangle. Returning a rectangle larger than the child creates extra navigable space while keeping scrollbar positions meaningful:

```csharp
public class CustomZoomBorder : ZoomBorder
{
    private Rect _navigationBounds = new(-5000, -5000, 10000, 10000);

    protected override Rect GetContentBounds() => _navigationBounds;

    protected override bool ValidateTransform(Matrix newMatrix)
    {
        return newMatrix.M11 is >= 0.1 and <= 20;
    }

    public void SetNavigationBounds(Rect bounds)
    {
        _navigationBounds = bounds;
        InvalidateContentBounds();
    }
}
```

```xml
<ScrollViewer HorizontalScrollBarVisibility="Auto"
              VerticalScrollBarVisibility="Auto">
  <local:CustomZoomBorder BoundsMode="Custom"
                          Stretch="None">
    <!-- content -->
  </local:CustomZoomBorder>
</ScrollViewer>
```

Panning remains bounded by the returned rectangle. Truly infinite panning cannot be represented by finite scrollbars; choose bounds large enough for the application’s workspace.

## When To Override

- content bounds depend on domain data rather than only the child's measured bounds
- certain zoom or pan regions must be blocked
- resize behavior needs to preserve a custom anchor or workflow-specific invariant

## Rotation Considerations

Rotation state is configurable, but advanced rotation-heavy surfaces should validate how rotation interacts with bounds, serialization, and any custom overlay math before relying on it as a full scene-graph feature.

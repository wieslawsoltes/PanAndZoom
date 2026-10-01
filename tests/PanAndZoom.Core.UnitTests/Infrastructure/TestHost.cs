// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
namespace PanAndZoom.Core.UnitTests.Infrastructure;

/// <summary>
/// A recording <see cref="IPanAndZoomHost"/> used to drive <see cref="PanAndZoomEngine"/> without a UI framework.
/// </summary>
public class TestHost : IPanAndZoomHost
{
    public bool HasElement { get; set; } = true;

    public CoreSize ViewportSize { get; set; }

    public CoreSize ElementSize { get; set; }

    public CorePoint ElementLayoutOffset { get; set; }

    /// <summary>
    /// The engine driven by this host (used by the default virtual extension points, like the controls do).
    /// </summary>
    public PanAndZoomEngine? Engine { get; set; }

    public Func<(double minZoom, double maxZoom)>? AutoZoomLimitsOverride { get; set; }

    public Func<CoreRect>? ContentBoundsOverride { get; set; }

    public Func<CoreMatrix, bool>? ValidateTransformOverride { get; set; }

    public Action<CoreSize, CoreSize>? ResizedCallback { get; set; }

    /// <summary>
    /// Ordered log of all host calls (and any entries tests add through <see cref="Log"/>).
    /// </summary>
    public List<string> CallLog { get; } = new();

    public List<(double ZoomX, double ZoomY, double OffsetX, double OffsetY)> ViewPropertyUpdates { get; } = new();

    public List<(CoreMatrix Matrix, bool SkipTransitions)> AppliedTransforms { get; } = new();

    public List<bool> PanningChanges { get; } = new();

    public List<bool> ZoomIndicatorVisibilityChanges { get; } = new();

    public List<TimeSpan> ZoomIndicatorTimerStarts { get; } = new();

    public List<(CoreSize OldSize, CoreSize NewSize)> ResizeCalls { get; } = new();

    public List<CoreMatrix> ValidatedMatrices { get; } = new();

    public int CalculateAutoZoomLimitsCalls { get; private set; }

    public int GetContentBoundsCalls { get; private set; }

    public (CoreMatrix Matrix, bool SkipTransitions) LastTransform => AppliedTransforms[^1];

    public void Log(string entry) => CallLog.Add(entry);

    public void ClearRecords()
    {
        CallLog.Clear();
        ViewPropertyUpdates.Clear();
        AppliedTransforms.Clear();
        PanningChanges.Clear();
        ZoomIndicatorVisibilityChanges.Clear();
        ZoomIndicatorTimerStarts.Clear();
        ResizeCalls.Clear();
        ValidatedMatrices.Clear();
        CalculateAutoZoomLimitsCalls = 0;
        GetContentBoundsCalls = 0;
    }

    public virtual (double minZoom, double maxZoom) CalculateAutoZoomLimits()
    {
        CalculateAutoZoomLimitsCalls++;
        CallLog.Add(nameof(CalculateAutoZoomLimits));

        if (AutoZoomLimitsOverride != null)
        {
            return AutoZoomLimitsOverride();
        }

        return Engine?.CalculateDefaultAutoZoomLimits() ?? (double.NegativeInfinity, double.PositiveInfinity);
    }

    public virtual CoreRect GetContentBounds()
    {
        GetContentBoundsCalls++;

        if (ContentBoundsOverride != null)
        {
            return ContentBoundsOverride();
        }

        return Engine?.GetDefaultContentBounds() ?? default;
    }

    public virtual bool ValidateTransform(CoreMatrix matrix)
    {
        ValidatedMatrices.Add(matrix);
        CallLog.Add(nameof(ValidateTransform));
        return ValidateTransformOverride?.Invoke(matrix) ?? true;
    }

    public virtual void OnResized(CoreSize oldSize, CoreSize newSize)
    {
        ResizeCalls.Add((oldSize, newSize));
        CallLog.Add(nameof(OnResized));
        ResizedCallback?.Invoke(oldSize, newSize);
    }

    public virtual void UpdateViewProperties(double zoomX, double zoomY, double offsetX, double offsetY)
    {
        ViewPropertyUpdates.Add((zoomX, zoomY, offsetX, offsetY));
        CallLog.Add(nameof(UpdateViewProperties));
    }

    public virtual void ApplyTransform(CoreMatrix renderMatrix, bool skipTransitions)
    {
        AppliedTransforms.Add((renderMatrix, skipTransitions));
        CallLog.Add(nameof(ApplyTransform));
    }

    public virtual void OnIsPanningChanged(bool isPanning)
    {
        PanningChanges.Add(isPanning);
        CallLog.Add(nameof(OnIsPanningChanged));
    }

    public virtual void OnZoomIndicatorVisibilityChanged(bool isVisible)
    {
        ZoomIndicatorVisibilityChanges.Add(isVisible);
        CallLog.Add(nameof(OnZoomIndicatorVisibilityChanged));
    }

    public virtual void StartZoomIndicatorTimer(TimeSpan duration)
    {
        ZoomIndicatorTimerStarts.Add(duration);
        CallLog.Add(nameof(StartZoomIndicatorTimer));
    }
}

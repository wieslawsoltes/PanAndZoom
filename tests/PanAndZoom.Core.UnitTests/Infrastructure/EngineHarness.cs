// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
namespace PanAndZoom.Core.UnitTests.Infrastructure;

/// <summary>
/// Creates a <see cref="PanAndZoomEngine"/> with a <see cref="TestHost"/> and <see cref="PanAndZoomSettings"/>
/// and optionally simulates the control lifecycle the Avalonia and Uno adapters perform.
/// </summary>
public sealed class EngineHarness
{
    private EngineHarness(PanAndZoomEngine engine, TestHost host, PanAndZoomSettings settings)
    {
        Engine = engine;
        Host = host;
        Settings = settings;
    }

    public PanAndZoomEngine Engine { get; }

    public TestHost Host { get; }

    public PanAndZoomSettings Settings { get; }

    /// <summary>
    /// Creates an engine. By default the viewport and the element are both 400x400 at layout offset (0,0),
    /// which makes every stretch mode produce the identity matrix.
    /// </summary>
    /// <param name="configure">Optional settings configuration applied before the element is attached.</param>
    /// <param name="viewport">The viewport (control) size.</param>
    /// <param name="element">The child element size.</param>
    /// <param name="layoutOffset">The layout offset of the child element inside the control.</param>
    /// <param name="attach">True to simulate an attached element (element attach, visual tree attach, arrange, size changed).</param>
    /// <param name="clearRecords">True to clear the host call records after the simulated attach.</param>
    public static EngineHarness Create(
        Action<PanAndZoomSettings>? configure = null,
        CoreSize? viewport = null,
        CoreSize? element = null,
        CorePoint layoutOffset = default,
        bool attach = true,
        bool clearRecords = true,
        TestHost? host = null)
    {
        var settings = new PanAndZoomSettings();
        configure?.Invoke(settings);

        host ??= new TestHost();
        host.ViewportSize = viewport ?? new CoreSize(400, 400);
        host.ElementSize = element ?? new CoreSize(400, 400);
        host.ElementLayoutOffset = layoutOffset;
        host.HasElement = false;

        var engine = new PanAndZoomEngine(host, settings);
        host.Engine = engine;

        var harness = new EngineHarness(engine, host, settings);

        if (attach)
        {
            harness.Attach();

            if (clearRecords)
            {
                host.ClearRecords();
            }
        }

        return harness;
    }

    /// <summary>
    /// Simulates the lifecycle the controls perform when a child is attached and laid out:
    /// element attach, attach to visual tree, arrange and viewport size changed.
    /// </summary>
    public void Attach()
    {
        Host.HasElement = true;
        Engine.OnElementAttached();
        Engine.OnAttachedToVisualTree();
        Engine.OnArranged(Host.ViewportSize);
        Engine.OnViewportSizeChanged(Host.ViewportSize);
    }

    /// <summary>
    /// Simulates the element being removed from the control.
    /// </summary>
    public void Detach()
    {
        Host.HasElement = false;
        Engine.OnElementDetached();
    }

    /// <summary>
    /// Simulates a viewport resize the way the controls report it.
    /// </summary>
    public void Resize(double width, double height)
    {
        var size = new CoreSize(width, height);
        Host.ViewportSize = size;
        Engine.OnViewportSizeChanged(size);
    }

    /// <summary>
    /// Sets an absolute matrix without transitions.
    /// </summary>
    public void SetMatrix(double zoom, double offsetX, double offsetY)
    {
        Engine.SetMatrix(MatrixMath.ScaleAndTranslate(zoom, zoom, offsetX, offsetY), skipTransitions: true);
    }
}

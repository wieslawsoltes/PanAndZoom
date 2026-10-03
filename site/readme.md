---
title: "PanAndZoom for Avalonia and Uno Platform"
layout: simple
og_type: website
---

<div class="paz-hero">
  <div class="paz-eyebrow"><i class="bi bi-bounding-box-circles" aria-hidden="true"></i> Avalonia and Uno Platform Control and Testing Toolkit</div>
  <div class="paz-hero-split">
    <div>
      <h1>PanAndZoom for Avalonia and Uno Platform</h1>
      <p class="lead"><strong>PanAndZoom</strong> packages a production-ready <code>ZoomBorder</code> control for pan, zoom, bounds management, and view-state workflows on <strong>Avalonia</strong> and <strong>Uno Platform</strong>. Both controls are thin adapters over one UI framework independent engine, <strong>PanAndZoom.Core</strong>. <strong>HeadlessTestingFramework</strong> adds gesture simulation, tree inspection, Appium-style APIs, and visual recording for Avalonia tests.</p>
      <div class="paz-hero-actions">
        <a class="btn btn-primary btn-lg" href="articles/getting-started/overview"><i class="bi bi-rocket-takeoff" aria-hidden="true"></i> Start Getting Started</a>
        <a class="btn btn-outline-secondary btn-lg" href="articles/headless-testing"><i class="bi bi-bezier2" aria-hidden="true"></i> Explore Testing APIs</a>
        <a class="btn btn-outline-secondary btn-lg" href="api"><i class="bi bi-braces-asterisk" aria-hidden="true"></i> Browse API</a>
      </div>
    </div>
    <div class="paz-hero-media">
      <img src="images/panandzoom.png" alt="PanAndZoom sample application" />
    </div>
  </div>
</div>

## Start Here

<div class="paz-link-grid">
  <a class="paz-link-card" href="articles/getting-started/installation">
    <span class="paz-link-card-title"><i class="bi bi-download" aria-hidden="true"></i> Installation</span>
    <p>Package setup for the Avalonia and Uno Platform controls, headless test prerequisites, and the first integration checks.</p>
  </a>
  <a class="paz-link-card" href="articles/getting-started/quickstart-zoom-border">
    <span class="paz-link-card-title"><i class="bi bi-arrows-move" aria-hidden="true"></i> Quickstart: ZoomBorder</span>
    <p>Create a zoomable surface, bind commands, and wire pointer and keyboard interaction.</p>
  </a>
  <a class="paz-link-card" href="articles/getting-started/quickstart-uno">
    <span class="paz-link-card-title"><i class="bi bi-phone" aria-hidden="true"></i> Quickstart: Uno Platform</span>
    <p>Add <code>PanAndZoom.Uno</code> to an Uno app and run the same control on desktop, WebAssembly, Android, iOS, and Windows.</p>
  </a>
  <a class="paz-link-card" href="articles/getting-started/quickstart-headless-testing">
    <span class="paz-link-card-title"><i class="bi bi-bug" aria-hidden="true"></i> Quickstart: Headless Testing</span>
    <p>Drive Avalonia controls with touch, keyboard, and Appium-style APIs inside headless tests.</p>
  </a>
  <a class="paz-link-card" href="articles/samples">
    <span class="paz-link-card-title"><i class="bi bi-window-stack" aria-hidden="true"></i> Sample Walkthrough</span>
    <p>Map the sample app tabs to concrete ZoomBorder features and testing scenarios.</p>
  </a>
</div>

## Packages

<div class="paz-link-grid paz-link-grid--wide">
  <a class="paz-link-card" href="articles/intro">
    <span class="paz-link-card-title"><i class="bi bi-zoom-in" aria-hidden="true"></i> PanAndZoom</span>
    <p>Avalonia <code>ZoomBorder</code>, matrix helpers, commands, history, state persistence, bounds control, and advanced viewport utilities.</p>
  </a>
  <a class="paz-link-card" href="articles/getting-started/quickstart-uno">
    <span class="paz-link-card-title"><i class="bi bi-phone" aria-hidden="true"></i> PanAndZoom.Uno</span>
    <p>Uno Platform (WinUI) <code>ZoomBorder</code> with the same feature set, WinUI manipulations for touch, and logical scroll state.</p>
  </a>
  <a class="paz-link-card" href="articles/concepts/architecture">
    <span class="paz-link-card-title"><i class="bi bi-cpu" aria-hidden="true"></i> PanAndZoom.Core</span>
    <p>The UI framework independent <code>PanAndZoomEngine</code>, host and settings contracts, core primitives, and shared enums.</p>
  </a>
  <a class="paz-link-card" href="articles/headless-testing">
    <span class="paz-link-card-title"><i class="bi bi-camera-video" aria-hidden="true"></i> HeadlessTestingFramework</span>
    <p>Input simulation, tree queries, template inspection, recording, video conversion, and Appium-like interaction layers.</p>
  </a>
</div>

## Documentation Sections

<div class="paz-link-grid paz-link-grid--wide">
  <a class="paz-link-card" href="articles/getting-started">
    <span class="paz-link-card-title"><i class="bi bi-signpost-split" aria-hidden="true"></i> Getting Started</span>
    <p>Choose the right package, install it, and get your first sample or test working quickly.</p>
  </a>
  <a class="paz-link-card" href="articles/concepts">
    <span class="paz-link-card-title"><i class="bi bi-diagram-3" aria-hidden="true"></i> Concepts</span>
    <p>Engine architecture, coordinate systems, transformation state, gestures, commands, and persistence mental models.</p>
  </a>
  <a class="paz-link-card" href="articles/guides">
    <span class="paz-link-card-title"><i class="bi bi-journal-code" aria-hidden="true"></i> Guides</span>
    <p>Scenario-driven recipes for bounds management, view history, keyboard control, zoom-to-rectangle, and more.</p>
  </a>
  <a class="paz-link-card" href="articles/headless-testing">
    <span class="paz-link-card-title"><i class="bi bi-cpu" aria-hidden="true"></i> Headless Testing</span>
    <p>Testing workflows spanning gesture simulation, tree introspection, Appium-like APIs, and recording.</p>
  </a>
  <a class="paz-link-card" href="articles/advanced">
    <span class="paz-link-card-title"><i class="bi bi-speedometer2" aria-hidden="true"></i> Advanced</span>
    <p>Custom bounds and resize hooks, ScrollViewer integration, diagnostics, and project-level testing strategy.</p>
  </a>
  <a class="paz-link-card" href="articles/reference">
    <span class="paz-link-card-title"><i class="bi bi-collection" aria-hidden="true"></i> Reference</span>
    <p>Namespace maps, API coverage, Lunet pipeline details, and licensing.</p>
  </a>
</div>

## Repository

- Source code and issues: [github.com/wieslawsoltes/PanAndZoom](https://github.com/wieslawsoltes/PanAndZoom)
- Published docs target: [wieslawsoltes.github.io/PanAndZoom](https://wieslawsoltes.github.io/PanAndZoom)

---
title: "Lunet Docs Pipeline"
---

# Lunet Docs Pipeline

This repository uses Lunet to publish PanAndZoom guides and generated API reference.

## Site Structure

- `site/config.scriban`: Lunet config, project metadata, and `.NET API` generation
- `site/menu.yml`: top-level navigation
- `site/readme.md`: documentation home page
- `site/articles/**`: narrative documentation
- `site/articles/**/menu.yml`: section sidebars
- `site/images/**`: shared docs assets
- `site/.lunet/css/template-main.css`: precompiled template stylesheet
- `site/.lunet/css/site-overrides.css`: project-specific visual customizations

## API Generation

The API site is generated from:

- `../src/PanAndZoom/PanAndZoom.csproj`

The Lunet `api.dotnet` block builds the `net8.0` target and publishes generated API pages under `/api`.

## Styling Pipeline Note

As with TreeDataGrid, this site checks in a precompiled template stylesheet because Lunet `1.0.10` on macOS 15 can hit a Dart Sass platform detection issue at build time.

The checked-in assets that support that workflow are:

- `site/.lunet/includes/_builtins/bundle.sbn-html`
- `site/.lunet/css/template-main.css`
- `site/.lunet/layouts/_default.api-dotnet*.sbn-md`

## Commands

From repository root:

```bash
./build-docs.sh
./check-docs.sh
./serve-docs.sh
```

PowerShell:

```powershell
./build-docs.ps1
./serve-docs.ps1
```

All commands run Lunet in `site/` and write output to `site/.lunet/build/www`.

## CI Publishing

`.github/workflows/docs.yml` builds the site and publishes `site/.lunet/build/www` to GitHub Pages on pushes to `main` and `master`.

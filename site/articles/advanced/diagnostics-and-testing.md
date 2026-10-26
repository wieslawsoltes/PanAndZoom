---
title: "Diagnostics and Testing"
---

# Diagnostics and Testing

The `Avalonia.Controls.PanAndZoom.UnitTests` project verifies the control's behavior using the published [ProTest](https://wieslawsoltes.github.io/ProTest/) headless testing packages.

## Test Coverage Areas

- pointer, wheel, keyboard, and gesture interaction
- bounds modes and dynamic zoom limits
- view history, saved views, and state serialization
- rotation, scale indicator, viewport culling, and `ILogicalScrollable`
- control integration with input simulation, tree inspection, automation, and recording

The testing framework's implementation and standalone tests are maintained in [the ProTest repository](https://github.com/wieslawsoltes/ProTest). Its [documentation](https://wieslawsoltes.github.io/ProTest/) covers helper APIs, recording artifacts, and test runner setup.

## Documentation Validation

Validate docs locally with:

```bash
./check-docs.sh
```

That rebuilds the Lunet site and PanAndZoom API reference from the same sources used by CI.

# Changelog

## 0.1.1 — 2026-11-05

### Fixed

- Addressed the reported `ItemFactory.EnsureRegistered` failure: `Sequence contains no matching element` during tracker discovery.
- Replaced the name-filtered `First` lookup with component-based discovery across the configured resource folder, loaded definitions and the item registry.
- Made an embedded battery display optional for donor selection. When absent, create a private, non-rendering native display copy with subtree-owned references; preserve native charging bookkeeping and the shared HUD.
- Defer discovery while resources or display templates are unavailable rather than permanently disabling registration after an empty initial search.
- Preserve required physics, inventory, battery and network checks; explicitly exclude native revival-robot prefabs.
- Log examined definitions, missing components and display-readiness status instead of a generic LINQ exception.

### Validation

- 45 core tests and 26 game API metadata checks passed; Release build completed with zero warnings/errors.
- The supplied game log confirms the old failure is in donor selection. It does not identify which old predicate condition excluded the live prefab.
- New donor discovery and private-display initialization still require an in-game retest. No claim of verified shop spawning or charging is made yet.

## 0.1.0 — presentation and investigation updates

### Investigation

- Documented custom versus native revival-item identity and the startup/registration/shop log markers for the reported missing shop item. At that investigation stage, the runtime cause was still pending the host's test log. The later donor-selection error and correction are recorded under 0.1.1.

### Changed

- Added original AI-generated industrial cover art featuring revival and electrical aftereffects.
- Replaced the package icon with a 256 × 256 adaptation of the cover.
- Included the full cover in the README and local test package.
- Replaced the legacy icon drawing script with an ImageMagick resize helper that preserves the cover source.
- Refreshed the v0.1.0 local test archive and checksum; gameplay binaries are unchanged.

## 0.1.0 — 2026-11-05

### Added

- BepInEx plugin targeting supplied R.E.P.O. 0.5.1 references and REPOLib 4.3.0.
- One reserved defibrillator shop slot and configurable zero default price.
- Host-authoritative hold-to-revive state machine with native death-head targeting.
- Three-second activation, two-meter range, one-HP revival and 25% charge consumption by default.
- Cancellation on release, damage, drop, invalid round, lost aim/range/visibility and input timeout.
- Per-death replay protection and concurrent-use/target reservations.
- Original procedural industrial visuals, synthesized audio and package icon.
- Native battery charging, persistence and inventory integration.
- Host-selected 50/50 comic voice pitch (1.8x / 0.55x), ending after 180 seconds, death or level end.
- Harmless host-scheduled aftershocks every 30 seconds until death or level end, with local camera shake and synchronized electrical arcs.
- Master-verified late-join effect snapshots, per-revival tokens and duplicate-shock protection.
- Build/package scripts, core CI workflow template and English testing/development documentation.
- 40 core behavior tests and 26 game API metadata checks.
- Inventory of the supplied mod profile without copying private configuration files.

### Validation

- Release compilation succeeded with zero warnings and zero errors.
- All 40 core tests and 26 game API metadata checks passed locally.
- In-game and multiplayer acceptance checks have not run. This is an experimental candidate.

### Known limitations

- Activation requires physically holding the device, not inventory-slot equipment.
- Native battery overlay has six bars; custom casing indicators represent quarters.
- Visual fit, charging/persistence, late join, host migration and full-profile compatibility require game testing.

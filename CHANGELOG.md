# Changelog

## Unreleased — presentation update

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

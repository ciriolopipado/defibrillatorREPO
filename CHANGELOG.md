# Changelog

## 0.1.3 — 2026-11-07

### Changed

- Increased default aftershock camera shake from 3.5 / 0.8 seconds to 7 / 1.2 seconds, with a once-only migration of legacy defaults.
- Added owner-only horizontal aim jolt up to 180 degrees using native additive aim, spread over at most 0.65 seconds. Player input remains available and no camera transform/position is overwritten.
- Added a configurable 25% host-rolled probability of native Q/tumble posture per eligible aftershock. The host synchronizes the native state and skips already-tumbling players.
- The forced posture does not trigger the voluntary tumble-launch upgrade, add direct damage or impose an extra get-up timer beyond the native behavior.
- Added host-verified impact packet fields for signed yaw and tumble choice, using protocol version 2. All players must update together.
- Preserved the existing electrical visuals, revival, battery, price and compact item physics.

### Validation

- Release build succeeded with zero warnings/errors; 61 core tests and 32 game/Photon API metadata checks passed.
- The user reported that the previous build worked well overall and the electrical visuals looked good, but the impact felt weak.
- The stronger jolt, native tumble, host/client presentation and recovery require an in-game retest.

## 0.1.2 — 2026-11-06

### Changed

- Replaced donor-bounds-dependent model scaling with a fixed compact scale: approximately 35 cm wide including paddles, with a nominal 3 kg Unity mass.
- Removed inherited colliders/markers and rebuilt six native grabbable box colliders around the casing, paddles and handle; cables remain cosmetic.
- Updated native mass originals and a private physics-preset copy so native startup does not restore the lightweight donor mass. Adjusted damping, center of mass, grab/shelf anchors and room overlap size.
- Normalized saved/network scale before native physics initialization to prevent old donor scale from reintroducing oversized collision bounds.
- Set default shop price to 2k and battery cost to 50%, allowing two revivals per full charge.
- Added a one-time migration of legacy free-price/four-use defaults while preserving other customized values and existing voice/aftershock settings.

### Validation

- 53 core tests and 28 API metadata checks passed; Release build completed without warnings/errors.
- Added balance migration, compact geometry-envelope and two-use battery regressions, plus native physics-init and Photon scale-setter checks.
- The user's preceding game test established an oversized model and unrealistic weight/collision behavior. The new physical behavior still requires in-game validation; automated checks do not establish weight feel or collision quality.

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

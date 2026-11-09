# Validation record — 0.1.4

## Completed locally

- SDK: .NET 8.0.427, Linux x64.
- Target framework: netstandard2.1 for plugin/core; net8.0 for test runners.
- Plugin: Release compilation against user-supplied Managed assemblies, zero warnings and zero errors.
- Core behavior runner: **69 executed, 69 passed, 0 failed**.
- API/resource runner: **44 executed, 44 passed, 0 failed** (40 native API checks and four embedded WAV equality checks).
- Tests include one-HP revival, two default battery uses and configurable capacity, one-time balance migration, compact collision envelope, cancellation, concurrency, repeated death identities, voice expiry, lifetime boundaries, shock intervals and no catch-up bursts.
- API checks validate every Harmony target's method arity, required private fields, native pitch/camera methods and the reflective shop method's ref-int parameter.
- Game-reference fingerprints are recorded separately; no reference DLLs are distributed.
- Package input is an explicit allowlist of original DLLs, metadata, cover art, icon and documentation.
- Presentation update: the cover is an opaque PNG, and the adapted Thunderstore icon is 256 × 256 RGB PNG. The earlier presentation-only update preserved gameplay DLLs; the 0.1.1 and 0.1.2 gameplay archives intentionally contain rebuilt DLLs for their respective fixes.

## Runtime evidence supplied by the user

The v0.1.0 plugin entered item registration and threw `InvalidOperationException: Sequence contains no matching element` from the name/component-filtered `Enumerable.First` lookup in `ItemFactory.EnsureRegistered`.
The displayed shop object was described as a robot with a face and propeller costing 47k, consistent with the independent native revival robot rather than the custom zero-price item.
Version 0.1.1 corrects donor discovery and optional display handling. The error does not identify which individual old filter condition failed, and this environment cannot verify the live prefab contents.

## Size/physics feedback and 0.1.2 changes

The user subsequently reported an enormous visible object with unrealistic weight and collision, and requested a smaller object, meaningful weight, a 2k price and two battery uses.
The model previously scaled from donor renderer bounds and retained native donor collision. `NotValuableObject.Start` also reapplies mass from its physics preset. The new implementation uses fixed compact model coordinates and matching compound boxes, updates the private mass preset, and normalizes the saved scale payload before native physics bounds are calculated.
Nominal mass is 3 Unity mass units (conventionally kilograms), with an approximately 35 cm width. These are configured values, not measured in-game outcomes. Weight feel, collision response, shelf/floor placement and old-save restoration remain unverified.

## Aftershock feedback and 0.1.3 changes

The user reported that the preceding build worked well overall and its shock visuals looked good, while the physical/camera impact was too weak. They requested significant horizontal camera motion up to 180 degrees and a probability of the native Q posture.
The game source identifies Q as tumble, not literal character scaling. The new host policy uses a 25% default probability and synchronizes the native posture with `TumbleSet(true, false)`. Camera yaw is owner-local via `AdditiveAimX`, with no input lock. Tests check probability boundaries, bounded turn totals at variable frame rates, and migration of weak defaults. Metadata checks cover both native APIs and the target avatar fields.
Visual presentation, native recovery, indirect environmental interactions and camera response must still be observed in multiplayer.

## Animal audio — 0.1.4

The user supplied four MP3s and Pixabay source links. Processed clips are mono PCM16 at 44100 Hz, with leading silence trimmed, normalization target -18 LUFS / -2 dBTP, short fades and a hard three-second cap. Actual lengths: dog 1.000 s, sheep 1.543 s, horse 2.492 s, cat 3.000 s.
Core checks decoded all four recordings as non-silent/non-clipped and reject invalid/truncated/overlength WAVs. The API/resource runner compared each embedded WAV against its validated source bytes. The animal source uses pitch 1, Doppler 0 and neutral voice routing; confirming natural timbre and restoration still requires real game playback.
Source attributions and the blocked online license-check result are in `docs/AUDIO.md` and the source manifest. Original user MP3s are not redistributed.

## Not established

No R.E.P.O. executable was run in this development environment, and v0.1.4 has not been retested by the user yet. Automated tests do not establish actual RPC delivery, microphone/audio behavior, camera presentation, shader appearance, save compatibility, shop interaction or charger behavior.
The complete supplied mod profile, late joining, host migration and actual multiplayer revival remain untested.
An optional core CI workflow template is included under `docs/ci`; it is not active CI. No remote CI run has been observed.
- The complete source, original icon and test ZIP were uploaded to `feature/defibrillator-source`. Direct pushes to `main` were rejected with `fatal error in commit_refs_rules`; the server supplied no root cause.

Use the acceptance checklist to record game-test outcomes before labeling any release stable.

## Reference provenance

- Game references: private `Managed.rar` supplied by the user for the reported game version 0.5.1.
- BepInEx compile references: official BepInEx 5.4.23.5 Windows x64 release, corresponding to the requested pack generation.
- REPOLib compile reference: published Zehs.REPOLib 4.3.0 NuGet package. Signature verification ran; online certificate-revocation endpoints were unreachable and produced warnings. Verification was not disabled.
- Public REPOLib source inspected at commit `76128a80ec0dcbe901c823356fd28c78ca30ad8e`.
- Profile export: package names/versions only; private configuration contents are not included.

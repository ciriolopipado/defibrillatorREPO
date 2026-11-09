# Development

Use .NET SDK **8.0.427** or an allowed patch from `global.json`.
Use the existing checkout: cloud tasks already have an isolated workspace. Do not create a worktree unless explicitly requested.

## References

Copy the game's `REPO_Data/Managed` DLLs into ignored `local-libs/`. Add these DLLs from the installed profile:

- `BepInEx/core/BepInEx.dll`
- `BepInEx/core/0Harmony.dll`
- `REPOLib.dll` from Zehs-REPOLib **4.3.0**

The public game-reference NuGet package only covered versions through 0.4.4 when investigated. It was not used for this build.
Game DLLs and the supplied archives must remain local and must not be redistributed.

```bash
# Core suite; no game references needed:
bash scripts/verify.sh

# Core suite, plugin build, API metadata checks:
bash scripts/verify-game.sh

# Alternative reference location:
dotnet build src/Defibrillator.Plugin/Defibrillator.Plugin.csproj -c Release -p:GameReferencePath=/absolute/path/to/references

# Explicit allowlist-only Thunderstore package:
python3 scripts/package.py
```

PowerShell users can run the individual dotnet commands from the shell scripts directly.
The test runner reports counts and exits nonzero on failure. API checks read metadata without loading Unity.
The workflow template in `docs/ci/core-workflow.example.yml` runs the core suite only. It is not active CI; game references must not be uploaded to GitHub Actions.
The package icon can be resized from the preserved cover using `bash scripts/create-icon.sh` with ImageMagick installed. See `thunderstore/ARTWORK.md` for artwork provenance.

## Architecture

- `Defibrillator.Core`: host-side timing, target leases, charge, cancellation and replay protection.
- `Plugin`: configuration, patch registration and host lifecycle.
- `ItemFactory`: component-based native tracker-shell selection and REPOLib registration.
- `BatteryDisplayFallback`: clone only self-contained native display subtrees into a private, non-rendering canvas when the donor lacks a display. The live HUD is never rebound.
- `Patches`: price-floor exception, reserved stock, damage revisions and completed-death tracking.
- `DevicePhysics` / `DeviceGeometry`: fixed 0.5 model scale, six compound colliders, 3 kg private mass preset, native grab/shelf/room anchors and saved-scale normalization.
- `BalanceMigration`: once-only conversion of old test defaults, with a persistent config revision and preservation of other custom values.
- `Device`: held-item input, native battery synchronization and feedback.
- `NetworkBridge`: REPOLib events, authenticated Photon sender ownership and host-only outcomes.
- `GameWorld` / `GameAccess`: version-sensitive observations and native revival.
- `VoiceDebuffs`: host-selected voice/aftershock state, timed native pitch overrides and late-join snapshots.
- `AftershockVisual`: existing local native shake and original electrical arcs.
- `AftershockCamera`: owner-only incremental native additive yaw; stops on death, menus or level exit, and does not lock input.
- `AftershockImpact`: bounded yaw curve, probability boundaries and once-only weak-shake migration.
- Native Q posture is applied once by the host through `PlayerTumble.TumbleSet(true, false)`, avoiding the voluntary launch upgrade. Impact packets in 0.1.3 use protocol 2; 0.1.4 uses protocol 3 for the added animal events/snapshots. Impact packets carry the host-selected signed angle and boolean decision.
- `AnimalClips` / `PcmWave`: strict decoding of four embedded, normalized PCM clips, each at most three seconds.
- `AnimalVoicePlayback`: neutral positional playback, temporary native volume suppression, and reversible speech-state overlays around native updates and speech RPCs. Microphone capture settings are never changed.
- `OriginalAssets`: original visuals and audio synthesis.

Client requests cannot set charge, health, position, elapsed time or range. The host observes those independently.
A death identity advances at `PlayerDeathDone`, after the native disabled-state transition.
Native revival grants one HP on the owner; additional configured health uses native healing.
Host changes cancel pending attempts and reconstruct charge from native item snapshots. This remains untested in game.
Aftereffect tokens prevent duplicate shock presentation. Missed host ticks produce one shock rather than a catch-up burst. All effect snapshots and pulse events must come from the current master; client requests cannot select pitch or trigger shocks. Voice expiration does not end the longer-lived electrical aftershocks.
Use the same mod set across clients because REPOLib assigns custom event codes by registration order.

## Release procedure

1. Run all automated checks with the target references.
2. Complete `docs/TESTING.md` in the actual game; record unrun and failed checks honestly.
3. Update plugin/assembly/manifest version and documentation together.
4. Run the package script and verify the ZIP contents/checksum.
5. Commit and push English source, documentation and the test ZIP. Exclude third-party binaries and user archives.

Only mark a release stable after in-game acceptance testing passes.

## Third-party audio assets

Builds embed `assets/audio/*.wav` directly in the plugin. The package includes source attribution but no standalone WAV/MP3 files. See `docs/AUDIO.md` and `assets/audio/SOURCES.json`; their source terms remain separate from project code/art. Core tests need the four checked-in WAV build assets, but no game references.

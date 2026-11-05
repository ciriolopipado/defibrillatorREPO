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
- `ItemFactory`: deterministic native tracker-shell selection and REPOLib registration.
- `Patches`: price-floor exception, reserved stock, damage revisions and completed-death tracking.
- `Device`: held-item input, native battery synchronization and feedback.
- `NetworkBridge`: REPOLib events, authenticated Photon sender ownership and host-only outcomes.
- `GameWorld` / `GameAccess`: version-sensitive observations and native revival.
- `VoiceDebuffs`: host-selected voice/aftershock state, timed native pitch overrides and late-join snapshots.
- `AftershockVisual`: local camera shake and original electrical arcs; no health, stun or physics calls.
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

# Industrial Defibrillator for R.E.P.O.

**0.1.0 is an experimental test candidate, not a verified stable release.**
The plugin compiles against the Managed references supplied for R.E.P.O. 0.5.1.
Automated core and API metadata checks pass. The game has not been run in this environment.

## Requirements

- R.E.P.O. **0.5.1**, Steam.
- **BepInEx-BepInExPack-5.4.2305**.
- **Zehs-REPOLib-4.3.0**.
- Every player, including the host, must install the same mod and dependency versions.

## Install the local test build

1. Download `releases/Industrial_Defibrillator-0.1.0.zip` from this repository.
2. Create a separate Thunderstore Mod Manager profile and install the two dependencies above.
3. Use **Settings > Import local mod** if available. Alternatively, open the profile folder and extract the ZIP's `BepInEx` directory into it.
4. Check that `Defibrillator.dll` and `Defibrillator.Core.dll` are together in `BepInEx/plugins/IndustrialDefibrillator/`.
5. Launch through Thunderstore on every machine. Check `BepInEx/LogOutput.log` for the plugin load and item registration messages.
6. Use a new test save and follow [the multiplayer checklist](docs/TESTING.md).

This ZIP has **not** been published to Thunderstore. No game, BepInEx or REPOLib DLLs are bundled.

## Default behavior

| Setting | Initial behavior |
| --- | --- |
| Shop | One reserved slot per normal shop population; price **0** |
| Use | Physically grab the device, aim at a dead player's **head**, hold **Interact** |
| Timing and range | **3 seconds**, within **2 meters** from the operator's camera to the head |
| Cancellation | Release, drop the device, move out of range, lose aim or line of sight, take damage, or leave the active round |
| Cancelled attempt | No battery consumed; a new attempt starts at zero |
| Revival | Native revival with **1 HP** |
| Battery | **25%** per successful revival; **four** uses per full charge |
| Charging | Native `ItemBattery` charging and persistence |
| Networking | Host checks identity, ownership, target, range, timing, damage and battery |
| Revival aftereffects | 50/50 high (1.8x) or low (0.55x) voice for up to 180 seconds; harmless electrical shock every 30 seconds |
| Assets | Original industrial model and synthesized sounds generated at runtime |

A vanilla tracker provides the runtime physics/inventory/battery shell. Its tracker behavior is removed and its visible model is replaced by original casing, paddles, cables and charge indicators. No Unity editor or AssetBundle is required.
Use from an equipped inventory slot is not implemented: physically hold the device.
The native battery overlay retains six bars; the casing has four 25% indicators.
The purchase limit is one new unit **per shop**, not one across the entire run.

The voice effect ends at 180 seconds, death or level end, whichever comes first. Electrical aftershocks continue for the rest of the level until death, even after the voice has returned to normal. The first shock occurs 30 seconds after revival. Each shock shakes only the affected player's camera and shows original blue electrical arcs to all players; it applies no damage, stun or physical impulse. Native camera-shake accessibility settings remain effective.

## Configuration

After first launch, edit `BepInEx/config/ciriolopipado.defibrillator.cfg` with the game closed, then restart.
The host's configuration controls validation and price; clients receive progress and outcomes.

```ini
[Revival]
HoldSeconds = 3
RangeMeters = 2
Health = 1

[Battery]
CostPercent = 25

[Shop]
Price = 0

[VoiceDebuff]
Seconds = 180
HighPitch = 1.8
LowPitch = 0.55

[Aftershock]
IntervalSeconds = 30
CameraShakeStrength = 3.5
CameraShakeSeconds = 0.8
```

Price uses displayed shop currency units. Invalid values disable the plugin with an error log.

## Evidence and limitations

- Passed: Release compilation without warnings/errors, 40 core behavior tests and 26 game API metadata checks.
- Unrun: in-game startup, shop purchase, visuals, actual charging/persistence, voice playback, aftershock presentation, network execution, late joining, host migration and compatibility with other mods.
- Automated core tests simulate the game adapter; they do not establish multiplayer stability.
- Hold messages are sent every 0.15 seconds. A gap above 0.75 seconds resets progress; high latency can interrupt use. Aftereffect snapshots are requested every three seconds to recover late joins without restarting the voice timer.
- An active recoverable death head is required.
- Procedural model fit, shader appearance and collision-shell alignment need in-game inspection.
- If a shop exposes no item volume, guaranteed placement cannot proceed and is logged.

See [development](docs/DEVELOPMENT.md), [testing](docs/TESTING.md), [profile inventory](docs/MOD_PROFILE.md) and [changelog](CHANGELOG.md).

## Conventions

Documentation, comments, changelog entries and commits use English. Progress is committed and pushed in coherent increments.
Never commit game binaries, credentials, user archives or third-party assets without redistribution permission.

# Defibrillator for R.E.P.O.

A planned multiplayer mod that adds a rechargeable defibrillator to the shop.

## Target environment

- R.E.P.O. 0.5.1, launched through Steam and Thunderstore.
- BepInExPack 5.4.2305.
- All players must install the mod.

## Agreed initial behavior

- Always offer one unit per shop for a test price of 0.
- Aim at the recoverable remains of a dead player and hold activation for 3 seconds within 2 meters.
- Cancel activation when the user releases the button, moves out of range, or takes damage.
- Cancelled activations consume no battery.
- Revive the target with 1 health point.
- Start with a full battery; each completed revival consumes 25%, allowing four revivals per charge.
- Recharge through the game's charger, subject to API verification.
- Use original industrial-style models and sounds.
- Validate revivals on the host and prevent duplicate revivals of the same player.
- Make price, activation duration, range, battery consumption, and revival health configurable.

## Development status

Requirements are recorded. No playable mod or assets have been implemented yet.
Game APIs, compatible supporting libraries, the exact revival target, and charger integration still require verification.
Multiplayer stability must be validated in the game with at least two players.

## Project conventions

Documentation, changelog entries, code comments, and commit messages are written in English.
Progress is committed and pushed to this repository in coherent increments.
Never commit game binaries, credentials, or third-party assets without redistribution permission.

## Next steps

1. Verify game APIs and select compatible dependencies.
2. Implement item registration, host-validated revival, battery handling, and configuration.
3. Create and integrate original visual and audio assets.
4. Build a Thunderstore package and validate shop, charging, cancellation, revival, and multiplayer behavior.

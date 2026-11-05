# In-game acceptance tests

## Evidence

Release compilation, 45 core behavior tests and 26 API metadata checks passed in the development environment.
All game scenarios below are **unrun for v0.1.1**. The user tested v0.1.0 and reported a confirmed registration failure in donor discovery; shop availability did not pass. Reference DLLs do not provide a playable game installation.

## Preparation

Use two players on R.E.P.O. 0.5.1 with the same v0.1.1 test ZIP (remove the old installation first), BepInExPack 5.4.2305 and REPOLib 4.3.0.
Create a separate profile and a new save. First test without optional mods.
Preserve `BepInEx/LogOutput.log` from both machines after the session.

| ID | Scenario | Expected | Status |
| --- | --- | --- | --- |
| S01 | Start and enter a normal shop | Plugin/item registration messages; no exceptions | Unrun |
| S02 | Inspect stock | Exactly one distinct Industrial Defibrillator | Unrun |
| S03 | Buy at zero currency | Price 0; purchase succeeds without currency loss | Unrun |
| S04 | Carry/store/retrieve | Native inventory behavior; no tracker functionality | Unrun |
| R01 | Hold Interact on a dead head within 2 meters for 3 seconds | Revival at head location, 1 HP, 75% remaining charge | Unrun |
| R02 | Swap host/client roles | Both roles can revive; both agree on outcome and charge | Unrun |
| R03 | Release before completion | Reset progress; no charge spent | Unrun |
| R04 | Move away, lose aim or put a wall in the way | Cancel with no charge loss | Unrun |
| R05 | Take damage, including followed by healing | Cancel with no charge loss | Unrun |
| R06 | Drop while charging | Cancel; another operator can start fresh | Unrun |
| R07 | Revive the same player through four separate deaths | Charge 75/50/25/0%; repeated deaths remain eligible | Unrun |
| R08 | Try a fifth use without charging | No revival; recharge message | Unrun |
| R09 | Two devices target one dead player | One completion and one battery deduction only | Unrun |
| R10 | Target a living player or unavailable head | No revival or charge loss | Unrun |
| B01 | Recharge at a powered native station | Charge increases; four uses after full recharge | Unrun |
| B02 | Save, exit and resume partially charged | Native charge restored without a mod-induced refill | Unrun |
| B03 | Level/shop transitions | Native item identity and charge rules remain consistent | Unrun |
| V01 | Inspect model and icon | Original art; no missing/pink materials or large collision mismatch | Unrun |
| V02 | Start/complete near another player | Charge cue locally; discharge once on nearby clients | Unrun |
| D01 | Revive and speak over the game voice chat | Every listener hears the same clearly high or low voice; other players remain unchanged | Unrun |
| D02 | Remain alive for over 180 seconds | Voice returns to normal; aftershocks continue | Unrun |
| D03 | Wait for 30/60/90 seconds after revival | One visible/audible shock at each interval; affected player's camera shakes; HP remains unchanged | Unrun |
| D04 | Observe D03 as another player | Electrical arcs visible on target; observer's camera unaffected | Unrun |
| D05 | Die again or end the level during either effect | Both effects stop; a later revival rolls a new pitch | Unrun |
| D06 | Late join while voice effect is active | Same pitch within the next snapshot; original expiry preserved, no replay of past shocks | Unrun |
| N01 | Operator disconnects mid-attempt | No delayed revival; another operator can start | Unrun |
| N02 | Host changes if the game supports continuation | Progress resets; charge preserved; no duplicate revival | Unrun |
| C01 | Change host settings and restart | Host price/duration/range/health/cost govern results | Unrun |
| M01 | Repeat shop, revive, damage, four-use and charger tests with the full supplied profile | No errors or feature interference | Unrun |
| M02 | Late join with a depleted/partially charged device | Correct prefab/charge; revival remains functional | Unrun |

Native station power requirements and native refill rules are preserved.

## Reporting

Include ZIP checksum, game version, mod list, host/client role, scenario ID, expected/observed result and both logs.
For visual failures include a screenshot. For HP failures record upgrades and other healing/revival mods.
Remove private information from logs; do not post game DLLs publicly.

Registration failures begin with `Item registration failed`. Missing stock should have a shop reservation/error message.
For repeated timer resets check aim, head availability, distance, damage and latency.

## Removal

Close the game and remove/disable this mod in the test profile. For manual installs remove its plugin directory.
Removing custom items from existing saves is untested; use a separate test save.

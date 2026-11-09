# In-game acceptance tests

## Evidence

Release compilation, 69 core behavior tests and 44 API/embedded-resource checks passed in the development environment.
All game scenarios below are **unrun for v0.1.4**. The user tested v0.1.0 and reported a confirmed registration failure in donor discovery; shop availability did not pass then. Subsequent tests reported that the object appeared but was oversized, with unrealistic weight and hitboxes. The user subsequently reported that the build worked well overall and the shock visuals looked good, but its impact felt weak. This is qualitative feedback, not a completed acceptance matrix. Reference DLLs do not provide a playable game installation.

## Preparation

Use two players on R.E.P.O. 0.5.1 with the same v0.1.4 test ZIP (remove the old installation first), BepInExPack 5.4.2305 and REPOLib 4.3.0.
Create a separate profile and a new save. First test without optional mods.
Preserve `BepInEx/LogOutput.log` from both machines after the session.

| ID | Scenario | Expected | Status |
| --- | --- | --- | --- |
| S01 | Start and enter a normal shop | Plugin/item registration messages; no exceptions | Unrun |
| S02 | Inspect stock | Exactly one distinct Industrial Defibrillator | Unrun |
| S03 | Buy with at least 2k, then try without enough currency | Price 2k; successful purchase deducts 2k; insufficient funds do not buy the item | Unrun |
| S04 | Carry/store/retrieve | Native inventory behavior; no tracker functionality | Unrun |
| R01 | Hold Interact on a dead head within 2 meters for 3 seconds | Revival at head location, 1 HP, 50% remaining charge | Unrun |
| R02 | Swap host/client roles | Both roles can revive; both agree on outcome and charge | Unrun |
| R03 | Release before completion | Reset progress; no charge spent | Unrun |
| R04 | Move away, lose aim or put a wall in the way | Cancel with no charge loss | Unrun |
| R05 | Take damage, including followed by healing | Cancel with no charge loss | Unrun |
| R06 | Drop while charging | Cancel; another operator can start fresh | Unrun |
| R07 | Revive the same player through two separate deaths | Charge 50/0%; repeated deaths remain eligible | Unrun |
| R08 | Try a third use without charging | No revival; recharge message | Unrun |
| R09 | Two devices target one dead player | One completion and one battery deduction only | Unrun |
| R10 | Target a living player or unavailable head | No revival or charge loss | Unrun |
| B01 | Recharge at a powered native station | Charge increases; two uses after full recharge | Unrun |
| B02 | Save, exit and resume partially charged | Native charge restored without a mod-induced refill | Unrun |
| B03 | Level/shop transitions | Native item identity and charge rules remain consistent | Unrun |
| P01 | Inspect/hold/drop the device | Approximately 35 cm wide; compact casing, recognizable 3 kg weight feel; no giant invisible hitbox | Unrun |
| P02 | Grab casing, paddles and handle; move near shelves/walls | Collision follows solid parts; empty handle opening and cables do not create a giant solid box | Unrun |
| P03 | Store/retrieve, transition levels and restore an older saved device | Compact scale and collision remain consistent; nominal mass returns after native equip overrides | Unrun |
| P04 | Upgrade a config containing Price=0 and CostPercent=25 | Once-only migration gives Price=2 and CostPercent=50; unrelated settings are preserved | Unrun |
| V01 | Inspect model and icon | Original art; no missing/pink materials or large collision mismatch | Unrun |
| V02 | Start/complete near another player | Charge cue locally; discharge once on nearby clients | Unrun |
| D01 | Revive and speak over the game voice chat | Every listener hears the same clearly high or low voice; other players remain unchanged | Unrun |
| D02 | Remain alive for over 180 seconds | Voice returns to normal; aftershocks continue | Unrun |
| D03 | Wait for 30/60/90 seconds after revival | One visible/audible shock at each interval; affected player's camera shakes/turns and may tumble; no direct HP subtraction (isolate from environmental impacts) | Unrun |
| D07 | Set YawDegrees=180 and TumbleChance=0; observe a shock | Strong shake and approximately 180-degree horizontal turn on target only; input remains usable | Unrun |
| D08 | Set TumbleChance=1; observe with host/client roles swapped | Native Q posture occurs once and is seen by all; recover with normal controls; no voluntary launch-upgrade impulse | Unrun |
| D09 | Set YawDegrees=0 and TumbleChance=0 | No forced turn or tumble; existing visuals and configured native shake remain | Unrun |
| D10 | Shock while target is already tumbling | Does not unfold target or restart native tumble | Unrun |
| D11 | Open menu, die or leave level during camera turn | Jolt stops without a delayed residual turn on recovery | Unrun |
| D04 | Observe D03 as another player | Electrical arcs visible on target; observer's camera unaffected | Unrun |
| D05 | Die again or end the level during either effect | Both effects stop; a later revival rolls a new pitch | Unrun |
| D06 | Late join while voice effect is active | Same pitch within the next snapshot; original expiry preserved, no replay of past shocks | Unrun |
| A01 | Remain alive after revival without pressing push-to-talk | First animal at 30 seconds; each new interval picks one of four, repeats allowed | Unrun |
| A02 | Speak via microphone/TTS during the animal | Only animal heard during the clip; real voice resumes immediately afterwards; no stuck mute | Unrun |
| A03 | Inspect speech animation with microphone disabled | Target visibly appears to speak according to the animal waveform | Unrun |
| A04 | Compare the same animal with high/low assigned player voice | Same natural animal pitch/speed and duration; source pitch stays 1, no Doppler | Unrun |
| A05 | Listen as host/client from different distances and with player-volume changes | Same animal chosen, positional playback and voice gain respected | Unrun |
| A06 | Die, leave level or disconnect mid-clip; late join afterwards | Playback/overrides clear, no old clip replay; next deadline preserved | Unrun |
| A07 | Set electrical interval to 10 seconds; leave animal interval at 30 | Animal remains every 30 seconds, not every shock | Unrun |
| A08 | Survive beyond comic pitch expiry | Voice pitch returns to normal; scheduled animal interruptions continue until death/level end | Unrun |
| N01 | Operator disconnects mid-attempt | No delayed revival; another operator can start | Unrun |
| N02 | Host changes if the game supports continuation | Progress resets; charge preserved; no duplicate revival | Unrun |
| C01 | Change host settings and restart | Host price/duration/range/health/cost govern results | Unrun |
| M01 | Repeat shop, revive, damage, two-use and charger tests with the full supplied profile | No errors or feature interference | Unrun |
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

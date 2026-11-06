# Troubleshooting shop identification and missing items

## Another revival item appears in the shop

The mod registers its item with these exact identifiers:

- Display name: `Industrial Defibrillator`.
- Definition/prefab name: `Item Industrial Defibrillator`.
- BepInEx plugin ID: `ciriolopipado.defibrillator`.
- Default shop price: `2k` (`Price=2` in the config, as of 0.1.2).
- Runtime donor: a native tracker with `ItemTracker`, not the native revival item.

The custom definition clears `itemNameLocalized`, so its display name falls back to the English `itemName`.
The inspected game assemblies also contain an independent native revival component, `ItemReviveItem`.
A different displayed name, such as the reported Spanish label `Desfibró`, does not establish that our item was renamed or replaced. The exact localized identity of that object has not been verified from a screenshot or game assets.
The native revival item is left in the game's normal shop pool and can appear alongside the custom item.

## Confirmed v0.1.0 registration failure

The user supplied this runtime error after testing:

```text
Item registration failed; no replacement of vanilla items was attempted.
System.InvalidOperationException: Sequence contains no matching element
at System.Linq.Enumerable.First(...)
at Defibrillator.Mod.ItemFactory.EnsureRegistered()
```

This confirms plugin execution reached donor discovery and the old combined predicate found no match. It does not prove whether the asset-name filter, root components, resource location or embedded display requirement was the excluding condition.
Version 0.1.1 removes those unnecessary name/location/display assumptions while preserving required shell components. Display-free donors wait for a safe native display template and get a private hidden copy for native battery bookkeeping.

New log messages include:

- `Donor discovery pending: No compatible tracker prefab is available yet.` followed by the resource folder, definition count and component report.
- `Donor discovery pending: Compatible tracker found:` followed by display-template readiness details.
- `Native donor selected:` followed by the definition and detected components.
- `Attached a private native battery-display fallback; the shared HUD is unchanged.` when the fallback is needed.

Repeated identical discovery reports are suppressed. Fatal errors during actual prefab construction or network registration are still logged and not retried blindly, to avoid duplicate registrations.

## Upgrading size, weight and balance in 0.1.2

The user reported that the spawned device was too large and had unrealistic mass/collision. The old visual size depended on donor mesh bounds and retained donor collision geometry. Native startup also reapplies mass from the donor physics preset.
The new version sets a fixed compact model scale, replaces collision with six fitted grabbable boxes, updates the cloned mass preset and normalizes any restored scale before native bounds initialization.

Legacy configuration defaults migrate once: `Price=0` becomes `2`, and `CostPercent=25` becomes `50`. Other values are preserved. The log contains `Balance defaults migrated:` on this migration.
If you intentionally want different values after migration, edit the config with the game closed. Do not enter `Price=2000` for 2k: the correct value is `2`.
All players must replace both DLLs with the same version. Physical geometry/mass are fixed rather than independently configurable on each client.

## Determine which stage failed

Read `BepInEx/LogOutput.log` from the actual Thunderstore profile used by the **host**.
The presence of a different revival item is insufficient to diagnose plugin loading or shop injection.

| Log evidence | Meaning / next investigation |
| --- | --- |
| No mention of Industrial Defibrillator or its plugin ID | Check installation, selected profile, loader discovery errors and dependency errors. Do not assume a specific cause yet. |
| `Defibrillator initialization failed; features disabled.` | Inspect the complete following exception; configuration, dependency or API initialization failed. |
| `Industrial Defibrillator 0.1.2 loaded.` | Plugin initialization completed; this alone does not confirm item registration or spawning. |
| `Registered Industrial Defibrillator using native shell:` | REPOLib accepted the custom prefab/definition. |
| `Item registration failed; no replacement of vanilla items was attempted.` | Inspect the following exception to identify donor discovery, prefab construction or REPOLib registration failure. |
| `Reserved one defibrillator shop slot at price 2.` | The host's native shop-spawn method accepted the custom item or queued its spawn. This alone does not prove the instantiated object completed startup. |
| `No shop slot available for the guaranteed defibrillator.` | No usable normal item volume was available when the injection ran. |
| `Defibrillator shop injection failed.` | Inspect the complete following exception. |
| `Defibrillator stopped after an unexpected runtime error.` | Runtime processing stopped; inspect the first exception before investigating subsequent symptoms. |

The generated configuration file `BepInEx/config/ciriolopipado.defibrillator.cfg` is supporting evidence that the plugin entered initialization, not proof that initialization succeeded.
Confirm both `Defibrillator.dll` and `Defibrillator.Core.dll` are present in the tested profile's plugin directory.

## Evidence needed for this report

Provide the host's complete log from a run that entered the affected shop, the tested ZIP/version, and a screenshot or description of the other item's shape and price.
Client logs are useful if registration succeeds on the host but only clients fail to see the object.
Remove private information before sharing logs. Do not publicly upload game DLLs or saved profile credentials.

The supplied log establishes donor lookup failure in v0.1.0. Successful physical behavior in v0.1.2 requires the new in-game test log and observation; the prior registration failure is recorded separately.

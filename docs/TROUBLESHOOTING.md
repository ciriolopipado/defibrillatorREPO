# Troubleshooting shop identification and missing items

## Another revival item appears in the shop

The mod registers its item with these exact identifiers:

- Display name: `Industrial Defibrillator`.
- Definition/prefab name: `Item Industrial Defibrillator`.
- BepInEx plugin ID: `ciriolopipado.defibrillator`.
- Default shop price: `0`.
- Runtime donor: a native tracker with `ItemTracker`, not the native revival item.

The custom definition clears `itemNameLocalized`, so its display name falls back to the English `itemName`.
The inspected game assemblies also contain an independent native revival component, `ItemReviveItem`.
A different displayed name, such as the reported Spanish label `Desfibró`, does not establish that our item was renamed or replaced. The exact localized identity of that object has not been verified from a screenshot or game assets.
The native revival item is left in the game's normal shop pool and can appear alongside the custom item.

## Determine which stage failed

Read `BepInEx/LogOutput.log` from the actual Thunderstore profile used by the **host**.
The presence of a different revival item is insufficient to diagnose plugin loading or shop injection.

| Log evidence | Meaning / next investigation |
| --- | --- |
| No mention of Industrial Defibrillator or its plugin ID | Check installation, selected profile, loader discovery errors and dependency errors. Do not assume a specific cause yet. |
| `Defibrillator initialization failed; features disabled.` | Inspect the complete following exception; configuration, dependency or API initialization failed. |
| `Industrial Defibrillator 0.1.0 loaded.` | Plugin initialization completed; this alone does not confirm item registration or spawning. |
| `Registered Industrial Defibrillator using native shell:` | REPOLib accepted the custom prefab/definition. |
| `Item registration failed; no replacement of vanilla items was attempted.` | Inspect the following exception to identify donor discovery, prefab construction or REPOLib registration failure. |
| `Reserved one defibrillator shop slot at price 0.` | The host's native shop-spawn method accepted the custom item or queued its spawn. This alone does not prove the instantiated object completed startup. |
| `No shop slot available for the guaranteed defibrillator.` | No usable normal item volume was available when the injection ran. |
| `Defibrillator shop injection failed.` | Inspect the complete following exception. |
| `Defibrillator stopped after an unexpected runtime error.` | Runtime processing stopped; inspect the first exception before investigating subsequent symptoms. |

The generated configuration file `BepInEx/config/ciriolopipado.defibrillator.cfg` is supporting evidence that the plugin entered initialization, not proof that initialization succeeded.
Confirm both `Defibrillator.dll` and `Defibrillator.Core.dll` are present in the tested profile's plugin directory.

## Evidence needed for this report

Provide the host's complete log from a run that entered the affected shop, the tested ZIP/version, and a screenshot or description of the other item's shape and price.
Client logs are useful if registration succeeds on the host but only clients fail to see the object.
Remove private information before sharing logs. Do not publicly upload game DLLs or saved profile credentials.

Source inspection confirms separate item identities. It does not establish the runtime cause of the reported missing item; that remains pending the actual test log.

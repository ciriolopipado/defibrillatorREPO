# Validation record — 0.1.0

## Completed locally

- SDK: .NET 8.0.427, Linux x64.
- Target framework: netstandard2.1 for plugin/core; net8.0 for test runners.
- Plugin: Release compilation against user-supplied Managed assemblies, zero warnings and zero errors.
- Core behavior runner: **40 executed, 40 passed, 0 failed**.
- Game API metadata runner: **26 executed, 26 passed, 0 failed**.
- Tests include one-HP revival, four battery uses, cancellation, concurrency, repeated death identities, voice expiry, lifetime boundaries, shock intervals and no catch-up bursts.
- API checks validate every Harmony target's method arity, required private fields, native pitch/camera methods and the reflective shop method's ref-int parameter.
- Game-reference fingerprints are recorded separately; no reference DLLs are distributed.
- Package input is an explicit allowlist of original DLLs, metadata, icon and documentation.

## Not established

No R.E.P.O. executable was run. Automated tests do not establish actual RPC delivery, microphone/audio behavior, camera presentation, shader appearance, save compatibility, shop interaction or charger behavior.
The complete supplied mod profile, late joining, host migration and actual multiplayer revival remain untested.
An optional core CI workflow template is included under `docs/ci`; it is not active CI. The server rejected a push containing a `.github/workflows` change, so the workflow was moved to documentation before retrying. No remote CI run has been observed.

Use the acceptance checklist to record game-test outcomes before labeling any release stable.

## Reference provenance

- Game references: private `Managed.rar` supplied by the user for the reported game version 0.5.1.
- BepInEx compile references: official BepInEx 5.4.23.5 Windows x64 release, corresponding to the requested pack generation.
- REPOLib compile reference: published Zehs.REPOLib 4.3.0 NuGet package. Signature verification ran; online certificate-revocation endpoints were unreachable and produced warnings. Verification was not disabled.
- Public REPOLib source inspected at commit `76128a80ec0dcbe901c823356fd28c78ca30ad8e`.
- Profile export: package names/versions only; private configuration contents are not included.

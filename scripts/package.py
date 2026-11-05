"""Build an explicit allowlist-only Thunderstore archive of original project files."""
import hashlib
import json
import pathlib
import zipfile

ROOT = pathlib.Path(__file__).resolve().parents[1]
manifest = json.loads((ROOT / "thunderstore/manifest.json").read_text())
version = manifest["version_number"]
files = {
    "manifest.json": ROOT / "thunderstore/manifest.json",
    "README.md": ROOT / "README.md",
    "CHANGELOG.md": ROOT / "CHANGELOG.md",
    "icon.png": ROOT / "thunderstore/icon.png",
    "thunderstore/cover.png": ROOT / "thunderstore/cover.png",
    "thunderstore/ARTWORK.md": ROOT / "thunderstore/ARTWORK.md",
    "docs/TESTING.md": ROOT / "docs/TESTING.md",
    "docs/TROUBLESHOOTING.md": ROOT / "docs/TROUBLESHOOTING.md",
    "docs/DEVELOPMENT.md": ROOT / "docs/DEVELOPMENT.md",
    "docs/MOD_PROFILE.md": ROOT / "docs/MOD_PROFILE.md",
    "docs/VALIDATION.md": ROOT / "docs/VALIDATION.md",
    "BepInEx/plugins/IndustrialDefibrillator/Defibrillator.dll": ROOT / "src/Defibrillator.Plugin/bin/Release/netstandard2.1/Defibrillator.dll",
    "BepInEx/plugins/IndustrialDefibrillator/Defibrillator.Core.dll": ROOT / "src/Defibrillator.Plugin/bin/Release/netstandard2.1/Defibrillator.Core.dll",
}
for name, source in files.items():
    if not source.is_file():
        raise SystemExit(f"Missing package input: {source}. Build the plugin first.")
out = ROOT / "releases" / f"Industrial_Defibrillator-{version}.zip"
out.parent.mkdir(exist_ok=True)
with zipfile.ZipFile(out, "w", compression=zipfile.ZIP_DEFLATED) as archive:
    for name, source in sorted(files.items()):
        info = zipfile.ZipInfo(name, (2026, 1, 1, 0, 0, 0))
        info.compress_type = zipfile.ZIP_DEFLATED
        info.external_attr = 0o100644 << 16
        archive.writestr(info, source.read_bytes())
with zipfile.ZipFile(out) as archive:
    assert archive.testzip() is None, "Corrupt ZIP output"
    assert set(archive.namelist()) == set(files), "Unexpected package contents"
digest = hashlib.sha256(out.read_bytes()).hexdigest()
out.with_suffix(".zip.sha256").write_text(f"{digest}  {out.name}\n")
print(f"Created {out} ({out.stat().st_size} bytes)")
print(f"SHA256 {digest}")

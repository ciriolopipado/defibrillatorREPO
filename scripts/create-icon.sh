#!/usr/bin/env bash
# Resize the approved cover art for Thunderstore without altering the source image.
set -euo pipefail
cd "$(dirname "$0")/.."
magick thunderstore/cover.png -resize 256x256 -strip PNG24:thunderstore/icon.png
magick identify thunderstore/icon.png

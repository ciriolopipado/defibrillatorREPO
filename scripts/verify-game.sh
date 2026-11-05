#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
bash scripts/verify.sh
dotnet build src/Defibrillator.Plugin/Defibrillator.Plugin.csproj --configuration Release
dotnet run --project tests/Defibrillator.Api.Tests/Defibrillator.Api.Tests.csproj --configuration Release -- local-libs

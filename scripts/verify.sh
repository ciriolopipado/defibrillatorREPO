#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
dotnet build src/Defibrillator.Core/Defibrillator.Core.csproj --configuration Release
dotnet run --project tests/Defibrillator.Core.Tests/Defibrillator.Core.Tests.csproj --configuration Release

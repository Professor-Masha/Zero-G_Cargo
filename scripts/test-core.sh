#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-${TMPDIR:-/tmp}/zero-g-dotnet}"
dotnet_bin="${DOTNET:-dotnet}"
exec "$dotnet_bin" run --project Tests/Core/ZeroGCargo.Core.Tests.csproj --configuration Release

#!/bin/zsh
set -euo pipefail

project_root="$(cd "$(dirname "$0")/.." && pwd)"
unity_editor="${UNITY_EDITOR:-/Applications/Unity/Hub/Editor/2022.3.44f1/Unity.app/Contents/MacOS/Unity}"
smoke_seconds="${TCC_SMOKE_SECONDS:-360}"
temp_root="$(mktemp -d /tmp/tcc-baseline.XXXXXX)"
temp_project="$temp_root/project"
player_path="$temp_root/The Call of the Cave.app"
build_log="$temp_root/unity-build.log"
smoke_log="$temp_root/smoke-player.log"

cleanup() {
  exit_code=$?
  if [[ $exit_code -ne 0 ]]; then
    print -u2 "Baseline smoke failed. Recent build output:"
    [[ -f "$build_log" ]] && tail -n 80 "$build_log" >&2
    print -u2 "Recent player output:"
    [[ -f "$smoke_log" ]] && tail -n 80 "$smoke_log" >&2
  fi
  rm -rf "$temp_root"
}
trap cleanup EXIT

if [[ ! -x "$unity_editor" ]]; then
  print -u2 "Unity editor not found: $unity_editor"
  exit 1
fi

mkdir -p "$temp_project"
rsync -a \
  --exclude '.git' \
  --exclude 'Library' \
  --exclude 'Temp' \
  --exclude 'Logs' \
  --exclude 'Builds' \
  "$project_root/" "$temp_project/"

"$unity_editor" \
  -batchmode \
  -nographics \
  -quit \
  -projectPath "$temp_project" \
  -executeMethod TCC.EditorTools.BaselineValidation.BuildBatch \
  -tccBaselineOutput "$player_path" \
  -logFile "$build_log"

player_binary="$player_path/Contents/MacOS/The Call of the Cave"
"$player_binary" \
  -batchmode \
  -nographics \
  -tccBaselineSmoke \
  -tccSmokeSeconds "$smoke_seconds" \
  -logFile "$smoke_log"

grep -F "[TCC Baseline] PASS:" "$smoke_log"

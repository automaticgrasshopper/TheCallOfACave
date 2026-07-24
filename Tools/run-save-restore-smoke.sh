#!/bin/zsh
set -euo pipefail

project_root="$(cd "$(dirname "$0")/.." && pwd)"
unity_editor="${UNITY_EDITOR:-/Applications/Unity/Hub/Editor/2022.3.44f1/Unity.app/Contents/MacOS/Unity}"
temp_root="$(mktemp -d /tmp/tcc-save-restore.XXXXXX)"
temp_project="$temp_root/project"
player_path="$temp_root/The Call of the Cave.app"
save_root="$temp_root/saved-profiles"
build_log="$temp_root/unity-build.log"
write_log="$temp_root/write-player.log"
read_log="$temp_root/read-player.log"
hub_licensing_ipc="$(
  ps -ax -o command= |
    sed -n 's|.*UnityLicensingClient_V1.*--namedPipe Unity-\([^ ]*\).*|\1|p' |
    head -n 1
)"

cleanup() {
  exit_code=$?
  if [[ $exit_code -ne 0 ]]; then
    print -u2 "Save/restore smoke failed. Relevant output:"
    for log in "$build_log" "$write_log" "$read_log"; do
      [[ -f "$log" ]] &&
        grep -E '\[TCC SaveRestore\]|\[TCC Save\]|error CS|Exception|Error' "$log" |
        tail -n 120 >&2 || true
    done
  fi
  rm -rf "$temp_root"
}
trap cleanup EXIT

if [[ ! -x "$unity_editor" ]]; then
  print -u2 "Unity editor not found: $unity_editor"
  exit 1
fi

if [[ -z "$hub_licensing_ipc" ]]; then
  print -u2 "Unity Hub licensing service is not running. Open Unity Hub and sign in first."
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
  -licensingIpc "$hub_licensing_ipc" \
  -projectPath "$temp_project" \
  -executeMethod TCC.EditorTools.BaselineValidation.BuildBatch \
  -tccBaselineOutput "$player_path" \
  -logFile "$build_log"

player_binary="$player_path/Contents/MacOS/The Call of the Cave"
"$player_binary" \
  -batchmode \
  -nographics \
  -tccSaveRestoreSmoke \
  -tccSaveRestoreMode write \
  -tccSaveRoot "$save_root" \
  -logFile "$write_log"

"$player_binary" \
  -batchmode \
  -nographics \
  -tccSaveRestoreSmoke \
  -tccSaveRestoreMode read \
  -tccSaveRoot "$save_root" \
  -logFile "$read_log"

grep -F "[TCC SaveRestore] WRITE PASS:" "$write_log"
grep -F "[TCC SaveRestore] READ PASS:" "$read_log"

#!/bin/zsh
set -euo pipefail

project_root="$(cd "$(dirname "$0")/.." && pwd)"
unity_editor="${UNITY_EDITOR:-/Applications/Unity/Hub/Editor/2022.3.44f1/Unity.app/Contents/MacOS/Unity}"
temp_root="$(mktemp -d /tmp/tcc-profile-repository.XXXXXX)"
temp_project="$temp_root/project"
test_log="$temp_root/profile-repository-tests.log"
hub_licensing_ipc="$(
  ps -ax -o command= |
    sed -n 's|.*UnityLicensingClient_V1.*--namedPipe Unity-\([^ ]*\).*|\1|p' |
    head -n 1
)"

cleanup() {
  exit_code=$?
  if [[ $exit_code -ne 0 && -f "$test_log" ]]; then
    print -u2 "Profile repository tests failed. Relevant output:"
    grep -E '\[TCC Repository\]|error CS|Exception|Error' "$test_log" | tail -n 120 >&2 || true
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
  -executeMethod TCC.EditorTools.ProfileRepositoryValidation.RunBatch \
  -logFile "$test_log"

grep -F "[TCC Repository] PASS:" "$test_log"

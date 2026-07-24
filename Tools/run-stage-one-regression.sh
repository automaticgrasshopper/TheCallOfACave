#!/bin/zsh
set -euo pipefail

project_root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$project_root"

Tools/run-profile-domain-tests.sh
Tools/run-profile-repository-tests.sh
Tools/run-world-snapshot-tests.sh
Tools/run-profile-migration-tests.sh
Tools/run-save-restore-smoke.sh
Tools/run-baseline-smoke.sh

print "[TCC Stage 1] PASS: days 1-6 regression suite."

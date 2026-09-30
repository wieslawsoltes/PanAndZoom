#!/usr/bin/env bash
# Builds and runs the Uno Platform runtime tests (tests/PanAndZoom.Uno.RuntimeTests) headless.
#
# Usage: build/run-uno-runtime-tests.sh [Configuration] [Filter]
#   Configuration  Build configuration (default: Release)
#   Filter         Optional runtime-test filter (test class or method name fragment)
#
# On Linux without a display the tests run under xvfb-run (X11 host).
# The script exits with a non-zero code when any test fails.
set -euo pipefail

CONFIGURATION="${1:-Release}"
FILTER="${2:-}"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT="$ROOT/tests/PanAndZoom.Uno.RuntimeTests/PanAndZoom.Uno.RuntimeTests.csproj"
RESULTS_DIR="$ROOT/artifacts/test-results"
RESULTS="$RESULTS_DIR/uno-runtime-tests.xml"

mkdir -p "$RESULTS_DIR"
rm -f "$RESULTS"

dotnet build "$PROJECT" -c "$CONFIGURATION" -f net10.0-desktop

export UNO_RUNTIME_TESTS_RUN_TESTS="${FILTER:-true}"
export UNO_RUNTIME_TESTS_OUTPUT_PATH="$RESULTS"

RUN=(dotnet run --project "$PROJECT" -c "$CONFIGURATION" -f net10.0-desktop --no-build)

if [[ "$(uname -s)" == "Linux" && -z "${DISPLAY:-}" ]]; then
  xvfb-run --auto-servernum --server-args="-screen 0 1920x1080x24" "${RUN[@]}"
else
  "${RUN[@]}"
fi

if [[ ! -f "$RESULTS" ]]; then
  echo "Runtime tests did not produce a result file at $RESULTS" >&2
  exit 1
fi

python3 - "$RESULTS" <<'PY'
import sys
import xml.etree.ElementTree as ET

root = ET.parse(sys.argv[1]).getroot()
total = int(root.get("total", 0))
passed = int(root.get("passed", 0))
failed = int(root.get("failed", 0))
skipped = int(root.get("skipped", 0))

for case in root.iter("test-case"):
    if case.get("result") == "Failed":
        message = case.find("./failure/message")
        print(f"FAILED: {case.get('fullname')}\n  {message.text.strip() if message is not None and message.text else ''}")

print(f"Uno runtime tests: total={total} passed={passed} failed={failed} skipped={skipped}")
sys.exit(1 if failed > 0 or total == 0 else 0)
PY

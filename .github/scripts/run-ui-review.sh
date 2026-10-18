#!/usr/bin/env bash
# Run within the emulator action; each action script line is otherwise a new shell.
set -u
cd "${GITHUB_WORKSPACE:-$(pwd)}/android" || exit 1
chmod +x ./gradlew
./gradlew connectedDebugAndroidTest --no-daemon 2>&1 | tee "${RUNNER_TEMP:-/tmp}/ui-review.log"
BUDU_TEST_STATUS=${PIPESTATUS[0]}
adb pull /sdcard/Pictures/BuduPingUI ../ui-screenshots || true
if [ "$BUDU_TEST_STATUS" -eq 0 ] && [ "$(find ../ui-screenshots -name '*.png' 2>/dev/null | wc -l)" -lt 5 ]; then
  echo 'Native tests passed but screenshots were not collected.'
  exit 1
fi
exit "$BUDU_TEST_STATUS"

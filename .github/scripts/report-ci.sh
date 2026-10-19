#!/usr/bin/env bash
# report-ci.sh <name> <status> [logfile]
# Writes reports/<name>.md to the `ci-reports` branch so the result (and the
# build log tail on failure) is readable over plain git. Never fails the job.
set -u
NAME="${1:-unknown}"
STATUS="${2:-unknown}"
LOGFILE="${3:-}"
WS="${GITHUB_WORKSPACE:-$(pwd)}"
TMPD="$(mktemp -d)"
OUT="$TMPD/report.md"

{
  echo "# CI report: $NAME"
  echo "- status: $STATUS"
  echo "- workflow: ${GITHUB_WORKFLOW:-}"
  echo "- run: ${GITHUB_RUN_ID:-}/${GITHUB_RUN_NUMBER:-}"
  echo "- sha: ${GITHUB_SHA:-}"
  echo "- time: $(date -u +%FT%TZ)"
  if [ "$STATUS" != "success" ] && [ -n "$LOGFILE" ] && [ -f "$LOGFILE" ]; then
    echo
    echo "## build log (tail 300)"
    echo '```'
    tail -n 300 "$LOGFILE" | sed 's/```/`` `/g'
    echo '```'
  fi
} > "$OUT"

cd "$WS" || exit 0
git config user.name "github-actions[bot]" || true
git config user.email "github-actions[bot]@users.noreply.github.com" || true

for attempt in 1 2 3 4 5; do
  git fetch origin ci-reports 2>/dev/null || true
  rm -rf "$WS/.ci-wt"
  if git show-ref --verify --quiet refs/remotes/origin/ci-reports; then
    git worktree add --detach "$WS/.ci-wt" origin/ci-reports >/dev/null 2>&1
  else
    git worktree add --orphan -b ci-reports "$WS/.ci-wt" >/dev/null 2>&1
  fi
  if [ ! -d "$WS/.ci-wt" ]; then
    sleep $((attempt * 3))
    continue
  fi
  mkdir -p "$WS/.ci-wt/reports"
  cp "$OUT" "$WS/.ci-wt/reports/$NAME.md"
  # Optional: UI-review screenshots (CI_UI_DIR) ride along so they can be
  # eyeballed over plain git, not only as Actions artifacts.
  if [ -n "${CI_UI_DIR:-}" ] && [ -d "$CI_UI_DIR" ]; then
    mkdir -p "$WS/.ci-wt/reports/ui-$NAME"
    for f in "$CI_UI_DIR"/*.png "$CI_UI_DIR"/*.json "$CI_UI_DIR"/*.md; do
      [ -f "$f" ] && cp "$f" "$WS/.ci-wt/reports/ui-$NAME/" 2>/dev/null || true
    done
  fi
  cd "$WS/.ci-wt" || break
  git add "reports/$NAME.md" || true
  [ -d "reports/ui-$NAME" ] && git add "reports/ui-$NAME" || true
  if git commit -m "ci-report: $NAME $STATUS run ${GITHUB_RUN_NUMBER:-?}" >/dev/null 2>&1; then
    if git push origin HEAD:ci-reports >/dev/null 2>&1; then
      cd "$WS" || true
      git worktree remove --force "$WS/.ci-wt" >/dev/null 2>&1 || true
      exit 0
    fi
  else
    # nothing to commit (report unchanged) counts as delivered
    cd "$WS" || true
    git worktree remove --force "$WS/.ci-wt" >/dev/null 2>&1 || true
    exit 0
  fi
  cd "$WS" || true
  git worktree remove --force "$WS/.ci-wt" >/dev/null 2>&1 || true
  sleep $((attempt * 3))
done
exit 0

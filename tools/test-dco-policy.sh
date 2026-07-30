#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
VERIFY_SCRIPT="$SCRIPT_DIR/verify-dco.sh"
TEMP_ROOT="$(mktemp -d)"
trap 'rm -rf "$TEMP_ROOT"' EXIT

REPO="$TEMP_ROOT/repo"
ALLOWLIST="$TEMP_ROOT/allowlist.json"
mkdir -p "$REPO"

cat > "$ALLOWLIST" <<'JSON'
{
  "schemaVersion": 1,
  "bots": [
    {
      "login": "trusted-maintenance-bot[bot]",
      "id": 1001,
      "type": "Bot",
      "authorEmails": [
        "1001+trusted-maintenance-bot[bot]@users.noreply.github.com"
      ],
      "committerEmails": [
        "1001+trusted-maintenance-bot[bot]@users.noreply.github.com"
      ]
    }
  ]
}
JSON

cd "$REPO"
git init -q
git config user.name "DCO Test Maintainer"
git config user.email "maintainer@example.test"
git commit --allow-empty -q -m "base"
BASE="$(git rev-parse HEAD)"

run_verify() {
  local head="$1"
  local login="$2"
  local id="$3"
  local type="$4"

  BASE_SHA="$BASE" \
  HEAD_SHA="$head" \
  PR_ACTOR_LOGIN="$login" \
  PR_ACTOR_ID="$id" \
  PR_ACTOR_TYPE="$type" \
  DCO_BOT_ALLOWLIST_PATH="$ALLOWLIST" \
    "$VERIFY_SCRIPT"
}

reset_repo() {
  git reset --hard -q "$BASE"
}

# A normal human commit with an identity-matching sign-off must pass.
git commit --allow-empty -q --signoff -m "signed human commit"
run_verify "$(git rev-parse HEAD)" "human-contributor" "2001" "User"
reset_repo

# A human commit without a sign-off must fail.
git commit --allow-empty -q -m "unsigned human commit"
if run_verify "$(git rev-parse HEAD)" "human-contributor" "2001" "User"; then
  echo "Unsigned human commit unexpectedly passed DCO verification."
  exit 1
fi
reset_repo

# A valid-looking trailer for a different identity must fail.
git commit --allow-empty -q -m $'mismatched sign-off\n\nSigned-off-by: Another Person <other@example.test>'
if run_verify "$(git rev-parse HEAD)" "human-contributor" "2001" "User"; then
  echo "Mismatched Signed-off-by identity unexpectedly passed DCO verification."
  exit 1
fi
reset_repo

# Spoofing [bot] in commit metadata must fail when the PR actor is a human.
GIT_AUTHOR_NAME="trusted-maintenance-bot[bot]" \
GIT_AUTHOR_EMAIL="1001+trusted-maintenance-bot[bot]@users.noreply.github.com" \
GIT_COMMITTER_NAME="trusted-maintenance-bot[bot]" \
GIT_COMMITTER_EMAIL="1001+trusted-maintenance-bot[bot]@users.noreply.github.com" \
  git commit --allow-empty -q -m "spoofed bot metadata"
if run_verify "$(git rev-parse HEAD)" "human-contributor" "2001" "User"; then
  echo "Spoofed bot metadata unexpectedly passed DCO verification."
  exit 1
fi
reset_repo

# An allowlisted GitHub bot identity with exact actor ID/type and emails may pass.
GIT_AUTHOR_NAME="trusted-maintenance-bot[bot]" \
GIT_AUTHOR_EMAIL="1001+trusted-maintenance-bot[bot]@users.noreply.github.com" \
GIT_COMMITTER_NAME="trusted-maintenance-bot[bot]" \
GIT_COMMITTER_EMAIL="1001+trusted-maintenance-bot[bot]@users.noreply.github.com" \
  git commit --allow-empty -q -m "trusted bot commit"
run_verify "$(git rev-parse HEAD)" "trusted-maintenance-bot[bot]" "1001" "Bot"

# The same metadata with a different GitHub actor ID must fail.
if run_verify "$(git rev-parse HEAD)" "trusted-maintenance-bot[bot]" "9999" "Bot"; then
  echo "Bot with an untrusted GitHub actor ID unexpectedly passed DCO verification."
  exit 1
fi

echo "DCO policy tests passed."

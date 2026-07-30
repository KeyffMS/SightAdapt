#!/usr/bin/env bash
set -euo pipefail

: "${BASE_SHA:?BASE_SHA is required}"
: "${HEAD_SHA:?HEAD_SHA is required}"

ROOT="$(git rev-parse --show-toplevel)"
ALLOWLIST_PATH="${DCO_BOT_ALLOWLIST_PATH:-$ROOT/.github/dco-bot-allowlist.json}"
PR_ACTOR_LOGIN="${PR_ACTOR_LOGIN:-}"
PR_ACTOR_ID="${PR_ACTOR_ID:-}"
PR_ACTOR_TYPE="${PR_ACTOR_TYPE:-}"

command -v git >/dev/null 2>&1 || {
  echo "::error::git is required for DCO verification."
  exit 1
}
command -v jq >/dev/null 2>&1 || {
  echo "::error::jq is required for trusted bot identity verification."
  exit 1
}

if [[ ! -f "$ALLOWLIST_PATH" ]]; then
  echo "::error::Trusted bot allowlist does not exist: $ALLOWLIST_PATH"
  exit 1
fi

if ! jq -e '
  .schemaVersion == 1 and
  (.bots | type == "array") and
  all(.bots[];
    (.login | type == "string" and length > 0) and
    (.id | type == "number" and . > 0) and
    (.type == "Bot" or .type == "App") and
    (.authorEmails | type == "array" and length > 0) and
    (.committerEmails | type == "array" and length > 0) and
    all(.authorEmails[]; type == "string" and length > 0) and
    all(.committerEmails[]; type == "string" and length > 0)
  ) and
  (([.bots[].login] | length) == ([.bots[].login] | unique | length)) and
  (([.bots[].id] | length) == ([.bots[].id] | unique | length))
' "$ALLOWLIST_PATH" >/dev/null; then
  echo "::error::Trusted bot allowlist has an invalid or duplicate identity record."
  exit 1
fi

if ! git cat-file -e "${BASE_SHA}^{commit}" 2>/dev/null; then
  echo "::error::BASE_SHA does not identify an available commit: $BASE_SHA"
  exit 1
fi
if ! git cat-file -e "${HEAD_SHA}^{commit}" 2>/dev/null; then
  echo "::error::HEAD_SHA does not identify an available commit: $HEAD_SHA"
  exit 1
fi

is_trusted_bot_commit() {
  local commit="$1"
  local author_email committer_email matches

  if [[ -z "$PR_ACTOR_LOGIN" || -z "$PR_ACTOR_ID" || -z "$PR_ACTOR_TYPE" ]]; then
    return 1
  fi

  author_email="$(git show -s --format='%ae' "$commit")"
  committer_email="$(git show -s --format='%ce' "$commit")"

  matches="$(jq -r \
    --arg login "$PR_ACTOR_LOGIN" \
    --arg id "$PR_ACTOR_ID" \
    --arg type "$PR_ACTOR_TYPE" \
    --arg authorEmail "$author_email" \
    --arg committerEmail "$committer_email" '
      [
        .bots[] |
        select(
          .login == $login and
          (.id | tostring) == $id and
          .type == $type and
          (((.authorEmails // []) | index($authorEmail)) != null) and
          (((.committerEmails // []) | index($committerEmail)) != null)
        )
      ] | length
    ' "$ALLOWLIST_PATH")"

  [[ "$matches" == "1" ]]
}

has_identity_signoff() {
  local commit="$1"
  local author_name author_email committer_name committer_email
  local expected_author expected_committer line key value

  author_name="$(git show -s --format='%an' "$commit")"
  author_email="$(git show -s --format='%ae' "$commit")"
  committer_name="$(git show -s --format='%cn' "$commit")"
  committer_email="$(git show -s --format='%ce' "$commit")"
  expected_author="$author_name <$author_email>"
  expected_committer="$committer_name <$committer_email>"

  while IFS= read -r line; do
    [[ -n "$line" ]] || continue
    key="${line%%:*}"
    value="${line#*:}"
    value="${value# }"
    if [[ "${key,,}" == "signed-off-by" ]] &&
       [[ "$value" == "$expected_author" || "$value" == "$expected_committer" ]]; then
      return 0
    fi
  done < <(git show -s --format='%B' "$commit" | git interpret-trailers --parse)

  return 1
}

mapfile -t commits < <(git rev-list --reverse "${BASE_SHA}..${HEAD_SHA}")
if [[ "${#commits[@]}" -eq 0 ]]; then
  echo "::error::No pull-request commits were found between BASE_SHA and HEAD_SHA."
  exit 1
fi

checked=0
trusted_bot_exceptions=0
missing=0

for commit in "${commits[@]}"; do
  author_name="$(git show -s --format='%an' "$commit")"
  author_email="$(git show -s --format='%ae' "$commit")"

  if has_identity_signoff "$commit"; then
    checked=$((checked + 1))
    echo "DCO sign-off verified: $commit $author_name <$author_email>"
    continue
  fi

  if is_trusted_bot_commit "$commit"; then
    trusted_bot_exceptions=$((trusted_bot_exceptions + 1))
    echo "Trusted bot exception verified: $commit $PR_ACTOR_LOGIN ($PR_ACTOR_ID, $PR_ACTOR_TYPE)"
    continue
  fi

  echo "::error::Commit $commit by $author_name <$author_email> lacks a Signed-off-by trailer matching its author or committer identity."
  missing=1
done

if [[ "$missing" -ne 0 ]]; then
  echo "Add or repair the trailer with: git commit --amend --signoff"
  exit 1
fi

echo "DCO verified for $checked signed commit(s) and $trusted_bot_exceptions trusted bot exception(s)."

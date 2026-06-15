#!/usr/bin/env bash
#
# bootstrap-github.sh — create the StreamForge GitHub repo and push this project.
#
# This script authenticates with YOUR local credentials via the GitHub CLI.
# No token is passed on the command line or stored anywhere by this script.
#
# Prerequisites:
#   - git            https://git-scm.com/
#   - gh (GitHub CLI) https://cli.github.com/
#
# Usage:
#   1. Authenticate once (opens a browser, stores the credential locally):
#        gh auth login
#   2. From the project root (the folder containing StreamForge.sln), run:
#        ./bootstrap-github.sh [repo-name] [public|private]
#
#   Defaults: repo-name = streamforge, visibility = private.

set -euo pipefail

REPO_NAME="${1:-streamforge}"
VISIBILITY="${2:-private}"

if ! command -v git >/dev/null 2>&1; then
  echo "error: git is not installed." >&2; exit 1
fi
if ! command -v gh >/dev/null 2>&1; then
  echo "error: GitHub CLI (gh) is not installed — see https://cli.github.com/" >&2; exit 1
fi
if ! gh auth status >/dev/null 2>&1; then
  echo "error: you are not logged in to gh. Run 'gh auth login' first." >&2; exit 1
fi
if [ ! -f "StreamForge.sln" ]; then
  echo "error: run this from the project root (no StreamForge.sln here)." >&2; exit 1
fi

echo ">> Initializing git repository..."
if [ ! -d ".git" ]; then
  git init -b main
fi

git add .
if git diff --cached --quiet; then
  echo ">> Nothing new to commit."
else
  git commit -m "Initial commit: StreamForge encoding orchestration MVP + docs"
fi

echo ">> Creating GitHub repo '${REPO_NAME}' (${VISIBILITY}) and pushing..."
gh repo create "${REPO_NAME}" \
  --"${VISIBILITY}" \
  --source=. \
  --remote=origin \
  --push \
  --description "StreamForge — a .NET FFmpeg-backed video encoding orchestration service"

echo ""
echo ">> Done. Repo created and pushed."
echo "   View it:        gh repo view --web"
echo ""
echo "   The CI workflow builds on every push. To publish the NuGet package later,"
echo "   tag a release (git tag v0.1.0 && git push --tags) after enabling the push"
echo "   step in .github/workflows/ci.yml with a NUGET_API_KEY secret."

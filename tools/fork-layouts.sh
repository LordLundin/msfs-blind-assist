#!/usr/bin/env bash
# fork-layouts.sh <store folder>
#
# For a fork, which has no deploy key for the layouts store (UPD-10): fills <store folder> with the
# layouts folder of the parent repository's newest published ZIP, the rolling preview while one
# exists (release.yml deletes it when a release is cut, so a standing preview is newer than the latest
# release), else the latest release. The workflow's LayoutBundle step then checks every file loads in
# this build, exactly as it does for the store. Needs gh with GH_TOKEN, and GITHUB_REPOSITORY.
set -euo pipefail

store="${1:?usage: fork-layouts.sh <store folder>}"
repo="${GITHUB_REPOSITORY:?GITHUB_REPOSITORY is not set}"

parent=$(gh api "repos/$repo" -q '.parent.full_name // empty')
if [ -z "$parent" ]; then
    echo "::error::$repo has no LAYOUTS_DEPLOY_KEY secret and is not a fork, so there is no layouts store and no parent release to take the display layouts from (UPD-10)."
    exit 1
fi

if gh release view preview -R "$parent" --json tagName >/dev/null 2>&1; then
    tag=preview
else
    tag=$(gh release view -R "$parent" --json tagName -q .tagName)
fi
echo "Display layouts from $parent, release $tag"

work=$(mktemp -d)
gh release download "$tag" -R "$parent" -p 'MSFSBA*.zip' -D "$work/zip"
zip=$(ls "$work"/zip/*.zip | head -n 1)

ps=pwsh
command -v pwsh >/dev/null 2>&1 || ps=powershell
"$ps" -NoProfile -Command "Expand-Archive -LiteralPath '$(cygpath -w "$zip" 2>/dev/null || echo "$zip")' -DestinationPath '$(cygpath -w "$work/x" 2>/dev/null || echo "$work/x")'"

if [ ! -d "$work/x/layouts" ]; then
    echo "::error::$parent's $tag ZIP has no layouts folder (UPD-10)."
    exit 1
fi

rm -rf "$store"
mv "$work/x/layouts" "$store"
find "$store" -type f | sort

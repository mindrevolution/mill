#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")"

# extract version from csproj
version=$(grep -oP '(?<=<Version>)[^<]+' cli/mill-cli.csproj)
tag="v$version"

echo "  > preparing release $tag"

# validate clean state
if [[ -n $(git status --porcelain) ]]; then
    echo "  x working directory not clean"
    exit 1
fi

# check if tag exists
if git rev-parse "$tag" >/dev/null 2>&1; then
    echo "  x tag $tag already exists"
    exit 1
fi

# create and push tag
git tag "$tag"
echo "  ✓ created tag $tag"

git push origin "$tag"
echo "  ✓ pushed to origin"

echo ""
echo "  release workflow triggered"
echo "  https://github.com/mindrevolution/mill/actions"

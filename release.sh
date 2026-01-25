#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")"

# extract version from csproj
version=$(grep -oP '(?<=<Version>)[^<]+' cli/mill-cli.csproj)
tag="v$version"

echo "  > preparing release $tag"

# warn if not on main branch
branch=$(git rev-parse --abbrev-ref HEAD)
if [[ "$branch" != "dev" && "$branch" != "main" ]]; then
    echo "  ! on branch '$branch', not dev/main"
    read -p "  press enter to continue or ctrl+c to abort"
fi

# validate no uncommitted changes (untracked files are ok)
if [[ -n $(git status --porcelain -uno) ]]; then
    echo "  x uncommitted changes"
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

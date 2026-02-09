#!/bin/bash
# Bump version and create git tag
# Reads current version from .csproj, bumps it, updates .csproj, and creates a git tag.
# Usage: ./bump-version.sh [major|minor|patch]

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

CSPROJ="HEICAutoConverter.csproj"

# Find .csproj relative to repo root
REPO_ROOT=$(git rev-parse --show-toplevel)
CSPROJ_PATH="${REPO_ROOT}/${CSPROJ}"

if [ ! -f "$CSPROJ_PATH" ]; then
    echo -e "${RED}Error: ${CSPROJ} not found at ${CSPROJ_PATH}${NC}"
    exit 1
fi

# Read current version from .csproj (single source of truth)
CURRENT_VERSION=$(grep -oP '(?<=<Version>)[^<]+' "$CSPROJ_PATH")
if [ -z "$CURRENT_VERSION" ]; then
    echo -e "${RED}Error: No <Version> found in ${CSPROJ}${NC}"
    exit 1
fi

echo -e "${YELLOW}Current version (from ${CSPROJ}): ${CURRENT_VERSION}${NC}"

# Check if git tag already exists for this version
if git rev-parse "v${CURRENT_VERSION}" >/dev/null 2>&1; then
    echo -e "${YELLOW}Tag v${CURRENT_VERSION} already exists${NC}"
else
    echo -e "${YELLOW}Note: No git tag exists yet for v${CURRENT_VERSION}${NC}"
fi

# Extract version parts
IFS='.' read -ra PARTS <<< "$CURRENT_VERSION"
MAJOR=${PARTS[0]:-0}
MINOR=${PARTS[1]:-0}
PATCH=${PARTS[2]:-0}

# Increment based on argument
BUMP_TYPE=${1:-patch}
case $BUMP_TYPE in
  major)
    MAJOR=$((MAJOR + 1))
    MINOR=0
    PATCH=0
    echo -e "${GREEN}Bumping MAJOR version${NC}"
    ;;
  minor)
    MINOR=$((MINOR + 1))
    PATCH=0
    echo -e "${GREEN}Bumping MINOR version${NC}"
    ;;
  patch)
    PATCH=$((PATCH + 1))
    echo -e "${GREEN}Bumping PATCH version${NC}"
    ;;
  *)
    echo -e "${RED}Error: Invalid bump type. Use: major, minor, or patch${NC}"
    echo "Usage: $0 [major|minor|patch]"
    exit 1
    ;;
esac

NEW_VERSION="${MAJOR}.${MINOR}.${PATCH}"
NEW_TAG="v${NEW_VERSION}"
echo -e "${GREEN}New version: ${NEW_VERSION} (tag: ${NEW_TAG})${NC}"

# Confirm before making changes
read -p "Update ${CSPROJ} and create tag ${NEW_TAG}? (y/N) " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo -e "${YELLOW}Cancelled.${NC}"
    exit 0
fi

# Update .csproj version
sed -i "s|<Version>${CURRENT_VERSION}</Version>|<Version>${NEW_VERSION}</Version>|" "$CSPROJ_PATH"
echo -e "${GREEN}Updated ${CSPROJ}: ${CURRENT_VERSION} -> ${NEW_VERSION}${NC}"

# Stage and commit the version bump
git add "$CSPROJ_PATH"
git commit -m "Bump version to ${NEW_VERSION}"
echo -e "${GREEN}Committed version bump${NC}"

# Get release notes
echo -e "${YELLOW}Enter release notes (Ctrl+D when done, or Enter then Ctrl+D to skip):${NC}"
RELEASE_NOTES=$(cat)

# Create annotated tag
if [ -z "$RELEASE_NOTES" ]; then
    git tag -a "$NEW_TAG" -m "Release ${NEW_VERSION}"
else
    git tag -a "$NEW_TAG" -m "Release ${NEW_VERSION}

${RELEASE_NOTES}"
fi

echo -e "${GREEN}Tag created: ${NEW_TAG}${NC}"
echo ""
echo "Next steps:"
echo "  1. Review: git show ${NEW_TAG}"
echo "  2. Push commit + tag: git push origin HEAD && git push origin ${NEW_TAG}"
echo "  3. Monitor: https://github.com/$(git remote get-url origin | sed 's/.*github.com[:/]\(.*\)\.git/\1/')/actions"
echo ""
echo -e "${YELLOW}To undo (delete tag + revert commit):${NC}"
echo "  git tag -d ${NEW_TAG} && git reset --soft HEAD~1"

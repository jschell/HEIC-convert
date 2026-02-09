#!/bin/bash
# Bump version and create git tag
# Usage: ./bump-version.sh [major|minor|patch]

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Get latest tag
LATEST_TAG=$(git describe --tags --abbrev=0 2>/dev/null || echo "v0.0.0")
echo -e "${YELLOW}Latest tag: ${LATEST_TAG}${NC}"

# Extract version parts
VERSION=${LATEST_TAG#v}
IFS='.' read -ra PARTS <<< "$VERSION"
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

NEW_VERSION="v${MAJOR}.${MINOR}.${PATCH}"
echo -e "${GREEN}New version: ${NEW_VERSION}${NC}"

# Confirm before creating tag
read -p "Create tag ${NEW_VERSION}? (y/N) " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo -e "${YELLOW}Cancelled.${NC}"
    exit 0
fi

# Get release notes
echo -e "${YELLOW}Enter release notes (Ctrl+D when done):${NC}"
RELEASE_NOTES=$(cat)

# Create annotated tag
if [ -z "$RELEASE_NOTES" ]; then
    git tag -a "$NEW_VERSION" -m "Release $NEW_VERSION"
else
    git tag -a "$NEW_VERSION" -m "Release $NEW_VERSION

$RELEASE_NOTES"
fi

echo -e "${GREEN}✓ Tag created: ${NEW_VERSION}${NC}"
echo ""
echo "Next steps:"
echo "  1. Review the tag: git show $NEW_VERSION"
echo "  2. Push to trigger release: git push origin $NEW_VERSION"
echo "  3. Monitor at: https://github.com/$(git remote get-url origin | sed 's/.*github.com[:/]\(.*\)\.git/\1/')/actions"
echo ""
echo -e "${YELLOW}To delete this tag if you made a mistake:${NC}"
echo "  git tag -d $NEW_VERSION"

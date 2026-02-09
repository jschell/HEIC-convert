# Release Creation Guide

## Quick Start - Create a Release

### Method 1: Automated Release (Recommended)

The repository has an automated release workflow that triggers on version tags.

#### Step 1: Create and Push a Tag

```bash
# Make sure you're on main branch and up to date
git checkout main
git pull origin main

# Create a version tag (format: v#.#.#)
git tag -a v1.0.0 -m "Release v1.0.0 - First stable release"

# Push the tag to GitHub
git push origin v1.0.0
```

#### Step 2: Automated Build Process

The workflow automatically:
1. ✅ Validates tag format (must be `v#.#.#`)
2. ✅ Runs all tests
3. ✅ Builds self-contained Windows executable
4. ✅ Creates ZIP archive
5. ✅ Generates SHA256 checksums
6. ✅ Creates GitHub release with auto-generated notes
7. ✅ Uploads binaries as release assets

#### Step 3: Monitor Progress

Watch the release build:
- Go to: https://github.com/jschell/HEIC-convert/actions
- Find the "Release" workflow run
- Wait for completion (~2-3 minutes)

#### Step 4: View the Release

Once complete, your release will be available at:
- https://github.com/jschell/HEIC-convert/releases

## Release Commands Reference

### Create a New Release
```bash
# For version 1.0.0
git tag -a v1.0.0 -m "Release v1.0.0: Initial release"
git push origin v1.0.0
```

### Create a Patch Release
```bash
# Bug fix release
git tag -a v1.0.1 -m "Release v1.0.1: Fix critical bug in conversion queue"
git push origin v1.0.1
```

### Create a Minor Release
```bash
# New features (backwards compatible)
git tag -a v1.1.0 -m "Release v1.1.0: Add dark mode support"
git push origin v1.1.0
```

### Create a Major Release
```bash
# Breaking changes
git tag -a v2.0.0 -m "Release v2.0.0: Major UI overhaul"
git push origin v2.0.0
```

### Delete a Tag (if you made a mistake)
```bash
# Delete local tag
git tag -d v1.0.0

# Delete remote tag
git push origin :refs/tags/v1.0.0
```

## Method 2: Manual Release (GitHub UI)

If you prefer to create releases manually:

### Step 1: Navigate to Releases
1. Go to https://github.com/jschell/HEIC-convert/releases
2. Click **"Draft a new release"**

### Step 2: Create Tag
1. Click **"Choose a tag"**
2. Type new tag name (e.g., `v1.0.0`)
3. Click **"Create new tag: v1.0.0 on publish"**

### Step 3: Fill Release Details
- **Release title**: `v1.0.0 - First Stable Release`
- **Description**: Describe what's new/changed
- Check **"Set as the latest release"**
- Optionally check **"Create a discussion"**

### Step 4: Upload Binaries
1. Build locally:
   ```bash
   dotnet publish HEICAutoConverter.csproj \
     --configuration Release \
     --runtime win-x64 \
     --self-contained true \
     -p:PublishSingleFile=true \
     -p:PublishTrimmed=true \
     --output ./publish
   ```
2. Drag `HEICAutoConverter.exe` to the release assets area

### Step 5: Publish
Click **"Publish release"**

## What Gets Released

The automated workflow builds and publishes:

### 📦 Release Artifacts

1. **HEICAutoConverter.exe** (Standalone executable)
   - Self-contained (no .NET runtime needed)
   - Single-file deployment
   - Trimmed for smaller size
   - ~50-60MB file size

2. **HEICAutoConverter-v#.#.#-win-x64.zip** (Zipped version)
   - Contains the same executable
   - Easier to download/distribute

3. **SHA256 Checksums** (in release notes)
   - For verifying download integrity
   - Security best practice

### 📝 Release Notes

Auto-generated content includes:
- List of commits since last release
- Pull requests merged
- Contributors
- System requirements
- Download instructions
- Checksums

## Versioning Strategy (Semantic Versioning)

Use [Semantic Versioning](https://semver.org/): `MAJOR.MINOR.PATCH`

### MAJOR version (v2.0.0)
Increment when you make **breaking changes**:
- Removing features
- Changing APIs
- Incompatible updates

### MINOR version (v1.1.0)
Increment when you add **new features** (backwards compatible):
- New conversion options
- Additional settings
- UI improvements

### PATCH version (v1.0.1)
Increment for **bug fixes** (backwards compatible):
- Crash fixes
- Performance improvements
- Security patches

## Pre-releases

For beta/alpha versions:

```bash
# Alpha release
git tag -a v1.0.0-alpha.1 -m "Release v1.0.0-alpha.1: Testing new features"
git push origin v1.0.0-alpha.1

# Beta release
git tag -a v1.0.0-beta.1 -m "Release v1.0.0-beta.1: Feature complete, testing"
git push origin v1.0.0-beta.1

# Release candidate
git tag -a v1.0.0-rc.1 -m "Release v1.0.0-rc.1: Release candidate"
git push origin v1.0.0-rc.1
```

Mark as pre-release in GitHub UI or workflow will auto-detect from tag name.

## Release Checklist

Before creating a release:

- [ ] All tests passing on main
- [ ] CI build successful
- [ ] Version bumped in code (if applicable)
- [ ] CHANGELOG updated (if you maintain one)
- [ ] Breaking changes documented
- [ ] Security vulnerabilities resolved
- [ ] Documentation updated
- [ ] Manual testing completed

## Troubleshooting

### "Tag must be in format v#.#.#"
**Problem:** Tag doesn't match expected format

**Solution:** Use semantic versioning format:
```bash
git tag -a v1.0.0 -m "Message"  # ✅ Correct
git tag -a 1.0.0 -m "Message"   # ❌ Missing 'v' prefix
git tag -a v1.0 -m "Message"    # ❌ Missing patch version
```

### Release workflow fails
**Problem:** Build or test failures during release

**Solution:**
1. Check the Actions tab for error details
2. Fix the issues
3. Delete the failed tag: `git push origin :refs/tags/v1.0.0`
4. Create a new tag after fixing

### Wrong version in binary
**Problem:** Executable shows wrong version

**Solution:** Version is extracted from tag automatically. Ensure:
- Tag format is correct: `v#.#.#`
- No typos in the tag

## Automation Enhancements (Optional)

### Auto-increment Version

Create a script to bump version automatically:

```bash
#!/bin/bash
# bump-version.sh

# Get latest tag
LATEST_TAG=$(git describe --tags --abbrev=0 2>/dev/null || echo "v0.0.0")
echo "Latest tag: $LATEST_TAG"

# Extract version parts
VERSION=${LATEST_TAG#v}
IFS='.' read -ra PARTS <<< "$VERSION"
MAJOR=${PARTS[0]}
MINOR=${PARTS[1]}
PATCH=${PARTS[2]}

# Increment based on argument
case $1 in
  major)
    MAJOR=$((MAJOR + 1))
    MINOR=0
    PATCH=0
    ;;
  minor)
    MINOR=$((MINOR + 1))
    PATCH=0
    ;;
  patch|*)
    PATCH=$((PATCH + 1))
    ;;
esac

NEW_VERSION="v${MAJOR}.${MINOR}.${PATCH}"
echo "New version: $NEW_VERSION"

# Create tag
git tag -a "$NEW_VERSION" -m "Release $NEW_VERSION"
echo "Tag created. Push with: git push origin $NEW_VERSION"
```

Usage:
```bash
chmod +x bump-version.sh
./bump-version.sh patch  # v1.0.0 -> v1.0.1
./bump-version.sh minor  # v1.0.1 -> v1.1.0
./bump-version.sh major  # v1.1.0 -> v2.0.0
```

## Related Files

- [Release Workflow](.github/workflows/release.yml) - Automated release process
- [CI Workflow](.github/workflows/ci.yml) - Pre-release testing
- [Project File](HEICAutoConverter.csproj) - Build configuration

## Examples

### Creating Your First Release

```bash
# Ensure main is up to date
git checkout main
git pull origin main

# Create v1.0.0 release
git tag -a v1.0.0 -m "Release v1.0.0

## What's New
- Automatic HEIC to JPG conversion
- Real-time folder monitoring
- Configurable output quality
- Multiple output strategies
- Windows toast notifications

## System Requirements
- Windows 10 (1809+) or Windows 11
- No .NET runtime required"

# Push the tag
git push origin v1.0.0

# Monitor at: https://github.com/jschell/HEIC-convert/actions
```

### Release Schedule Suggestion

- **Patch releases**: As needed for bugs (e.g., weekly if issues found)
- **Minor releases**: Monthly for new features
- **Major releases**: Yearly or when breaking changes necessary

## Support

For issues with releases:
- Check [Actions](https://github.com/jschell/HEIC-convert/actions) for build logs
- Review [Releases](https://github.com/jschell/HEIC-convert/releases) page
- Open an issue if workflow fails

# Releases

## How to Release

There are three ways to create a release. All of them end the same way: a `v*` tag on main triggers the [release workflow](.github/workflows/release.yml), which builds, smoke-tests, and publishes the executable to GitHub Releases.

### Method 1: Commit Message Keywords (Recommended)

Include a keyword in your PR title or merge commit message. When the commit lands on main, the [auto-release workflow](.github/workflows/auto-release.yml) reads the version from `.csproj`, creates a tag, and the release workflow takes over.

| Keyword | Effect |
|---------|--------|
| `[release]` | Tag current `.csproj` version as-is (use when you've already bumped the version) |
| `[bump patch]` | Bump patch (0.1.2 -> 0.1.3), update `.csproj`, commit, tag |
| `[bump minor]` | Bump minor (0.1.2 -> 0.2.0), update `.csproj`, commit, tag |
| `[bump major]` | Bump major (0.1.2 -> 1.0.0), update `.csproj`, commit, tag |

Examples:

```
Fix conversion crash on locked files [release]
Add dark mode support [bump minor]
Breaking: new settings format [bump major]
```

`[release]` is the default — it assumes you've already set the version in `.csproj` as part of your PR. Use `[bump patch]` / `[bump minor]` / `[bump major]` to let the workflow handle the version bump for you.

### Method 2: Bump Script (Local)

Run the script on main after merging. It reads the version from `.csproj`, bumps it, commits, creates a tag, and tells you what to push.

```bash
# Bash
./scripts/bump-version.sh patch    # 0.1.2 -> 0.1.3
./scripts/bump-version.sh minor    # 0.1.2 -> 0.2.0
./scripts/bump-version.sh major    # 0.1.2 -> 1.0.0

# PowerShell
.\scripts\bump-version.ps1 patch
.\scripts\bump-version.ps1 minor
.\scripts\bump-version.ps1 major
```

Then push both the commit and tag:

```bash
git push origin main && git push origin v0.1.3
```

### Method 3: GitHub UI

1. Go to [Releases](../../releases) -> **Draft a new release**
2. In **Choose a tag**, type `v0.1.3` -> **Create new tag on publish**
3. Select **main** as the target branch
4. Fill in title and notes, click **Publish release**

## What the Release Workflow Does

1. Validates tag format (`v#.#.#`)
2. Runs full test suite
3. Publishes self-contained single-file exe (`win-x64`, ~200 MB)
4. Smoke-tests the published exe (catches startup crashes)
5. Creates ZIP archive
6. Generates SHA256 checksums
7. Creates GitHub Release with artifacts and auto-generated notes

## Version Source of Truth

The version lives in `HEICAutoConverter.csproj`:

```xml
<Version>0.1.2</Version>
```

All release methods read from this file. The release workflow passes the tag version to `dotnet publish` via `-p:Version=`, so the exe file properties always match the tag.

## Versioning (Semver)

| Bump | When | Example |
|------|------|---------|
| **Patch** | Bug fixes, security patches | 0.1.2 -> 0.1.3 |
| **Minor** | New features, backwards compatible | 0.1.2 -> 0.2.0 |
| **Major** | Breaking changes | 0.1.2 -> 1.0.0 |

## Fixing a Bad Release

```bash
# Delete local and remote tag
git tag -d v0.1.3
git push origin :refs/tags/v0.1.3

# Delete the GitHub Release from the UI, fix the issue, then re-release
```

## Related Files

| File | Purpose |
|------|---------|
| [`.github/workflows/release.yml`](.github/workflows/release.yml) | Build and publish on tag push |
| [`.github/workflows/auto-release.yml`](.github/workflows/auto-release.yml) | Create tag from commit message keywords |
| [`.github/workflows/ci.yml`](.github/workflows/ci.yml) | Test and smoke-test on every PR |
| [`scripts/bump-version.sh`](scripts/bump-version.sh) | Local version bump (bash) |
| [`scripts/bump-version.ps1`](scripts/bump-version.ps1) | Local version bump (PowerShell) |
| [`HEICAutoConverter.csproj`](HEICAutoConverter.csproj) | Version source of truth |

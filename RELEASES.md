# Releases

## How to Release

Include a keyword in your PR title or merge commit message. When the commit lands on main, the [release workflow](.github/workflows/release.yml) bumps the version, builds, tests, and publishes to GitHub Releases — all in one workflow.

| Keyword | Effect |
|---------|--------|
| `[bump patch]` | Bump patch (0.1.2 -> 0.1.3), build, release |
| `[bump minor]` | Bump minor (0.1.2 -> 0.2.0), build, release |
| `[bump major]` | Bump major (0.1.2 -> 1.0.0), build, release |
| `[release]` | Release current `.csproj` version as-is (use when you've already bumped) |

Examples:

```
Fix conversion crash on locked files [bump patch]
Add dark mode support [bump minor]
Breaking: new settings format [bump major]
```

No keyword = no release. The commit lands on main and nothing happens beyond the merge.

### Dependabot Auto-Release

Dependabot PRs are handled automatically by the [auto-merge workflow](.github/workflows/dependabot-auto-merge.yml):

| Update Type | What Happens |
|-------------|-------------|
| Patch / Minor | CI must pass. Auto-approved, squash-merged with `[bump patch]`, which triggers a release. |
| Major | Auto-approved but **not merged**. Requires manual review for breaking changes. |

Full chain: Dependabot PR → CI passes → auto-merge with `[bump patch]` → release workflow bumps version, builds, tags, publishes. No manual steps.

### Local Bump Script

Run on main after merging. Reads version from `.csproj`, bumps, commits, creates tag, and pushes.

```bash
# Bash
./scripts/bump-version.sh patch    # 0.1.2 -> 0.1.3
./scripts/bump-version.sh minor    # 0.1.2 -> 0.2.0

# PowerShell
.\scripts\bump-version.ps1 patch
.\scripts\bump-version.ps1 minor
```

**Note:** Local bump scripts push a tag directly. The release workflow only triggers from push-to-main with a keyword — so if using the script, you'll need to manually create the GitHub Release from the tag via the UI, or push a commit with `[release]` instead.

## What the Release Workflow Does

1. Checks commit message for release keyword (skips if none)
2. Bumps `.csproj` version and commits (for `[bump *]` keywords)
3. Builds and runs full test suite
4. Publishes self-contained single-file exe (`win-x64`, ~200 MB)
5. Smoke-tests the published exe (catches startup crashes)
6. Creates tag **only after build passes** (no orphaned tags from failed builds)
7. Creates ZIP archive with SHA256 checksums
8. Publishes GitHub Release with artifacts

## Version Source of Truth

The version lives in `HEICAutoConverter.csproj`:

```xml
<Version>0.1.6</Version>
```

All release methods read from this file. The release workflow passes the version to `dotnet publish` via `-p:Version=`, so the exe file properties always match the tag.

## Versioning (Semver)

| Bump | When | Example |
|------|------|---------|
| **Patch** | Bug fixes, dependency updates, security patches | 0.1.2 -> 0.1.3 |
| **Minor** | New features, backwards compatible | 0.1.2 -> 0.2.0 |
| **Major** | Breaking changes | 0.1.2 -> 1.0.0 |

## Fixing a Bad Release

```bash
# Delete local and remote tag
git tag -d v0.1.3
git push origin :refs/tags/v0.1.3

# Delete the GitHub Release from the UI, fix the issue, then re-release
```

## Setup: RELEASE_TOKEN

The release workflow requires a Personal Access Token to push version bump commits and tags to main. (GitHub's default `GITHUB_TOKEN` cannot trigger workflows or push when branch protection is active.)

1. Go to **GitHub Settings** → **Developer settings** → **Fine-grained personal access tokens**
2. Create a token with **Contents: Read and write** permission scoped to this repo
3. In the repo, go to **Settings** → **Secrets and variables** → **Actions**
4. Add a repository secret named `RELEASE_TOKEN` with the token value

## Related Files

| File | Purpose |
|------|---------|
| [`.github/workflows/release.yml`](.github/workflows/release.yml) | Version bump, build, tag, and publish on push to main |
| [`.github/workflows/dependabot-auto-merge.yml`](.github/workflows/dependabot-auto-merge.yml) | Auto-merge Dependabot PRs with `[bump patch]` |
| [`.github/workflows/ci.yml`](.github/workflows/ci.yml) | PR gate — test and smoke-test before merge |
| [`scripts/bump-version.sh`](scripts/bump-version.sh) | Local version bump (bash) |
| [`scripts/bump-version.ps1`](scripts/bump-version.ps1) | Local version bump (PowerShell) |
| [`HEICAutoConverter.csproj`](HEICAutoConverter.csproj) | Version source of truth |

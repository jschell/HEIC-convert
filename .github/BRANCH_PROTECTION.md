# Branch Protection

Protection rules for the `main` branch. Designed for a solo-developer repo with Dependabot auto-merge.

## Active Rules

| Setting | Value | Rationale |
|---------|-------|-----------|
| Require status checks | `build-and-test` | Blocks merge until CI passes (build, test, publish, smoke test) |
| Require up-to-date branch | No | Avoids rebase bottleneck when multiple Dependabot PRs are open |
| Require PR approvals | No | Solo repo — would block Dependabot auto-merge |
| Enforce for admins | No | Allows direct pushes and release bot version bumps |
| Force pushes | Disabled | Safety net |
| Branch deletion | Disabled | Safety net |

## How It Works

```
Dependabot PR opens (or any PR to main)
       │
  ci.yml (pull_request) ─── build + test + publish + smoke
       │
  +────+────+
  │         │
PASS      FAIL ──> merge blocked, PR stays open
  │
  ├── patch/minor ──> auto-approved, auto-merged with [bump patch]
  │
  └── major ──> auto-approved, requires manual merge
       │
  PR merges to main
       │
  release.yml (push to main)
  ├── check-release (ubuntu, ~30s)
  │     no keyword? ──> done, no build
  │     [bump patch]? ──> bump .csproj, commit, push
  │
  └── build-and-release (windows, ~10min)
        build + test + publish + smoke test
        zip + checksums
        create tag (only after build passes)
        create GitHub Release with artifacts
```

Workflows:
- [`ci.yml`](workflows/ci.yml) — PR gate (build, test, publish, smoke test)
- [`dependabot-auto-merge.yml`](workflows/dependabot-auto-merge.yml) — approve and merge passing Dependabot PRs
- [`release.yml`](workflows/release.yml) — version bump, build, tag, and publish GitHub Release

## Setup

Branch protection was configured via the GitHub API:

```bash
gh api repos/jschell/HEIC-convert/branches/main/protection \
  --method PUT \
  --input - <<'EOF'
{
  "required_status_checks": { "strict": false, "contexts": ["build-and-test"] },
  "enforce_admins": false,
  "required_pull_request_reviews": null,
  "restrictions": null,
  "allow_force_pushes": false,
  "allow_deletions": false
}
EOF
```

Or via GitHub UI: **Settings > Branches > Add rule > `main` > Require status checks > `build-and-test` > Save**

## Prerequisites

| Secret | Scope | Used by |
|--------|-------|---------|
| `RELEASE_TOKEN` | Contents: Read and write | `release.yml` — pushes version bump commits and tags |

`GITHUB_TOKEN` (built-in) is sufficient for all other workflows.

**Note:** `enforce_admins` must remain `false` — the release workflow pushes version bump commits directly to main. Enabling it would break the release chain.

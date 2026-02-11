# Branch Protection

Protection rules for the `main` branch. Designed for a solo-developer repo with Dependabot auto-merge.

## Active Rules

| Setting | Value | Rationale |
|---------|-------|-----------|
| Require status checks | `build-and-test` | Blocks merge until CI passes (build, test, publish, smoke test) |
| Require up-to-date branch | No | Avoids rebase bottleneck when multiple Dependabot PRs are open |
| Require PR approvals | No | Solo repo — would block Dependabot auto-merge |
| Enforce for admins | No | Allows direct pushes for hotfixes |
| Force pushes | Disabled | Safety net |
| Branch deletion | Disabled | Safety net |

## How It Works

```
Dependabot PR opens
       |
  CI runs (ci.yml)
       |
  +----+----+
  |         |
PASS      FAIL --> merge blocked, PR stays open
  |
  +--- patch/minor --> auto-approved, auto-merged with [bump patch]
  |
  +--- major --> auto-approved, requires manual merge
```

Workflows involved:
- [`ci.yml`](workflows/ci.yml) — build, test, publish, smoke test
- [`dependabot-auto-merge.yml`](workflows/dependabot-auto-merge.yml) — approve and merge passing PRs
- [`auto-release.yml`](workflows/auto-release.yml) — creates tag + release from `[bump patch]` keyword

## Setup

Branch protection is configured via the GitHub API. Run once:

```bash
curl -L \
  -X PUT \
  -H "Accept: application/vnd.github+json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  "https://api.github.com/repos/jschell/HEIC-convert/branches/main/protection" \
  -d '{
    "required_status_checks": {
      "strict": false,
      "contexts": ["build-and-test"]
    },
    "enforce_admins": false,
    "required_pull_request_reviews": null,
    "restrictions": null,
    "allow_force_pushes": false,
    "allow_deletions": false
  }'
```

Replace `YOUR_TOKEN` with a PAT that has admin scope on this repo, or use the GitHub UI:

**Settings > Branches > Add rule > `main` > Require status checks > `build-and-test` > Save**

## Prerequisites

| Secret | Scope | Used by |
|--------|-------|---------|
| `RELEASE_TOKEN` | Contents: Read and write | `auto-release.yml` — pushes tags that trigger `release.yml` |

`GITHUB_TOKEN` (built-in) is sufficient for all other workflows.

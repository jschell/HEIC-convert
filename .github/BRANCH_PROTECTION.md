# Branch Protection Rules

This document outlines the recommended branch protection rules for this repository to ensure code quality and prevent broken builds from being merged.

## Recommended Rules for `main` Branch

### Required Status Checks

Enable **"Require status checks to pass before merging"** with:

- ✅ **Require branches to be up to date before merging**
- ✅ Required checks:
  - `build-and-test` (from CI workflow)
  - All tests must pass
  - Build must succeed

### Pull Request Requirements

- ✅ **Require a pull request before merging**
  - Require approvals: **1** (recommended)
  - Dismiss stale pull request approvals when new commits are pushed
  - Require review from Code Owners (optional)

### Additional Protections

- ✅ **Require conversation resolution before merging**
- ✅ **Require linear history** (optional, prevents merge commits)
- ✅ **Include administrators** (enforce rules for everyone)

### Bypass Settings

- ❌ **Do not allow force pushes**
- ❌ **Do not allow deletions**

## How to Configure

### Via GitHub UI

1. Go to **Settings** → **Branches**
2. Click **Add branch protection rule**
3. Set **Branch name pattern**: `main`
4. Configure settings as outlined above
5. Click **Create** or **Save changes**

### Via GitHub CLI

```bash
gh api repos/{owner}/{repo}/branches/main/protection \
  --method PUT \
  --field required_status_checks[strict]=true \
  --field required_status_checks[contexts][]=build-and-test \
  --field required_pull_request_reviews[required_approving_review_count]=1 \
  --field required_pull_request_reviews[dismiss_stale_reviews]=true \
  --field required_conversation_resolution[enabled]=true \
  --field enforce_admins[enabled]=true \
  --field allow_force_pushes[enabled]=false \
  --field allow_deletions[enabled]=false
```

### Via Terraform

```hcl
resource "github_branch_protection" "main" {
  repository_id = github_repository.repo.node_id
  pattern       = "main"

  required_status_checks {
    strict   = true
    contexts = ["build-and-test"]
  }

  required_pull_request_reviews {
    dismiss_stale_reviews           = true
    require_code_owner_reviews      = false
    required_approving_review_count = 1
  }

  enforce_admins                  = true
  require_conversation_resolution = true
  allow_force_pushes             = false
  allow_deletions                = false
}
```

## Dependabot Integration

With these rules in place:

1. ✅ Dependabot PRs **must pass CI** before merging
2. ✅ Failed Dependabot PRs **will be blocked automatically**
3. ✅ The [Dependabot Failure Monitor](.github/workflows/dependabot-failure-monitor.yml) workflow will create issues for failed PRs
4. ⚠️ You can still **manually close** failed Dependabot PRs without merging

## Auto-Merge for Dependabot (Optional)

To enable auto-merge for Dependabot PRs that pass all checks:

### 1. Enable Auto-Merge in Repository Settings

1. Go to **Settings** → **General**
2. Scroll to **Pull Requests**
3. Check ✅ **Allow auto-merge**

### 2. Create Dependabot Auto-Merge Workflow

Create `.github/workflows/dependabot-auto-merge.yml`:

```yaml
name: Dependabot Auto-Merge

on:
  pull_request:
    types:
      - opened
      - synchronize
      - reopened

permissions:
  contents: write
  pull-requests: write

jobs:
  auto-merge:
    runs-on: ubuntu-latest
    if: github.actor == 'dependabot[bot]'

    steps:
      - name: Enable auto-merge for Dependabot PRs
        run: gh pr merge --auto --squash "$PR_URL"
        env:
          PR_URL: ${{ github.event.pull_request.html_url }}
          GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

### 3. Configure Dependabot Auto-Merge Preferences

In `.github/dependabot.yml`, you can add PR labeling to control which PRs auto-merge:

```yaml
updates:
  - package-ecosystem: "nuget"
    # ... other settings ...
    labels:
      - "dependencies"
      - "automerge"  # Add this label for auto-merge eligibility
```

## Testing the Configuration

1. Create a test PR from a feature branch
2. Verify that the PR is blocked until CI passes
3. Push a commit that breaks tests
4. Verify that the PR cannot be merged
5. Fix the tests and verify merge is allowed

## Monitoring

- **Failed Dependabot PRs**: Check the [Issues](../../issues?q=is%3Aissue+is%3Aopen+label%3Adependabot-failure) page for `dependabot-failure` label
- **CI Status**: Check the [Actions](../../actions) page for workflow runs
- **Protected Branches**: Settings → Branches → Branch protection rules

## Troubleshooting

### "Required status checks are failing"

- Wait for CI to complete
- Check the Actions tab for failure details
- Fix failing tests or code issues
- Push new commits to update the PR

### "Merge is blocked due to branch protection"

- Ensure all required status checks are passing
- Get required number of approvals
- Resolve all conversations if required
- Update branch to match base if "up to date" is required

### Dependabot PR stuck

- Check if issue was created by Dependabot Failure Monitor
- Review the build logs linked in the issue
- Either fix code to accommodate new version or close PR
- Consider adding version constraints in dependabot.yml

## Related Documentation

- [GitHub Branch Protection Docs](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/about-protected-branches)
- [Dependabot Configuration](dependabot.yml)
- [CI Workflow](workflows/ci.yml)
- [Dependabot Failure Monitor](workflows/dependabot-failure-monitor.yml)

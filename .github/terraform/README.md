# Branch Protection as Code

This directory contains Infrastructure as Code (IaC) configurations for GitHub branch protection.

## Available Options

### Option 1: GitHub Actions Workflow (Recommended)

**File:** [`..workflows/setup-branch-protection.yml`](../workflows/setup-branch-protection.yml)

**Pros:**
- ✅ Runs automatically on push or manual trigger
- ✅ No local tools required
- ✅ Version controlled and auditable
- ✅ Can be triggered from GitHub UI

**Setup:**

1. Create a Personal Access Token (classic):
   - Go to: https://github.com/settings/tokens
   - Generate new token with `repo` scope (full control)
   - Copy the token

2. Add as repository secret:
   - Go to: https://github.com/jschell/HEIC-convert/settings/secrets/actions
   - Click "New repository secret"
   - Name: `BRANCH_PROTECTION_TOKEN`
   - Value: Your token

3. Run the workflow:
   - Go to: https://github.com/jschell/HEIC-convert/actions/workflows/setup-branch-protection.yml
   - Click "Run workflow"
   - Wait for completion

**Note:** The workflow will fail if using default `GITHUB_TOKEN` because it lacks admin permissions.

### Option 2: Shell Script

**File:** [`../scripts/setup-branch-protection.sh`](../scripts/setup-branch-protection.sh)

**Pros:**
- ✅ Simple and straightforward
- ✅ Works with `gh` CLI
- ✅ Fast execution

**Prerequisites:**
```bash
# Install GitHub CLI
brew install gh  # macOS
# or
sudo apt install gh  # Ubuntu/Debian

# Authenticate
gh auth login
```

**Usage:**
```bash
cd .github/scripts
chmod +x setup-branch-protection.sh
./setup-branch-protection.sh
```

### Option 3: Terraform (Infrastructure as Code)

**File:** [`branch-protection.tf`](branch-protection.tf)

**Pros:**
- ✅ Full IaC approach
- ✅ State management
- ✅ Plan before apply
- ✅ Can manage multiple repositories
- ✅ Integrates with CI/CD pipelines

**Prerequisites:**
```bash
# Install Terraform
brew install terraform  # macOS
# or
wget https://releases.hashicorp.com/terraform/1.7.0/terraform_1.7.0_linux_amd64.zip
unzip terraform_1.7.0_linux_amd64.zip
sudo mv terraform /usr/local/bin/
```

**Setup:**
```bash
# Set GitHub token
export GITHUB_TOKEN="ghp_your_token_here"

# Navigate to this directory
cd .github/terraform

# Initialize Terraform
terraform init

# Preview changes
terraform plan

# Apply configuration
terraform apply
```

**Destroy (remove protection):**
```bash
terraform destroy
```

## Comparison Matrix

| Feature | GitHub Actions | Shell Script | Terraform |
|---------|---------------|--------------|-----------|
| Auto-apply on push | ✅ | ❌ | ⚠️ Via CI |
| Local execution | ❌ | ✅ | ✅ |
| State tracking | ❌ | ❌ | ✅ |
| Preview changes | ❌ | ❌ | ✅ |
| Multiple repos | ⚠️ | ⚠️ | ✅ |
| No local tools | ✅ | ❌ | ❌ |
| Rollback support | ❌ | ❌ | ✅ |

## What Gets Configured

All three methods configure the same protection rules:

- ✅ **Required status check:** `build-and-test` must pass
- ✅ **Require branches to be up to date** before merging
- ✅ **Require 1 pull request review** before merging
- ✅ **Dismiss stale reviews** when new commits are pushed
- ✅ **Require conversation resolution** before merging
- ✅ **Enforce for administrators** (no bypass)
- ✅ **Prevent force pushes** to main
- ✅ **Prevent branch deletion** of main

## Verification

After applying, verify the configuration:

```bash
# Using GitHub CLI
gh api /repos/jschell/HEIC-convert/branches/main/protection

# Using curl
curl -H "Authorization: token YOUR_TOKEN" \
  https://api.github.com/repos/jschell/HEIC-convert/branches/main/protection

# Or visit:
# https://github.com/jschell/HEIC-convert/settings/branches
```

## Troubleshooting

### "Resource not accessible by integration"

**Problem:** Default `GITHUB_TOKEN` doesn't have admin permissions.

**Solution:** Create and use a Personal Access Token with `repo` scope.

### "Branch protection rule already exists"

**Problem:** Rules were previously configured manually.

**Solution:** Either:
1. Delete existing rules first (Settings → Branches → Delete rule)
2. Or let the script/workflow update the existing rules (works with PUT method)

### Terraform state conflicts

**Problem:** Multiple people running Terraform.

**Solution:** Use remote state backend (S3, Terraform Cloud, etc.)

```hcl
terraform {
  backend "s3" {
    bucket = "my-terraform-state"
    key    = "github/branch-protection.tfstate"
    region = "us-east-1"
  }
}
```

## Related Documentation

- [GitHub Branch Protection API](https://docs.github.com/en/rest/branches/branch-protection)
- [GitHub Terraform Provider](https://registry.terraform.io/providers/integrations/github/latest/docs)
- [GitHub CLI Manual](https://cli.github.com/manual/)
- [Branch Protection Guide](../BRANCH_PROTECTION.md)

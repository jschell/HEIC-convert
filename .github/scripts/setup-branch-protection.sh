#!/bin/bash
# Setup branch protection rules for main branch
# Requires: gh CLI installed and authenticated
# Usage: ./setup-branch-protection.sh

set -e

REPO="jschell/HEIC-convert"
BRANCH="main"

echo "🔒 Setting up branch protection for ${REPO}:${BRANCH}..."

# Create branch protection rule
gh api \
  --method PUT \
  -H "Accept: application/vnd.github+json" \
  -H "X-GitHub-Api-Version: 2022-11-28" \
  "/repos/${REPO}/branches/${BRANCH}/protection" \
  -f required_status_checks[strict]=true \
  -f required_status_checks[contexts][]=build-and-test \
  -f required_pull_request_reviews[dismiss_stale_reviews]=true \
  -f required_pull_request_reviews[require_code_owner_reviews]=false \
  -f required_pull_request_reviews[required_approving_review_count]=1 \
  -f required_pull_request_reviews[require_last_push_approval]=false \
  -f enforce_admins=true \
  -f required_conversation_resolution=true \
  -f allow_force_pushes=false \
  -f allow_deletions=false \
  -f block_creations=false \
  -f required_linear_history=false \
  -f lock_branch=false

echo "✅ Branch protection configured successfully!"
echo ""
echo "Rules applied to ${BRANCH}:"
echo "  ✓ Required status check: build-and-test"
echo "  ✓ Require branches to be up to date"
echo "  ✓ Require 1 approving review"
echo "  ✓ Dismiss stale reviews on new commits"
echo "  ✓ Require conversation resolution"
echo "  ✓ Enforce for administrators"
echo "  ✓ Prevent force pushes"
echo "  ✓ Prevent branch deletion"
echo ""
echo "View configuration: https://github.com/${REPO}/settings/branches"

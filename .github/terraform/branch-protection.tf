# Terraform configuration for GitHub branch protection
#
# Prerequisites:
# 1. Install Terraform: https://www.terraform.io/downloads
# 2. Create GitHub Personal Access Token with 'repo' scope
# 3. Set environment variable: export GITHUB_TOKEN="your_token_here"
#
# Usage:
#   terraform init
#   terraform plan
#   terraform apply

terraform {
  required_version = ">= 1.0"

  required_providers {
    github = {
      source  = "integrations/github"
      version = "~> 6.0"
    }
  }
}

provider "github" {
  owner = "jschell"
  # Token from GITHUB_TOKEN environment variable
}

# Branch protection for main branch
resource "github_branch_protection" "main" {
  repository_id = "HEIC-convert"
  pattern       = "main"

  # Require status checks to pass before merging
  required_status_checks {
    strict   = true # Require branches to be up to date
    contexts = ["build-and-test"]
  }

  # Require pull request reviews before merging
  required_pull_request_reviews {
    dismiss_stale_reviews           = true
    require_code_owner_reviews      = false
    required_approving_review_count = 1
    require_last_push_approval      = false
  }

  # Enforce all configured restrictions for administrators
  enforce_admins = true

  # Require conversation resolution before merging
  require_conversation_resolution = true

  # Prevent force pushes
  allows_force_pushes = false

  # Prevent branch deletion
  allows_deletions = false

  # Allow branch creation (for PRs)
  # allows_creations = false # Not commonly restricted

  # Require linear history (optional)
  # required_linear_history = true

  # Lock branch (prevents all changes)
  # lock_branch = false
}

# Output the protection details
output "branch_protection" {
  description = "Branch protection configuration for main"
  value = {
    repository = github_branch_protection.main.repository_id
    pattern    = github_branch_protection.main.pattern
    strict     = github_branch_protection.main.required_status_checks[0].strict
    contexts   = github_branch_protection.main.required_status_checks[0].contexts
  }
}

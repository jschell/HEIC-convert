#!/usr/bin/env python3
"""
Build Status Checker - Validates GitHub Actions workflows and checks for build issues
"""

import yaml
import json
import os
import sys
from pathlib import Path

def validate_yaml(file_path):
    """Validate YAML syntax and structure"""
    try:
        with open(file_path, 'r') as f:
            data = yaml.safe_load(f)
        return True, data, None
    except Exception as e:
        return False, None, str(e)

def check_workflow_structure(workflow_data, workflow_name):
    """Check workflow structure for common issues"""
    issues = []
    warnings = []

    # Check for required fields
    if 'jobs' not in workflow_data:
        issues.append("Missing 'jobs' section")
        return issues, warnings

    for job_name, job_data in workflow_data['jobs'].items():
        # Check for timeout
        if 'timeout-minutes' in job_data:
            timeout = job_data['timeout-minutes']
            warnings.append(f"✅ Job '{job_name}' has timeout: {timeout} minutes")
        else:
            warnings.append(f"⚠️ Job '{job_name}' has no timeout (will use default)")

        # Check for runs-on
        if 'runs-on' not in job_data:
            issues.append(f"Job '{job_name}' missing 'runs-on'")

        # Check steps
        if 'steps' not in job_data:
            issues.append(f"Job '{job_name}' missing 'steps'")
            continue

        # Analyze steps
        steps = job_data['steps']
        step_names = [s.get('name', 'unnamed') for s in steps]

        # Check for caching
        has_cache = any('cache' in name.lower() for name in step_names)
        if has_cache:
            warnings.append(f"✅ Job '{job_name}' uses caching")

        # Check for tests
        has_test = any('test' in name.lower() for name in step_names)
        if has_test:
            warnings.append(f"✅ Job '{job_name}' includes tests")

        # Check for artifact uploads
        has_artifacts = any(s.get('uses', '').startswith('actions/upload-artifact') for s in steps)
        if has_artifacts:
            artifact_count = sum(1 for s in steps if s.get('uses', '').startswith('actions/upload-artifact'))
            warnings.append(f"✅ Job '{job_name}' uploads {artifact_count} artifact(s)")

    return issues, warnings

def check_project_files():
    """Check .csproj files for issues"""
    issues = []
    warnings = []

    # Find all .csproj files
    csproj_files = list(Path('.').rglob('*.csproj'))

    for csproj in csproj_files:
        try:
            with open(csproj, 'r') as f:
                content = f.read()

            # Check for test project
            if 'IsTestProject' in content and 'true' in content:
                # Check for coverlet
                if 'coverlet.collector' in content:
                    warnings.append(f"✅ {csproj.name}: Has code coverage (coverlet.collector)")
                else:
                    warnings.append(f"⚠️ {csproj.name}: Missing code coverage package")

                # Check for test framework
                if 'xunit' in content:
                    warnings.append(f"✅ {csproj.name}: Uses xUnit test framework")
        except Exception as e:
            issues.append(f"Error reading {csproj}: {e}")

    return issues, warnings

def main():
    print("=" * 80)
    print("GitHub Actions Workflow Validation")
    print("=" * 80)
    print()

    all_issues = []
    all_warnings = []

    # Check CI workflow
    print("📋 Validating CI Workflow (.github/workflows/ci.yml)")
    print("-" * 80)

    ci_valid, ci_data, ci_error = validate_yaml('.github/workflows/ci.yml')
    if not ci_valid:
        print(f"❌ YAML Syntax Error: {ci_error}")
        all_issues.append(f"CI workflow YAML error: {ci_error}")
    else:
        print("✅ YAML syntax valid")
        issues, warnings = check_workflow_structure(ci_data, "CI")
        all_issues.extend(issues)
        all_warnings.extend(warnings)

        for warning in warnings:
            print(f"  {warning}")

    print()

    # Check Release workflow
    print("📋 Validating Release Workflow (.github/workflows/release.yml)")
    print("-" * 80)

    release_valid, release_data, release_error = validate_yaml('.github/workflows/release.yml')
    if not release_valid:
        print(f"❌ YAML Syntax Error: {release_error}")
        all_issues.append(f"Release workflow YAML error: {release_error}")
    else:
        print("✅ YAML syntax valid")
        issues, warnings = check_workflow_structure(release_data, "Release")
        all_issues.extend(issues)
        all_warnings.extend(warnings)

        for warning in warnings:
            print(f"  {warning}")

    print()

    # Check project files
    print("📋 Validating Project Files")
    print("-" * 80)

    issues, warnings = check_project_files()
    all_issues.extend(issues)
    all_warnings.extend(warnings)

    for warning in warnings:
        print(f"  {warning}")

    print()

    # Check Dependabot
    print("📋 Validating Dependabot Configuration")
    print("-" * 80)

    if os.path.exists('.github/dependabot.yml'):
        dep_valid, dep_data, dep_error = validate_yaml('.github/dependabot.yml')
        if dep_valid:
            print("✅ Dependabot configuration valid")
            if 'updates' in dep_data:
                ecosystems = [u['package-ecosystem'] for u in dep_data['updates']]
                print(f"  ✅ Monitoring {len(ecosystems)} ecosystem(s): {', '.join(ecosystems)}")
        else:
            print(f"❌ Dependabot YAML error: {dep_error}")
            all_issues.append(f"Dependabot error: {dep_error}")
    else:
        print("⚠️ No Dependabot configuration found")

    print()
    print("=" * 80)
    print("Summary")
    print("=" * 80)

    if all_issues:
        print(f"\n❌ Found {len(all_issues)} issue(s):")
        for issue in all_issues:
            print(f"  - {issue}")
        return 1
    else:
        print(f"\n✅ No issues found!")
        print(f"📊 Total validations: {len(all_warnings)}")
        print("\n🎯 Build Status: READY TO RUN")
        print("🔒 Confidence Level: HIGH")
        return 0

if __name__ == "__main__":
    sys.exit(main())

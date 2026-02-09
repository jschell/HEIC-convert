# Bump version and create git tag
# Reads current version from .csproj, bumps it, updates .csproj, and creates a git tag.
# Usage: .\bump-version.ps1 [major|minor|patch]

param(
    [ValidateSet("major", "minor", "patch")]
    [string]$BumpType = "patch"
)

$ErrorActionPreference = "Stop"

$Csproj = "HEICAutoConverter.csproj"
$RepoRoot = git rev-parse --show-toplevel 2>$null
if (-not $RepoRoot) {
    Write-Host "Error: Not inside a git repository" -ForegroundColor Red
    exit 1
}

$CsprojPath = Join-Path $RepoRoot $Csproj
if (-not (Test-Path $CsprojPath)) {
    Write-Host "Error: ${Csproj} not found at ${CsprojPath}" -ForegroundColor Red
    exit 1
}

# Read current version from .csproj (single source of truth)
$Content = Get-Content $CsprojPath -Raw
if ($Content -match '<Version>([^<]+)</Version>') {
    $CurrentVersion = $Matches[1]
} else {
    Write-Host "Error: No <Version> found in ${Csproj}" -ForegroundColor Red
    exit 1
}

Write-Host "Current version (from ${Csproj}): ${CurrentVersion}" -ForegroundColor Yellow

# Check if git tag already exists for this version
$TagExists = git rev-parse "v${CurrentVersion}" 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Host "Tag v${CurrentVersion} already exists" -ForegroundColor Yellow
} else {
    Write-Host "Note: No git tag exists yet for v${CurrentVersion}" -ForegroundColor Yellow
}

# Extract version parts
$Parts = $CurrentVersion.Split('.')
$Major = [int]$Parts[0]
$Minor = [int]$Parts[1]
$Patch = [int]$Parts[2]

# Increment based on argument
switch ($BumpType) {
    "major" {
        $Major++
        $Minor = 0
        $Patch = 0
        Write-Host "Bumping MAJOR version" -ForegroundColor Green
    }
    "minor" {
        $Minor++
        $Patch = 0
        Write-Host "Bumping MINOR version" -ForegroundColor Green
    }
    "patch" {
        $Patch++
        Write-Host "Bumping PATCH version" -ForegroundColor Green
    }
}

$NewVersion = "${Major}.${Minor}.${Patch}"
$NewTag = "v${NewVersion}"
Write-Host "New version: ${NewVersion} (tag: ${NewTag})" -ForegroundColor Green

# Confirm before making changes
$Confirm = Read-Host "Update ${Csproj} and create tag ${NewTag}? (y/N)"
if ($Confirm -ne 'y' -and $Confirm -ne 'Y') {
    Write-Host "Cancelled." -ForegroundColor Yellow
    exit 0
}

# Update .csproj version
$Content = $Content -replace "<Version>${CurrentVersion}</Version>", "<Version>${NewVersion}</Version>"
Set-Content -Path $CsprojPath -Value $Content -NoNewline
Write-Host "Updated ${Csproj}: ${CurrentVersion} -> ${NewVersion}" -ForegroundColor Green

# Stage and commit the version bump
git add $CsprojPath
git commit -m "Bump version to ${NewVersion}"
Write-Host "Committed version bump" -ForegroundColor Green

# Get release notes
Write-Host "Enter release notes (blank line to skip):" -ForegroundColor Yellow
$ReleaseNotes = Read-Host

# Create annotated tag
if ([string]::IsNullOrWhiteSpace($ReleaseNotes)) {
    git tag -a $NewTag -m "Release ${NewVersion}"
} else {
    git tag -a $NewTag -m "Release ${NewVersion}`n`n${ReleaseNotes}"
}

Write-Host "Tag created: ${NewTag}" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:"
Write-Host "  1. Review: git show ${NewTag}"
Write-Host "  2. Push commit + tag: git push origin HEAD; git push origin ${NewTag}"

$RemoteUrl = git remote get-url origin 2>$null
if ($RemoteUrl -match 'github\.com[:/](.+?)(?:\.git)?$') {
    Write-Host "  3. Monitor: https://github.com/$($Matches[1])/actions"
}

Write-Host ""
Write-Host "To undo (delete tag + revert commit):" -ForegroundColor Yellow
Write-Host "  git tag -d ${NewTag}; git reset --soft HEAD~1"

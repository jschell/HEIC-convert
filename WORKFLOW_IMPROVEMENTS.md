# Recommended CI/CD Workflow Improvements

This document contains specific, actionable improvements for the GitHub Actions workflows based on the build status analysis.

## 1. Enhanced CI Workflow with Caching and Security

```yaml
name: CI

on:
  push:
    branches: [ main, 'claude/**' ]
  pull_request:
    branches: [ main ]

jobs:
  build-and-test:
    runs-on: windows-latest
    timeout-minutes: 15  # Prevent hung builds

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET 8
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      # NEW: Add NuGet package caching
      - name: Cache NuGet packages
        uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}
          restore-keys: |
            ${{ runner.os }}-nuget-

      - name: Restore dependencies
        run: dotnet restore HEICAutoConverter.sln

      # NEW: Run static code analysis
      - name: Run code analysis
        run: dotnet build HEICAutoConverter.sln --configuration Release --no-restore /p:EnableNETAnalyzers=true /p:AnalysisLevel=latest

      - name: Build
        run: dotnet build HEICAutoConverter.sln --configuration Release --no-restore

      - name: Test
        run: dotnet test tests/HEICAutoConverter.Tests.csproj --configuration Release --no-build --verbosity normal --results-directory ./TestResults --logger "trx;LogFileName=test-results.trx"

      # NEW: Generate code coverage
      - name: Test with coverage
        run: dotnet test tests/HEICAutoConverter.Tests.csproj --configuration Release --no-build --collect:"XPlat Code Coverage" --results-directory ./TestResults

      - name: Upload test results
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: test-results
          path: ./TestResults/test-results.trx
          if-no-files-found: warn

      # NEW: Upload coverage reports
      - name: Upload coverage reports
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: coverage-reports
          path: ./TestResults/**/coverage.cobertura.xml
          if-no-files-found: ignore

  # NEW: Security scanning job
  security-scan:
    runs-on: windows-latest
    timeout-minutes: 10
    permissions:
      security-events: write

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET 8
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Restore dependencies
        run: dotnet restore HEICAutoConverter.sln

      # Run .NET security analysis
      - name: Run security analysis
        run: dotnet list package --vulnerable --include-transitive
        continue-on-error: true  # Don't fail build, just warn

      # Optional: Add Snyk or similar tool
      # - name: Run Snyk security scan
      #   uses: snyk/actions/dotnet@master
      #   env:
      #     SNYK_TOKEN: ${{ secrets.SNYK_TOKEN }}
      #   with:
      #     command: test
```

**Benefits:**
- ⚡ 30-60% faster builds with NuGet caching
- 🔒 Proactive vulnerability detection
- 📊 Code coverage tracking
- 🐛 Early detection of code quality issues
- ⏱️ Timeout prevents stuck builds

---

## 2. Enhanced Release Workflow with Code Signing

```yaml
name: Release

on:
  push:
    tags:
      - 'v*'

permissions:
  contents: write

jobs:
  # NEW: Pre-release validation
  validate:
    runs-on: windows-latest
    timeout-minutes: 10
    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET 8
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Restore dependencies
        run: dotnet restore HEICAutoConverter.sln

      - name: Run full test suite
        run: dotnet test tests/HEICAutoConverter.Tests.csproj --configuration Release --verbosity normal

      - name: Validate tag format
        run: |
          if ("${{ github.ref_name }}" -notmatch "^v\d+\.\d+\.\d+$") {
            Write-Error "Tag must be in format v#.#.#"
            exit 1
          }

  build-and-release:
    needs: validate
    runs-on: windows-latest
    timeout-minutes: 20

    # NEW: Build matrix for multiple architectures
    strategy:
      matrix:
        runtime: [win-x64, win-arm64]

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET 8
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Extract version from tag
        id: version
        shell: bash
        run: echo "VERSION=${GITHUB_REF_NAME#v}" >> "$GITHUB_OUTPUT"

      # NEW: Cache for faster release builds
      - name: Cache NuGet packages
        uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}

      - name: Restore dependencies
        run: dotnet restore HEICAutoConverter.sln

      - name: Publish self-contained executable
        run: >
          dotnet publish HEICAutoConverter.csproj
          --configuration Release
          --runtime ${{ matrix.runtime }}
          --self-contained true
          -p:PublishSingleFile=true
          -p:PublishTrimmed=true
          -p:Version=${{ steps.version.outputs.VERSION }}
          --output ./publish/${{ matrix.runtime }}

      # NEW: Code signing (requires certificate)
      # Uncomment when code signing certificate is available
      # - name: Sign executable
      #   run: |
      #     $cert = [Convert]::FromBase64String("${{ secrets.SIGNING_CERTIFICATE }}")
      #     [IO.File]::WriteAllBytes("cert.pfx", $cert)
      #     signtool sign /f cert.pfx /p "${{ secrets.CERT_PASSWORD }}" /t http://timestamp.digicert.com /fd SHA256 ./publish/${{ matrix.runtime }}/HEICAutoConverter.exe
      #     Remove-Item cert.pfx

      - name: Create zip archive
        shell: pwsh
        run: |
          $archiveName = "HEICAutoConverter-${{ github.ref_name }}-${{ matrix.runtime }}.zip"
          Compress-Archive -Path ./publish/${{ matrix.runtime }}/HEICAutoConverter.exe -DestinationPath ./publish/$archiveName

      - name: Generate SHA256 checksums
        shell: pwsh
        run: |
          $exeHash = (Get-FileHash ./publish/${{ matrix.runtime }}/HEICAutoConverter.exe -Algorithm SHA256).Hash
          $zipHash = (Get-FileHash ./publish/HEICAutoConverter-${{ github.ref_name }}-${{ matrix.runtime }}.zip -Algorithm SHA256).Hash

          $checksums = @"
          ``````
          SHA256 Checksums (${{ matrix.runtime }}):
          $exeHash  HEICAutoConverter.exe
          $zipHash  HEICAutoConverter-${{ github.ref_name }}-${{ matrix.runtime }}.zip
          ``````
          "@

          echo "CHECKSUMS<<EOF" >> $env:GITHUB_OUTPUT
          echo $checksums >> $env:GITHUB_OUTPUT
          echo "EOF" >> $env:GITHUB_OUTPUT
        id: checksums

      - name: Upload release artifacts
        uses: actions/upload-artifact@v4
        with:
          name: release-${{ matrix.runtime }}
          path: |
            ./publish/${{ matrix.runtime }}/HEICAutoConverter.exe
            ./publish/HEICAutoConverter-${{ github.ref_name }}-${{ matrix.runtime }}.zip

  # NEW: Create unified GitHub release
  create-release:
    needs: build-and-release
    runs-on: ubuntu-latest
    steps:
      - name: Download all artifacts
        uses: actions/download-artifact@v4
        with:
          path: ./artifacts

      - name: Create GitHub Release
        uses: softprops/action-gh-release@v2
        with:
          generate_release_notes: true
          body: |
            ## Downloads

            ### Windows x64 (Intel/AMD)
            | File | Description |
            |------|-------------|
            | `HEICAutoConverter.exe` (x64) | Portable executable for Intel/AMD processors |
            | `HEICAutoConverter-${{ github.ref_name }}-win-x64.zip` | Zipped portable executable (x64) |

            ### Windows ARM64
            | File | Description |
            |------|-------------|
            | `HEICAutoConverter.exe` (ARM64) | Portable executable for ARM64 processors (Surface Pro X, etc.) |
            | `HEICAutoConverter-${{ github.ref_name }}-win-arm64.zip` | Zipped portable executable (ARM64) |

            ## System Requirements
            - **Windows 10** (1809+) or **Windows 11**
            - No .NET runtime installation required (self-contained)

            ## Architecture Guide
            - **x64**: For most Windows PCs with Intel or AMD processors
            - **ARM64**: For Windows ARM devices (Surface Pro X, Snapdragon-based devices)

            See checksums in individual architecture folders.
          files: |
            ./artifacts/release-win-x64/win-x64/HEICAutoConverter.exe
            ./artifacts/release-win-x64/HEICAutoConverter-${{ github.ref_name }}-win-x64.zip
            ./artifacts/release-win-arm64/win-arm64/HEICAutoConverter.exe
            ./artifacts/release-win-arm64/HEICAutoConverter-${{ github.ref_name }}-win-arm64.zip
```

**Benefits:**
- ✅ Pre-release validation prevents bad releases
- 🏗️ Multi-architecture support (x64 + ARM64)
- 🔐 Code signing ready (when certificate available)
- ⚡ Faster builds with caching
- 🎯 Structured release artifacts

---

## 3. Quick Wins (Minimal Changes)

### Add to existing CI workflow (`ci.yml`):

```yaml
# Add after the jobs section, before steps
timeout-minutes: 15

# Add after Setup .NET step
- name: Cache NuGet packages
  uses: actions/cache@v4
  with:
    path: ~/.nuget/packages
    key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}
    restore-keys: |
      ${{ runner.os }}-nuget-
```

### Add to existing Release workflow (`release.yml`):

```yaml
# Add after the jobs section, before steps
timeout-minutes: 20

# Add after Setup .NET step
- name: Cache NuGet packages
  uses: actions/cache@v4
  with:
    path: ~/.nuget/packages
    key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}
```

---

## 4. Code Signing Setup (When Ready)

### Prerequisites:
1. Obtain a code signing certificate (DigiCert, Sectigo, etc.)
2. Export certificate as .pfx file with password
3. Convert to base64: `[Convert]::ToBase64String([IO.File]::ReadAllBytes("cert.pfx"))`
4. Add secrets to GitHub repository:
   - `SIGNING_CERTIFICATE`: Base64 encoded certificate
   - `CERT_PASSWORD`: Certificate password

### Usage in workflow:
```yaml
- name: Sign executable
  shell: pwsh
  run: |
    # Decode and save certificate
    $certBytes = [Convert]::FromBase64String("${{ secrets.SIGNING_CERTIFICATE }}")
    $certPath = Join-Path $env:TEMP "cert.pfx"
    [IO.File]::WriteAllBytes($certPath, $certBytes)

    # Sign the executable
    $signTool = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.22000.0\x64\signtool.exe"
    & $signTool sign /f $certPath /p "${{ secrets.CERT_PASSWORD }}" `
      /t http://timestamp.digicert.com `
      /fd SHA256 `
      /d "HEIC Auto Converter" `
      ./publish/HEICAutoConverter.exe

    # Clean up
    Remove-Item $certPath

    # Verify signature
    & $signTool verify /pa ./publish/HEICAutoConverter.exe
```

---

## 5. Dependency Scanning Setup

### Option A: Built-in .NET Security Check

Already available in all .NET 8 projects - just add to CI:

```yaml
- name: Check for vulnerable packages
  run: dotnet list package --vulnerable --include-transitive
  continue-on-error: true  # Warning only, doesn't fail build
```

### Option B: GitHub Dependabot

Create `.github/dependabot.yml`:

```yaml
version: 2
updates:
  - package-ecosystem: "nuget"
    directory: "/"
    schedule:
      interval: "weekly"
    open-pull-requests-limit: 5

  - package-ecosystem: "github-actions"
    directory: "/"
    schedule:
      interval: "monthly"
```

### Option C: Snyk (Requires Free Account)

```yaml
- name: Run Snyk security scan
  uses: snyk/actions/dotnet@master
  env:
    SNYK_TOKEN: ${{ secrets.SNYK_TOKEN }}
  with:
    command: test
    args: --severity-threshold=high
```

---

## 6. Implementation Priority

### Phase 1: Immediate (30 minutes)
- ✅ Add timeout-minutes to both workflows
- ✅ Add NuGet caching to both workflows
- ✅ Add `dotnet list package --vulnerable` to CI

### Phase 2: Short-term (2-4 hours)
- 📊 Add code coverage collection
- 🔍 Enable code analysis warnings
- 📝 Create dependabot.yml

### Phase 3: Medium-term (1-2 days)
- 🏗️ Add ARM64 build support
- ✅ Add pre-release validation job
- 🔒 Set up Snyk or similar security scanning

### Phase 4: Long-term (When resources available)
- 🔐 Obtain code signing certificate
- 🔐 Implement code signing in release workflow
- 🧪 Add integration and UI tests

---

## 7. Testing Improvements

### Add to test project (`HEICAutoConverter.Tests.csproj`):

```xml
<ItemGroup>
  <!-- Code coverage collection -->
  <PackageReference Include="coverlet.collector" Version="6.0.0">
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    <PrivateAssets>all</PrivateAssets>
  </PackageReference>

  <!-- For more detailed assertions -->
  <PackageReference Include="FluentAssertions" Version="6.12.0" />

  <!-- For mocking complex dependencies -->
  <PackageReference Include="Moq" Version="4.20.70" />
</ItemGroup>
```

---

## 8. Monitoring and Alerts

### GitHub Actions Status Badge

Add to README.md:

```markdown
[![CI](https://github.com/jschell/HEIC-convert/actions/workflows/ci.yml/badge.svg)](https://github.com/jschell/HEIC-convert/actions/workflows/ci.yml)
[![Release](https://github.com/jschell/HEIC-convert/actions/workflows/release.yml/badge.svg)](https://github.com/jschell/HEIC-convert/actions/workflows/release.yml)
```

### Workflow Notification Setup

Add to workflow for critical failures:

```yaml
- name: Notify on failure
  if: failure()
  uses: actions/github-script@v7
  with:
    script: |
      github.rest.issues.create({
        owner: context.repo.owner,
        repo: context.repo.repo,
        title: `CI Build Failed - ${context.sha.substring(0, 7)}`,
        body: `Build failed on ${context.ref}\n\nWorkflow: ${context.workflow}\nRun: ${context.runId}`,
        labels: ['bug', 'ci-failure']
      })
```

---

*Recommendations compiled: 2026-02-08*
*Based on: BUILD_STATUS_ANALYSIS.md*

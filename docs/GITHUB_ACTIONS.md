# GitHub Actions for Partner Admin Link Tool

This repository uses GitHub Actions to automatically build, test, and create releases of the Partner Admin Link Tool. The CI/CD pipeline ensures code quality and provides automated release generation for multiple platforms.

## 🔄 Workflows Overview

### 1. CI/CD Pipeline (`ci.yml`)

**Triggers:**
- Push to `main`, `master`, or `develop` branches
- Pull requests to `main` or `master` branches

**What it does:**
- ✅ Builds the entire solution
- ✅ Runs all unit tests
- ✅ Performs code quality analysis
- ✅ Uploads test results as artifacts

### 2. Automated Release (`release.yml`)

**Triggers:**
- When you push a version tag (e.g., `v1.0.0`)
- When a GitHub release is published

**What it does:**
- 🔨 Builds self-contained executables for multiple platforms:
  - Windows (x64, x86, ARM64)
  - Linux (x64, ARM64)
  - macOS (x64, ARM64)
- 📦 Creates platform-specific packages (ZIP for Windows, TAR.GZ for Linux/macOS)
- 📋 Generates comprehensive release notes
- 🚀 Automatically creates a GitHub release with all artifacts

### 3. Manual Release (`manual-release.yml`)

**Triggers:**
- Manual execution via GitHub Actions UI

**What it does:**
- Same as automated release but allows you to:
  - Specify custom version numbers
  - Mark releases as pre-release
  - Create releases without pushing tags

## 🚀 How to Create a Release

### Option 1: Automatic Release (Recommended)

1. **Create and push a version tag:**
   ```bash
   git tag v1.0.0
   git push origin v1.0.0
   ```

2. **The workflow will automatically:**
   - Build all platform versions
   - Run tests to ensure quality
   - Create a GitHub release
   - Upload all executable packages

### Option 2: Manual Release

1. **Go to your repository on GitHub**
2. **Click "Actions" tab**
3. **Select "Manual Release" workflow**
4. **Click "Run workflow"**
5. **Specify:**
   - Version (e.g., `v1.0.0`)
   - Whether it's a pre-release

### Option 3: GitHub Release UI

1. **Go to "Releases" in your repository**
2. **Click "Create a new release"**
3. **Create a new tag (e.g., `v1.0.0`)**
4. **Publish the release**
5. **The release workflow will automatically build and attach artifacts**

## 📦 Release Artifacts

Each release includes executables for:

| Platform | Architecture | File Name | File Type |
|----------|-------------|-----------|-----------|
| Windows | x64 | `PartnerAdminLinkTool-win-x64.zip` | ZIP |
| Windows | x86 | `PartnerAdminLinkTool-win-x86.zip` | ZIP |
| Windows | ARM64 | `PartnerAdminLinkTool-win-arm64.zip` | ZIP |
| Linux | x64 | `PartnerAdminLinkTool-linux-x64.tar.gz` | TAR.GZ |
| Linux | ARM64 | `PartnerAdminLinkTool-linux-arm64.tar.gz` | TAR.GZ |
| macOS | x64 (Intel) | `PartnerAdminLinkTool-osx-x64.tar.gz` | TAR.GZ |
| macOS | ARM64 (Apple Silicon) | `PartnerAdminLinkTool-osx-arm64.tar.gz` | TAR.GZ |

### What's Included in Each Package

- ✅ Self-contained executable (no .NET runtime required)
- ✅ README.md with usage instructions
- ✅ APP_REGISTRATION_SETUP.md with Azure setup guide
- ✅ All necessary dependencies

## 🔧 Workflow Configuration

### Environment Variables

- `DOTNET_VERSION`: .NET version used for builds (currently 8.0.x)
- `PROJECT_PATH`: Path to the main executable project

### Build Configuration

- **Configuration**: Release
- **Self-contained**: Yes (includes .NET runtime)
- **Single file**: Yes (creates a single executable)
- **Trimmed**: No (keeps full framework for compatibility)
- **Ready to Run**: No (optimizes for compatibility over startup speed)

## 🛠️ Customizing the Workflows

### Adding New Target Platforms

To add a new target platform, modify the `matrix` section in the release workflows:

```yaml
matrix:
  target: 
    - your-new-target
  include:
    - target: your-new-target
      artifact_name: PartnerAdminLinkTool-your-new-target
      asset_name: PartnerAdminLinkTool-your-new-target.zip
```

### Modifying Build Settings

Update the `dotnet publish` command in the workflows to change build settings:

```yaml
dotnet publish ${{ env.PROJECT_PATH }} \
  --configuration Release \
  --runtime ${{ matrix.target }} \
  --self-contained true \
  /p:PublishSingleFile=true \
  /p:PublishTrimmed=false
```

### Changing Trigger Conditions

Modify the `on:` section to change when workflows run:

```yaml
on:
  push:
    branches: [ main, develop ]  # Add or remove branches
    tags: [ 'v*.*.*' ]          # Modify tag pattern
```

## 📊 Monitoring and Debugging

### Viewing Workflow Runs

1. Go to the "Actions" tab in your repository
2. Click on any workflow run to see details
3. Click on individual jobs to see logs

### Common Issues and Solutions

| Issue | Solution |
|-------|----------|
| Build fails | Check the build logs, ensure all dependencies are properly referenced |
| Tests fail | Review test output in the artifacts, fix failing tests |
| Release not created | Ensure tag follows pattern `v*.*.*` and you have push permissions |
| Artifacts missing | Check if all matrix jobs completed successfully |

### Debug Mode

Add this step to any workflow for debugging:

```yaml
- name: Debug Information
  run: |
    echo "GitHub Event: ${{ github.event_name }}"
    echo "GitHub Ref: ${{ github.ref }}"
    echo "Matrix Target: ${{ matrix.target }}"
    pwd
    ls -la
```

## 🔒 Security and Permissions

### Required Permissions

The workflows require these permissions (automatically granted for repository actions):

- `contents: read` - To checkout code
- `contents: write` - To create releases
- `actions: read` - To access workflow artifacts

### Secrets Used

- `GITHUB_TOKEN` - Automatically provided by GitHub for release creation

### Security Best Practices

- ✅ Workflows only run on specific triggers
- ✅ No sensitive information in workflow files
- ✅ Uses official GitHub Actions only
- ✅ Minimal required permissions

## 🚦 Status Badges

Add these badges to your README to show build status:

```markdown
![CI/CD Pipeline](https://github.com/yourusername/yourrepo/workflows/CI%2FCD%20Pipeline/badge.svg)
![Release](https://github.com/yourusername/yourrepo/workflows/Release/badge.svg)
```

## 🔄 Version Management

### Semantic Versioning

Follow semantic versioning for tags:
- `v1.0.0` - Major release
- `v1.1.0` - Minor release (new features)
- `v1.0.1` - Patch release (bug fixes)

### Pre-releases

Create pre-release versions:
- `v1.0.0-alpha.1` - Alpha version
- `v1.0.0-beta.1` - Beta version
- `v1.0.0-rc.1` - Release candidate

## 📈 Performance Optimization

### Caching

The workflows use caching to speed up builds:
- NuGet packages are cached automatically
- .NET SDK setup uses caching

### Parallel Execution

Multiple platform builds run in parallel to reduce total build time.

### Artifact Retention

- Test results: 7 days
- Release artifacts: 30 days
- GitHub releases: Permanent (until manually deleted)

---

**Need help?** Check the [GitHub Actions documentation](https://docs.github.com/en/actions) or review the workflow logs for detailed error information.

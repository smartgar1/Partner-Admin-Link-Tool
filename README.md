# 🌐 Partner Admin Link (PAL) Tool

A modern .NET application that helps Microsoft partners link their Partner ID (Associated PartnerID) to all accessible Microsoft Entra tenants, enabling proper partner recognition and revenue attribution.

## 🎯 What This Tool Does

**Partner Admin Link (PAL)** enables Microsoft to identify and recognize partners who drive Azure customer success. By linking your Microsoft AI Cloud Partner Program (Associated PartnerID) to customer tenants, Microsoft can:

- ✅ **Attribute revenue** from your customers' Azure consumption to your organization
- ✅ **Recognize your impact** on customer success and business outcomes  
- ✅ **Provide insights** into your partner engagement effectiveness
- ✅ **Enable partner incentives** based on customer growth

## 🚀 Features

- **🔐 Multiple Authentication Methods**: Support for both Interactive and Device Code Flow authentication
- **🔍 Automatic Tenant Discovery**: Finds all Microsoft Entra tenants you have access to
- **🔗 Bulk Partner ID Linking**: Link your Partner ID to multiple tenants at once
- **📊 Progress Tracking**: Real-time progress feedback during bulk operations
- **🎨 Modern Console UI**: Beautiful text-based interface using Spectre.Console
- **📱 Cross-Platform**: Runs on Windows, macOS, and Linux
- **☁️ GitHub Codespaces Ready**: Full .devcontainer support for cloud development

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Partner Admin Link Tool                 │
├─────────────────────────────────────────────────────────────┤
│  Console UI Layer (PartnerAdminLinkTool.UI)               │
│  ├─ Spectre.Console for beautiful terminal UI             │
│  ├─ Interactive menus and progress bars                   │
│  └─ Device Code Flow & Interactive Auth support           │
├─────────────────────────────────────────────────────────────┤
│  Core Business Logic (PartnerAdminLinkTool.Core)          │
│  ├─ Authentication Service (MSAL.NET)                     │
│  ├─ Tenant Discovery Service (Azure Management API)       │
│  ├─ Partner Link Service (Azure Management API)           │
│  └─ Clean, testable, well-documented code                 │
├─────────────────────────────────────────────────────────────┤
│  External APIs                                            │
│  ├─ Azure Management API (tenant discovery & PAL linking) │
│  └─ Microsoft Identity Platform (authentication)          │
│  Note: Microsoft Graph removed to avoid consent issues    │
└─────────────────────────────────────────────────────────────┘
```

## 🛠️ Prerequisites

- **.NET 8.0 SDK** or later
- **Microsoft AI Cloud Partner Program (Associated PartnerID)**
- **Access to customer tenants** (as guest user or member)
- **Appropriate permissions** in customer tenants (Contributor or higher)

## 🏃‍♂️ Quick Start

### Option 1: Run Locally

```bash
# Clone the repository
git clone https://github.com/lukemurraynz/Partner-Admin-Link-PAL-Tool.git
cd Partner-Admin-Link-PAL-Tool

# Restore dependencies and build
dotnet restore
dotnet build

# Run the application
cd src/PartnerAdminLinkTool.UI
dotnet run
```

### Option 2: Use GitHub Codespaces

1. Click the **"Code"** button on this repository
2. Select **"Codespaces"** tab
3. Click **"Create codespace on main"**
4. Wait for the environment to set up automatically
5. Run `dotnet run` in the terminal

## 🚫 Microsoft Graph Dependency Removed

**Important**: This tool previously used Microsoft Graph for enhanced tenant discovery, but this dependency has been **completely removed** to address consent issues in organizations that block Microsoft Graph APIs.

### Why Was Microsoft Graph Removed?

- **Consent Issues**: Many organizations block `Directory.Read.All` and `ManagedTenants.Read.All` permissions
- **Not Essential**: Microsoft Graph was only used for friendly tenant names and domains
- **PAL Still Works**: Core PAL functionality requires only Azure Management API

### What Changed?

- ✅ **PAL linking/unlinking**: Still works perfectly (uses Azure Management API)
- ✅ **Tenant discovery**: Still works perfectly (uses Azure Management API)
- ❌ **Friendly tenant names**: Now shows tenant IDs instead of organization names
- ✅ **No consent issues**: Only requires `https://management.azure.com/user_impersonation`

### For Organizations That Block Microsoft Graph

This tool is now **perfect** for organizations with strict Microsoft Graph policies, as it requires only:
- Azure Management API access (standard for Azure administration)
- Basic authentication permissions (which most users already have)

````
```

### Option 2: Use GitHub Codespaces

1. Click the **"Code"** button on this repository
2. Select **"Codespaces"** tab
3. Click **"Create codespace on main"**
4. Wait for the environment to set up automatically
5. Run `dotnet run` in the terminal

## 📖 How to Use

### 1. Authentication

When you first run the tool, you'll need to authenticate:

**Interactive Authentication** (recommended for desktop):
- Opens a browser window for sign-in
- Faster and more user-friendly

**Device Code Authentication** (for servers/restricted environments):
- Displays a code and URL
- Sign in on another device with the code

### 2. Discover Tenants

The tool automatically discovers all Microsoft Entra tenants you have access to, showing:
- Tenant name and domain
- Your access level (guest/member)
- Current Partner ID links (if any)

### 3. Link Partner ID

Enter your Microsoft AI Cloud Partner Program (Associated PartnerID) and the tool will:
- Validate the ID format
- Link it to all accessible tenants
- Show real-time progress
- Report success/failure for each tenant

## � Pre-built Releases

### Download Ready-to-Use Executables

Visit the [Releases page](../../releases) to download pre-built executables for your platform:

- **Windows**: `PartnerAdminLinkTool-win-x64.zip` (64-bit), `PartnerAdminLinkTool-win-x86.zip` (32-bit), `PartnerAdminLinkTool-win-arm64.zip` (ARM64)
- **Linux**: `PartnerAdminLinkTool-linux-x64.tar.gz` (64-bit), `PartnerAdminLinkTool-linux-arm64.tar.gz` (ARM64)  
- **macOS**: `PartnerAdminLinkTool-osx-x64.tar.gz` (Intel), `PartnerAdminLinkTool-osx-arm64.tar.gz` (Apple Silicon)

**Advantages of pre-built releases:**
- ✅ No .NET SDK installation required
- ✅ Self-contained executables
- ✅ Automatic updates via GitHub releases
- ✅ Multiple platform support

### Installation from Release

1. Download the appropriate package for your platform
2. Extract the archive to a folder
3. Follow the setup instructions in `APP_REGISTRATION_SETUP.md`
4. Run the executable to start the tool

## 🚀 Automated Releases

This repository uses **GitHub Actions** for automated building and releasing:

- **🔄 Continuous Integration**: Automatic building and testing on every commit
- **📦 Multi-Platform Builds**: Automatic creation of executables for Windows, Linux, and macOS
- **🏷️ Tag-Based Releases**: Create a release by pushing a version tag (e.g., `v1.0.0`)
- **📋 Release Notes**: Automatically generated comprehensive release notes

### For Maintainers: Creating a Release

```bash
# Create and push a version tag
git tag v1.0.0
git push origin v1.0.0

# Or create a manual release via GitHub Actions UI
```

See [GitHub Actions Documentation](docs/GITHUB_ACTIONS.md) for detailed information about the CI/CD pipeline.

## �🔧 Configuration

### appsettings.json

```json
{
  "Authentication": {
    "ClientId": "14d82eec-204b-4c2f-b7e8-296a70dab67e",
    "Authority": "https://login.microsoftonline.com/organizations",
    "RedirectUri": "http://localhost"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

### Environment Variables

You can also configure using environment variables:
- `Authentication__ClientId`: Azure AD application client ID
- `Authentication__Authority`: Authentication authority URL (should be `https://login.microsoftonline.com/organizations` for Azure Resource Manager compatibility)

## 🧪 Development

### Project Structure

```
src/
├── PartnerAdminLinkTool.Core/     # Core business logic
│   ├── Models/                    # Data models
│   ├── Services/                  # Business services
│   └── Extensions/                # Utility extensions
├── PartnerAdminLinkTool.UI/       # Console application
│   ├── Services/                  # UI services
│   └── Program.cs                 # Application entry point
tests/
├── PartnerAdminLinkTool.Tests/    # Unit tests
docs/                              # Additional documentation
.devcontainer/                     # GitHub Codespaces config
```

### Running Tests

```bash
dotnet test
```

### Code Style

This project follows Microsoft's coding conventions and includes extensive documentation for beginners:

- **Clean Architecture**: Separation of concerns with Core and UI layers
- **Dependency Injection**: All services are registered and injected
- **Async/Await**: Proper asynchronous programming patterns
- **Error Handling**: Comprehensive exception handling and logging
- **Documentation**: Every class and method is fully documented

## 🔐 Security

- **No credentials stored**: Uses MSAL.NET token cache securely
- **Least privilege**: Only requests necessary API permissions
- **Secure communication**: All API calls use HTTPS
- **No third-party dependencies**: Direct REST API calls to Microsoft services

## 📚 Learning Resources

### For .NET Beginners

- [Microsoft Learn: .NET Fundamentals](https://docs.microsoft.com/learn/paths/dotnet-fundamentals/)
- [C# Programming Guide](https://docs.microsoft.com/dotnet/csharp/programming-guide/)
- [Dependency Injection in .NET](https://docs.microsoft.com/dotnet/core/extensions/dependency-injection)

### For Microsoft Partners

- [Partner Admin Link Documentation](https://docs.microsoft.com/partner-center/link-partner-id-for-azure-performance-pal-dpor)
- [Microsoft AI Cloud Partner Program](https://partner.microsoft.com/)
- [Azure Partner Recognition](https://docs.microsoft.com/azure/cost-management-billing/manage/link-partner-id)

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 Support

- **Documentation**: Check this README and inline code comments
- **Issues**: Create an issue on GitHub for bugs or feature requests
- **Discussions**: Use GitHub Discussions for questions and community support

## ⚠️ Important Notes

- **PAL Association**: Only adds your Partner ID to existing credentials
- **No Permission Changes**: Does not alter user permissions or access
- **Revenue Attribution**: May take 24-48 hours to appear in partner reports
- **Customer Consent**: Ensure you have proper agreements with customers before linking

---

**Built with ❤️ for the Microsoft Partner Community**
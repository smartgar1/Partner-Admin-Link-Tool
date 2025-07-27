# Contributing to Partner Admin Link Tool

Thank you for your interest in contributing to the Partner Admin Link Tool! This document provides guidelines and information for contributors.

## 🏗️ Development Setup

### Prerequisites

- .NET 8.0 SDK or later
- Git
- Visual Studio Code (recommended) or Visual Studio

### Getting Started

1. **Fork and Clone**
   ```bash
   git clone https://github.com/yourusername/Partner-Admin-Link-PAL-Tool.git
   cd Partner-Admin-Link-PAL-Tool
   ```

2. **Restore Dependencies**
   ```bash
   dotnet restore
   ```

3. **Build the Solution**
   ```bash
   dotnet build
   ```

4. **Run Tests**
   ```bash
   dotnet test
   ```

### Using GitHub Codespaces

For the easiest development experience:

1. Click "Code" → "Codespaces" → "Create codespace on main"
2. Wait for the environment to initialize
3. All dependencies and tools are pre-configured!

## 📁 Project Structure

```
src/
├── PartnerAdminLinkTool.Core/        # Core business logic
│   ├── Models/                       # Data models and DTOs
│   ├── Services/                     # Business services and interfaces
│   └── Extensions/                   # Utility extensions
├── PartnerAdminLinkTool.UI/          # Console application
│   ├── Services/                     # UI-specific services
│   └── Program.cs                    # Application entry point
tests/
├── PartnerAdminLinkTool.Tests/       # Unit tests
docs/                                 # Documentation
.devcontainer/                        # GitHub Codespaces configuration
```

## 🎯 Coding Standards

### Code Style

- Follow [Microsoft C# Coding Conventions](https://docs.microsoft.com/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use PascalCase for public members, camelCase for private members
- Include XML documentation comments for all public APIs
- Use meaningful variable and method names

### Documentation

**Every class and public method must include:**
- XML documentation comments
- Purpose and behavior description
- Parameter descriptions
- Return value descriptions
- Usage examples for complex methods

**Example:**
```csharp
/// <summary>
/// Links a Partner ID to a specific Microsoft Entra tenant.
/// 
/// For beginners: This method creates the association between your 
/// AI Cloud Partner Program ID and a customer's tenant, enabling Microsoft 
/// to track your involvement with that customer's Azure consumption.
/// </summary>
/// <param name="partnerId">The Microsoft AI Cloud Partner Program (Associated PartnerID)</param>
/// <param name="tenant">The target tenant to link to</param>
/// <returns>Result indicating success or failure with details</returns>
public async Task<PartnerLinkResult> LinkPartnerIdAsync(string partnerId, Tenant tenant)
```

### Architecture Principles

1. **Separation of Concerns**: Core business logic in `Core` project, UI logic in `UI` project
2. **Dependency Injection**: Use DI container for all dependencies
3. **Interface-Based Design**: Program against interfaces, not implementations
4. **Async/Await**: Use async patterns for all I/O operations
5. **Error Handling**: Comprehensive exception handling with logging
6. **Testability**: Design code to be easily unit testable

## 🧪 Testing Guidelines

### Unit Tests

- Write tests for all business logic in the Core project
- Use descriptive test method names: `Method_Scenario_ExpectedBehavior`
- Follow the Arrange-Act-Assert pattern
- Mock external dependencies using interfaces

### Test Structure

```csharp
[Fact]
public async Task LinkPartnerIdAsync_ValidInput_ReturnsSuccessResult()
{
    // Arrange
    var mockAuth = new Mock<IAuthenticationService>();
    var service = new PartnerLinkService(mockLogger, mockAuth.Object);
    var tenant = new Tenant { Id = "test-id", DisplayName = "Test Tenant" };
    
    // Act
    var result = await service.LinkPartnerIdAsync("1234567", tenant);
    
    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal("1234567", result.PartnerId);
}
```

## 🔄 Pull Request Process

### Before Submitting

1. **Run Tests**: Ensure all tests pass
   ```bash
   dotnet test
   ```

2. **Check Build**: Verify solution builds without warnings
   ```bash
   dotnet build
   ```

3. **Update Documentation**: Update README.md or add documentation as needed

4. **Follow Commit Convention**: Use clear, descriptive commit messages
   ```
   feat: add support for device code authentication
   fix: resolve null reference in tenant discovery
   docs: update setup instructions for Codespaces
   ```

### Pull Request Template

When creating a PR, include:

- **Description**: What does this PR do?
- **Changes**: List of specific changes made
- **Testing**: How was this tested?
- **Screenshots**: For UI changes (if applicable)
- **Breaking Changes**: Any breaking changes?

### Review Process

1. Automated checks must pass (build, tests)
2. Code review by maintainers
3. Address feedback and update as needed
4. Approval and merge by maintainers

## 🐛 Issue Guidelines

### Bug Reports

Include:
- Steps to reproduce
- Expected vs actual behavior
- Environment details (.NET version, OS)
- Error messages or stack traces
- Minimal code example if applicable

### Feature Requests

Include:
- Clear description of the feature
- Use case and business value
- Proposed implementation approach
- Any breaking changes considerations

## 🎓 Learning Resources

### For New Contributors

- [Git Basics](https://git-scm.com/book/en/v2/Getting-Started-Git-Basics)
- [.NET Development](https://docs.microsoft.com/dotnet/core/)
- [Microsoft Graph API](https://docs.microsoft.com/graph/)
- [Azure Management API](https://docs.microsoft.com/rest/api/azure/)

### For Partner Admin Link

- [PAL Overview](https://docs.microsoft.com/partner-center/link-partner-id-for-azure-performance-pal-dpor)
- [Partner Recognition](https://docs.microsoft.com/azure/cost-management-billing/manage/link-partner-id)

## 📞 Getting Help

- **GitHub Discussions**: Ask questions and get community help
- **Issues**: Report bugs or request features
- **Documentation**: Check the README and inline code comments

## 🙏 Recognition

Contributors will be:
- Listed in the project contributors
- Acknowledged in release notes for significant contributions
- Invited to help maintain the project (for regular contributors)

Thank you for helping make this tool better for the Microsoft Partner community!
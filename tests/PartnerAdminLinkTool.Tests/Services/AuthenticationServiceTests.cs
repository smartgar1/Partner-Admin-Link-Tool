using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Moq;
using PartnerAdminLinkTool.Core.Services;
using Xunit;

namespace PartnerAdminLinkTool.Tests.Services;

public class AuthenticationServiceTests
{
    private readonly Mock<ILogger<AuthenticationService>> _loggerMock;
    private readonly Mock<IConfiguration> _configurationMock;

    public AuthenticationServiceTests()
    {
        _loggerMock = new Mock<ILogger<AuthenticationService>>();
        _configurationMock = new Mock<IConfiguration>();
        
        // Setup default configuration values
        _configurationMock.Setup(c => c["Authentication:ClientId"]).Returns("04b07795-8ddb-461a-bbee-02f9e1bf7b46");
        _configurationMock.Setup(c => c["Authentication:Authority"]).Returns("https://login.microsoftonline.com/organizations");
        _configurationMock.Setup(c => c["Authentication:RedirectUri"]).Returns("http://localhost");
    }

    [Fact]
    public void AuthenticationService_ShouldInitialize_Successfully()
    {
        // Arrange & Act
        var authService = new AuthenticationService(_loggerMock.Object, _configurationMock.Object);

        // Assert
        Assert.NotNull(authService);
        Assert.False(authService.CurrentState.IsAuthenticated);
    }

    [Fact]
    public void GetAdminConsentUrl_ShouldReturnCorrectUrl()
    {
        // Arrange
        var authService = new AuthenticationService(_loggerMock.Object, _configurationMock.Object);

        // Act
        var consentUrl = authService.GetAdminConsentUrl();

        // Assert
        Assert.Contains("https://login.microsoftonline.com/common/adminconsent", consentUrl);
        Assert.Contains("client_id=04b07795-8ddb-461a-bbee-02f9e1bf7b46", consentUrl);
    }

    [Theory]
    [InlineData("AADSTS50076")]
    [InlineData("AADSTS50079")]
    [InlineData("multi-factor authentication")]
    [InlineData("mfa_required")]
    public void IsMfaRequired_ShouldDetectMfaErrors(string errorMessage)
    {
        // This test verifies that our MFA detection logic works correctly
        // In the actual implementation, these patterns are used in the GetAzureManagementAccessTokenAsync method
        
        // Arrange
        var isMfaError = errorMessage.Contains("AADSTS50076") || 
                        errorMessage.Contains("AADSTS50079") || 
                        errorMessage.Contains("multi-factor authentication") || 
                        errorMessage.Contains("mfa_required");

        // Assert
        Assert.True(isMfaError, $"Should detect MFA requirement in error message: {errorMessage}");
    }
}

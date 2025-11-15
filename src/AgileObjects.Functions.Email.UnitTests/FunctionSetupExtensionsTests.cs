using AgileObjects.Functions.Email.Configuration;
using AgileObjects.Tezdi;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AgileObjects.Functions.Email.UnitTests;

[TestClass]
public sealed class FunctionSetupExtensionsTests
{
    [TestMethod]
    public void DefaultConfiguration_ConfiguresSettings()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .Add(
                ("SmtpHost", "mail.test.com"),
                ("SmtpUsername", "username@test.com"),
                ("SmtpPassword", "P4ssW0rd!"),
                ("Recipient", "to@test.com"),
                ("IsSubjectRequired", false),
                ("FallbackSubject", "Email received!"),
                ("UseRedirectResponse", false),
                ("AllowUserRedirectUrls", false),
                ("SuccessRedirectUrl", string.Empty))
            .Build();

        var serviceProvider = new ServiceCollection()
            .AddSendEmailFunction(configuration)
            .BuildServiceProvider();

        // Act
        var settings = serviceProvider
            .GetService<IOptions<FunctionSettings>>()?
            .Value;

        // Assert
        settings.Should().NotBeNull();
        settings.Recipient.Should().Be("to@test.com");
        settings.IsSubjectRequired.Should().BeFalse();
        settings.FallbackSubject.Should().Be("Email received!");
        settings.UseRedirectResponse.Should().BeFalse();
        settings.UseOkResponse.Should().BeTrue();
        settings.AllowUserRedirectUrls.Should().BeFalse();
        settings.HasNoSuccessRedirectUrl.Should().BeTrue();
        settings.SuccessRedirectUrl.Should().BeEmpty();
    }

    [TestMethod]
    public void SubjectRequiredConfiguration_ConfiguresSettings()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .Add(
                ("SmtpHost", "mail.test.com"),
                ("SmtpUsername", "username@test.com"),
                ("SmtpPassword", "P4ssW0rd!"),
                ("Recipient", "to@test.com"),
                ("IsSubjectRequired", true),
                ("FallbackSubject", "Email received!"),
                ("UseRedirectResponse", false),
                ("AllowUserRedirectUrls", false),
                ("SuccessRedirectUrl", default(string)))
            .Build();

        var serviceProvider = new ServiceCollection()
            .AddSendEmailFunction(configuration)
            .BuildServiceProvider();

        // Act
        var settings = serviceProvider
            .GetService<IOptions<FunctionSettings>>()?
            .Value;

        // Assert
        settings.Should().NotBeNull();
        settings.Recipient.Should().Be("to@test.com");
        settings.IsSubjectRequired.Should().BeTrue();
    }

    [TestMethod]
    public void RedirectResponseConfiguration_ConfiguresSettings()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .Add(
                ("SmtpHost", "mail.test.com"),
                ("SmtpUsername", "username@test.com"),
                ("SmtpPassword", "P4ssW0rd!"),
                ("Recipient", "to@test.com"),
                ("IsSubjectRequired", false),
                ("FallbackSubject", "Email received!"),
                ("UseRedirectResponse", true),
                ("AllowUserRedirectUrls", true),
                ("SuccessRedirectUrl", "https://email-sent.com"))
            .Build();

        var serviceProvider = new ServiceCollection()
            .AddSendEmailFunction(configuration)
            .BuildServiceProvider();

        // Act
        var settings = serviceProvider
            .GetService<IOptions<FunctionSettings>>()?
            .Value;

        // Assert
        settings.Should().NotBeNull();
        settings.Recipient.Should().Be("to@test.com");
        settings.IsSubjectRequired.Should().BeFalse();
        settings.FallbackSubject.Should().Be("Email received!");
        settings.UseRedirectResponse.Should().BeTrue();
        settings.UseOkResponse.Should().BeFalse();
        settings.AllowUserRedirectUrls.Should().BeTrue();
        settings.HasNoSuccessRedirectUrl.Should().BeFalse();
        settings.SuccessRedirectUrl.Should().Be("https://email-sent.com");
    }

    [TestMethod]
    public void MinimalConfiguration_ConfiguresSettings()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .Add(
                ("SmtpHost", "mail.test.com"),
                ("SmtpUsername", "username@test.com"),
                ("SmtpPassword", "P4ssW0rd!"),
                ("Recipient", "to@test.com"),
                ("IsSubjectRequired", default(string)),
                ("FallbackSubject", default(string)),
                ("UseRedirectResponse", default(string)),
                ("AllowUserRedirectUrls", default(string)),
                ("SuccessRedirectUrl", default(string)))
            .Build();

        var serviceProvider = new ServiceCollection()
            .AddSendEmailFunction(configuration)
            .BuildServiceProvider();

        // Act
        var settings = serviceProvider
            .GetService<IOptions<FunctionSettings>>()?
            .Value;

        // Assert
        settings.Should().NotBeNull();
        settings.Recipient.Should().Be("to@test.com");
        settings.IsSubjectRequired.Should().BeFalse();
        settings.FallbackSubject.Should().Be("Email received");
        settings.UseRedirectResponse.Should().BeFalse();
        settings.UseOkResponse.Should().BeTrue();
        settings.AllowUserRedirectUrls.Should().BeFalse();
        settings.HasNoSuccessRedirectUrl.Should().BeTrue();
        settings.SuccessRedirectUrl.Should().BeNull();
    }
}
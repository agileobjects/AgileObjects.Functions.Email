using AgileObjects.Functions.Email.Configuration;
using AgileObjects.Functions.Email.Http;
using AgileObjects.Tezdi;
using FluentAssertions;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using MimeKit;
using Moq;
using static System.Net.HttpStatusCode;

namespace AgileObjects.Functions.Email.UnitTests;

[TestClass]
public sealed class SendEmailFunctionTests :
    TezdiAzureAspNetCoreFunctionTestClass<SendEmailFunction>
{
    [TestMethod]
    public Task ReadFormDataErrors_ReturnsBadRequest()
    {
        return RunAsync(async context =>
        {
            // Arrange
            context.Mock<IRequestReader>(mockReader =>
            {
                mockReader
                    .Setup(rdr => rdr.ReadFormAsync(
                        It.IsAny<HttpRequest>(),
                        It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new InvalidOperationException());
            });

            // Act
            var response = await context
                .TriggerFunctionPostAsync("api/SendEmail")
                .ConfigureAwait(false);

            // Assert
            await response.Should()
                .HaveStatusCode(BadRequest).And
                .HaveContentAsync("Invalid form data.")
                .ConfigureAwait(false);
        });
    }

    [TestMethod]
    [DataRow(null, "test@test.com", "Test message!")]
    [DataRow("Captain Test", null, "Test message!")]
    [DataRow("Captain Test", "test@test.com", null)]
    public Task MissingEmailDetails_ReturnsBadRequest(
        string? name,
        string? email,
        string? message)
    {
        return RunAsync(async context =>
        {
            // Arrange
            using var httpRequest =
                CreateHttpRequest(name, email, message);

            // Act
            var response = await context
                .TriggerFunctionAsync(httpRequest)
                .ConfigureAwait(false);

            // Assert
            await response.Should()
                .HaveStatusCode(BadRequest).And
                .HaveContentAsync("Missing email details.")
                .ConfigureAwait(false);
        });
    }

    [TestMethod]
    [DataRow("", "test@test.com", "Test message!")]
    [DataRow("Captain Test", "", "Test message!")]
    [DataRow("Captain Test", "test@test.com", "")]
    public Task EmptyEmailDetails_ReturnsBadRequest(
        string name,
        string email,
        string message)
    {
        return RunAsync(async context =>
        {
            // Arrange
            using var httpRequest =
                CreateHttpRequest(name, email, message);

            // Act
            var response = await context
                .TriggerFunctionAsync(httpRequest)
                .ConfigureAwait(false);

            // Assert
            await response.Should()
                .HaveStatusCode(BadRequest).And
                .HaveContentAsync("Missing email details.")
                .ConfigureAwait(false);
        });
    }

    [TestMethod]
    public Task InvalidEmail_ReturnsBadRequest()
    {
        return RunAsync(async context =>
        {
            // Arrange
            using var httpRequest = CreateHttpRequest(
                name: "Captain Test",
                email: "not a valid email",
                message: "Hello?!");

            // Act
            var response = await context
                .TriggerFunctionAsync(httpRequest)
                .ConfigureAwait(false);

            // Assert
            await response.Should()
                .HaveStatusCode(BadRequest).And
                .HaveContentAsync("Invalid from email address 'not a valid email'.")
                .ConfigureAwait(false);
        });
    }

    [TestMethod]
    public Task SubjectRequired_MissingSubject_ReturnsBadRequest()
    {
        return RunAsync(async context =>
        {
            // Arrange
            context.StubConfiguration(new FunctionSettings
            {
                IsSubjectRequired = true
            });

            using var httpRequest = CreateHttpRequest(
                name: "Captain Test",
                email: "test@test.com",
                message: "Hello?!");

            // Act
            var response = await context
                .TriggerFunctionAsync(httpRequest)
                .ConfigureAwait(false);

            // Assert
            await response.Should()
                .HaveStatusCode(BadRequest).And
                .HaveContentAsync("Missing email details.")
                .ConfigureAwait(false);
        });
    }

    [TestMethod]
    public Task SubjectRequired_EmptySubject_ReturnsBadRequest()
    {
        return RunAsync(async context =>
        {
            // Arrange
            context.StubConfiguration(new FunctionSettings
            {
                IsSubjectRequired = true
            });

            using var httpRequest = CreateHttpRequest(
                name: "Captain Test",
                email: "test@test.com",
                subject: string.Empty,
                message: "Hello?!");

            // Act
            var response = await context
                .TriggerFunctionAsync(httpRequest)
                .ConfigureAwait(false);

            // Assert
            await response.Should()
                .HaveStatusCode(BadRequest).And
                .HaveContentAsync("Blank email details.")
                .ConfigureAwait(false);
        });
    }

    [TestMethod]
    public Task SendEmailThrows_ReturnsError()
    {
        return RunAsync(async context =>
        {
            // Arrange
            context.StubLogging();

            var sendEmailEx = new InvalidOperationException("BOOM");

            context.Mock<ISmtpClient>(mockSmtpClient =>
            {
                mockSmtpClient
                    .Setup(sc => sc.SendAsync(
                        It.IsAny<MimeMessage>(),
                        It.IsAny<CancellationToken>()))
                    .ThrowsAsync(sendEmailEx);
            });

            using var httpRequest = CreateHttpRequest(
                name: "Captain Test",
                email: "test@test.com",
                message: "Test message!");

            // Act
            var response = await context
                .TriggerFunctionAsync(httpRequest)
                .ConfigureAwait(false);

            // Assert
            response.Should().HaveStatusCode(InternalServerError);

            context.VerifyLogged<SendEmailFunction>(sendEmailEx);
        });
    }

    [TestMethod]
    public Task ValidDetails_ReturnsNoContent()
    {
        return RunAsync(async context =>
        {
            // Arrange
            context.StubConfiguration(new FunctionSettings
            {
                Recipient = "to@test.com",
                FallbackSubject = "You got mail!"
            });

            var mockSmtpClient = context.Mock<ISmtpClient>();

            using var httpRequest = CreateHttpRequest(
                name: "Captain Test",
                email: "test@test.com",
                message: "Test message!");

            // Act
            var response = await context
                .TriggerFunctionAsync(httpRequest)
                .ConfigureAwait(false);

            // Assert
            response.Should().HaveStatusCode(NoContent);

            await response
                .Should().HaveEmptyContentAsync()
                .ConfigureAwait(false);

            mockSmtpClient.Verify(
                smtp => smtp.SendAsync(
                    It.Is<MimeMessage>(msg =>
                        msg.From.Equals("Captain Test", "test@test.com") &&
                        msg.To.Equals("Steve", "to@test.com") &&
                        msg.Subject == "You got mail!" &&
                        msg.TextBody == "Test message!"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        });
    }

    [TestMethod]
    public Task ConfiguredRedirectUrl_ValidDetails_ReturnsRedirectUrlJson()
    {
        return RunAsync(async context =>
        {
            // Arrange
            context.StubConfiguration(new FunctionSettings
            {
                SuccessRedirectUrl = "email-sent.com",
                UseRedirectResponse = false
            });

            using var httpRequest = CreateHttpRequest(
                name: "Captain Test",
                email: "test@test.com",
                message: "Test message!");

            // Act
            var response = await context
                .TriggerFunctionAsync(httpRequest)
                .ConfigureAwait(false);

            // Assert
            await response.Should()
                .HaveStatusCode(OK).And
                .HaveContentAsync("{\"redirect\":\"email-sent.com\"}")
                .ConfigureAwait(false);

            context.VerifyEmailSent();
        });
    }

    [TestMethod]
    public Task UserRedirectUrl_ValidDetails_ReturnsRedirectUrlJson()
    {
        return RunAsync(async context =>
        {
            // Arrange
            context.StubConfiguration(new FunctionSettings
            {
                UseRedirectResponse = false,
                AllowUserRedirectUrls = true
            });

            using var httpRequest = CreateHttpRequest(
                name: "Captain Test",
                email: "test@test.com",
                message: "Test message!",
                redirectUrl: "user-email-sent.com");

            // Act
            var response = await context
                .TriggerFunctionAsync(httpRequest)
                .ConfigureAwait(false);

            // Assert
            await response.Should()
                .HaveStatusCode(OK).And
                .HaveContentAsync("{\"redirect\":\"user-email-sent.com\"}")
                .ConfigureAwait(false);

            context.VerifyEmailSent();
        });
    }

    [TestMethod]
    public Task UseRedirectResponse_ConfiguredRedirectUrl_UseValidDetails_ReturnsRedirect()
    {
        return RunAsync(async context =>
        {
            // Arrange
            context.StubConfiguration(new FunctionSettings
            {
                UseRedirectResponse = true,
                SuccessRedirectUrl = "https://email-sent.com"
            });

            using var httpRequest = CreateHttpRequest(
                name: "Captain Test",
                email: "test@test.com",
                message: "Test message!");

            // Act
            var response = await context
                .TriggerFunctionAsync(httpRequest)
                .ConfigureAwait(false);

            // Assert
            response.Should()
                .HaveStatusCode(Redirect).And
                .HaveLocationHeader("https://email-sent.com");

            await response
                .Should().HaveEmptyContentAsync()
                .ConfigureAwait(false);

            context.VerifyEmailSent();
        });
    }

    [TestMethod]
    public Task UseRedirectResponse_UserRedirectUrl_NoUserUrl_NoFallbackUrl_ReturnsBadRequest()
    {
        return RunAsync(async context =>
        {
            // Arrange
            context.StubConfiguration(new FunctionSettings
            {
                UseRedirectResponse = true,
                AllowUserRedirectUrls = true
            });

            using var httpRequest = CreateHttpRequest(
                name: "Captain Test",
                email: "test@test.com",
                message: "Test message?");

            // Act
            var response = await context
                .TriggerFunctionAsync(httpRequest)
                .ConfigureAwait(false);

            // Assert
            await response.Should()
                .HaveStatusCode(BadRequest).And
                .HaveContentAsync("Missing redirect URL.")
                .ConfigureAwait(false);

            context.VerifyEmailNotSent();
        });
    }

    [TestMethod]
    public Task UseRedirectResponse_UserRedirectUrl_ValidDetails_ReturnsRedirect()
    {
        return RunAsync(async context =>
        {
            // Arrange
            context.StubConfiguration(new FunctionSettings
            {
                UseRedirectResponse = true,
                AllowUserRedirectUrls = true
            });

            using var httpRequest = CreateHttpRequest(
                name: "Captain Test",
                email: "test@test.com",
                message: "Test message?!",
                redirectUrl: "https://user-email-sent.com");

            // Act
            var response = await context
                .TriggerFunctionAsync(httpRequest)
                .ConfigureAwait(false);

            // Assert
            response.Should()
                .HaveStatusCode(Redirect).And
                .HaveLocationHeader("https://user-email-sent.com");

            await response
                .Should().HaveEmptyContentAsync()
                .ConfigureAwait(false);

            context.VerifyEmailSent();
        });
    }

    [TestMethod]
    public Task UseRedirectResponse_UserRedirectUrl_NoUserUrl_ReturnsRedirectToFallbackUrl()
    {
        return RunAsync(async context =>
        {
            // Arrange
            context.StubConfiguration(new FunctionSettings
            {
                UseRedirectResponse = true,
                AllowUserRedirectUrls = true,
                SuccessRedirectUrl = "https://fallback-redirect.com"
            });

            using var httpRequest = CreateHttpRequest(
                name: "Captain Test",
                email: "test@test.com",
                message: "Test message?!");

            // Act
            var response = await context
                .TriggerFunctionAsync(httpRequest)
                .ConfigureAwait(false);

            // Assert
            response.Should()
                .HaveStatusCode(Redirect).And
                .HaveLocationHeader("https://fallback-redirect.com");

            await response
                .Should().HaveEmptyContentAsync()
                .ConfigureAwait(false);

            context.VerifyEmailSent();
        });
    }

    #region Helper Members

    protected override void ConfigureConfiguration(
        IConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Add(new FunctionSettings());
    }

    protected override void ConfigureFunction(
        ITezdiFunctionBuilder functionBuilder)
    {
        functionBuilder.Services
            .AddSendEmailFunction(functionBuilder.Configuration);
    }

    protected override void ConfigureContext(
        IAzureAspNetCoreFunctionTezdiContext tezdiContext)
    {
        tezdiContext.Mock<ISmtpClient>();
    }

    private static HttpRequestMessage CreateHttpRequest(
        string? name,
        string? email,
        string? subject = null,
        string? message = null,
        string? redirectUrl = null)
    {
        var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "api/SendEmail");

        var formData = new Dictionary<string, string?>();

        if (name != null)
        {
            formData[nameof(name)] = name;
        }

        if (email != null)
        {
            formData[nameof(email)] = email;
        }

        if (subject != null)
        {
            formData[nameof(subject)] = subject;
        }

        if (message != null)
        {
            formData[nameof(message)] = message;
        }

        if (redirectUrl != null)
        {
            formData[nameof(redirectUrl)] = redirectUrl;
        }

        httpRequest.Content = new FormUrlEncodedContent(formData);

        return httpRequest;
    }

    #endregion
}
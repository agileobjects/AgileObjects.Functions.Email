namespace AgileObjects.Functions.Email.UnitTests
{
    using System;
    using System.Net.Mail;
    using System.Text.Json;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Configuration;
    using Http;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Primitives;
    using Moq;
    using Moq.Language;
    using Smtp;
    using Xunit;

    public class WhenSendingEmails
    {
        [Fact]
        public async Task ShouldReturnBadRequestIfErrorReadingFormData()
        {
            var config = new FunctionConfiguration();

            var requestReader = CreateRequestReader(onReadForm => onReadForm
                .ThrowsAsync(new InvalidOperationException()));

            var function = new SendEmailFunction(
                config,
                requestReader,
                Mock.Of<ISmtpClient>);

            var result = await function.Run(Mock.Of<HttpRequest>(), Mock.Of<ILogger>());

            var badRequestResult = Assert.IsType<BadRequestErrorMessageResult>(result);

            Assert.Equal("Invalid form data", badRequestResult.Message);
        }

        [Fact]
        public async Task ShouldReturnBadRequestIfNameNotSupplied()
        {
            var config = new FunctionConfiguration();

            var requestReader = CreateRequestReader(
                "email", "test@test.com",
                "message", "Test message!");

            var function = new SendEmailFunction(
                config,
                requestReader,
                Mock.Of<ISmtpClient>);

            var result = await function.Run(Mock.Of<HttpRequest>(), Mock.Of<ILogger>());

            var badRequestResult = Assert.IsType<BadRequestErrorMessageResult>(result);

            Assert.Equal("Missing email details.", badRequestResult.Message);
        }

        [Fact]
        public async Task ShouldReturnBadRequestIfNameEmpty()
        {
            var config = new FunctionConfiguration();

            var requestReader = CreateRequestReader(
                "name", string.Empty,
                "email", "test@test.com",
                "message", "Test message!");

            var function = new SendEmailFunction(
                config,
                requestReader,
                Mock.Of<ISmtpClient>);

            var result = await function.Run(Mock.Of<HttpRequest>(), Mock.Of<ILogger>());

            var badRequestResult = Assert.IsType<BadRequestErrorMessageResult>(result);

            Assert.Equal("Blank email details.", badRequestResult.Message);
        }

        [Fact]
        public async Task ShouldReturnBadRequestIfEmailNotSupplied()
        {
            var config = new FunctionConfiguration();

            var requestReader = CreateRequestReader(
                "name", "Captain Test",
                "message", "Test message!");

            var function = new SendEmailFunction(
                config,
                requestReader,
                Mock.Of<ISmtpClient>);

            var result = await function.Run(Mock.Of<HttpRequest>(), Mock.Of<ILogger>());

            var badRequestResult = Assert.IsType<BadRequestErrorMessageResult>(result);

            Assert.Equal("Missing email details.", badRequestResult.Message);
        }

        [Fact]
        public async Task ShouldReturnBadRequestIfEmailEmpty()
        {
            var config = new FunctionConfiguration();

            var requestReader = CreateRequestReader(
                "name", "Captain Test",
                "email", string.Empty,
                "message", "Test message!");

            var function = new SendEmailFunction(
                config,
                requestReader,
                Mock.Of<ISmtpClient>);

            var result = await function.Run(Mock.Of<HttpRequest>(), Mock.Of<ILogger>());

            var badRequestResult = Assert.IsType<BadRequestErrorMessageResult>(result);

            Assert.Equal("Blank email details.", badRequestResult.Message);
        }

        [Fact]
        public async Task ShouldReturnBadRequestIfEmailInvalid()
        {
            var config = new FunctionConfiguration();

            var requestReader = CreateRequestReader(
                "name", "Captain Test",
                "email", "not a valid email",
                "message", "Test message!");

            var function = new SendEmailFunction(
                config,
                requestReader,
                Mock.Of<ISmtpClient>);

            var result = await function.Run(Mock.Of<HttpRequest>(), Mock.Of<ILogger>());

            var badRequestResult = Assert.IsType<BadRequestErrorMessageResult>(result);

            Assert.Equal("Invalid from email address 'not a valid email'.", badRequestResult.Message);
        }

        [Fact]
        public async Task ShouldReturnBadRequestIfSubjectRequiredAndNotSupplied()
        {
            var config = new FunctionConfiguration { IsSubjectRequired = true };

            var requestReader = CreateRequestReader(
                "name", "Captain Test",
                "email", "test@test.com",
                "message", "Test message!");

            var function = new SendEmailFunction(
                config,
                requestReader,
                Mock.Of<ISmtpClient>);

            var result = await function.Run(Mock.Of<HttpRequest>(), Mock.Of<ILogger>());

            var badRequestResult = Assert.IsType<BadRequestErrorMessageResult>(result);

            Assert.Equal("Missing email details.", badRequestResult.Message);
        }

        [Fact]
        public async Task ShouldReturnBadRequestIfSubjectRequiredAndEmpty()
        {
            var config = new FunctionConfiguration { IsSubjectRequired = true };

            var requestReader = CreateRequestReader(
                "name", "Captain Test",
                "email", "test@test.com",
                "subject", string.Empty,
                "message", "Test message!");

            var function = new SendEmailFunction(
                config,
                requestReader,
                Mock.Of<ISmtpClient>);

            var result = await function.Run(Mock.Of<HttpRequest>(), Mock.Of<ILogger>());

            var badRequestResult = Assert.IsType<BadRequestErrorMessageResult>(result);

            Assert.Equal("Blank email details.", badRequestResult.Message);
        }

        [Fact]
        public async Task ShouldReturnBadRequestIfMessageNotSupplied()
        {
            var config = new FunctionConfiguration();

            var requestReader = CreateRequestReader(
                "name", "Captain Test",
                "email", "test@test.com");

            var function = new SendEmailFunction(
                config,
                requestReader,
                Mock.Of<ISmtpClient>);

            var result = await function.Run(Mock.Of<HttpRequest>(), Mock.Of<ILogger>());

            var badRequestResult = Assert.IsType<BadRequestErrorMessageResult>(result);

            Assert.Equal("Missing email details.", badRequestResult.Message);
        }

        [Fact]
        public async Task ShouldReturnBadRequestIfMessageEmpty()
        {
            var config = new FunctionConfiguration();

            var requestReader = CreateRequestReader(
                "name", "Captain Test",
                "email", "test@test.com",
                "message", string.Empty);

            var function = new SendEmailFunction(
                config,
                requestReader,
                Mock.Of<ISmtpClient>);

            var result = await function.Run(Mock.Of<HttpRequest>(), Mock.Of<ILogger>());

            var badRequestResult = Assert.IsType<BadRequestErrorMessageResult>(result);

            Assert.Equal("Blank email details.", badRequestResult.Message);
        }

        [Fact]
        public async Task ShouldReturnOk()
        {
            var config = new FunctionConfiguration
            {
                Recipient = "to@test.com",
                FallbackSubject = "You got mail!"
            };

            var requestReader = CreateRequestReader(
                "name", "Captain Test",
                "email", "test@test.com",
                "message", "Test message!");

            var sentMail = default(MailMessage);

            var smtpClientMock = new Mock<ISmtpClient>();
            smtpClientMock
                .Setup(sc => sc.SendAsync(It.IsAny<MailMessage>()))
                .Callback((MailMessage mail) => sentMail = mail)
                .Returns(Task.CompletedTask);

            var function = new SendEmailFunction(
                config,
                requestReader,
                () => smtpClientMock.Object);

            var result = await function.Run(Mock.Of<HttpRequest>(), Mock.Of<ILogger>());

            Assert.IsType<OkResult>(result);

            Assert.NotNull(sentMail);
            Assert.Equal("\"Captain Test\" <test@test.com>", sentMail.From.ToString());
            Assert.Equal("to@test.com", sentMail.To.ToString());
            Assert.Equal("You got mail!", sentMail.Subject);
            Assert.Equal("Test message!", sentMail.Body);
        }

        [Fact]
        public async Task ShouldReturnOkWithFixedRedirectUrl()
        {
            var config = new FunctionConfiguration
            {
                Recipient = "to@test.com",
                FallbackSubject = "You got mail!",
                SuccessRedirectUrl = "email-sent.com",
                UseRedirectResponse = false
            };

            var requestReader = CreateRequestReader(
                "name", "Captain Test",
                "email", "test@test.com",
                "message", "Test message!");

            var sentMail = default(MailMessage);

            var smtpClientMock = new Mock<ISmtpClient>();
            smtpClientMock
                .Setup(sc => sc.SendAsync(It.IsAny<MailMessage>()))
                .Callback((MailMessage mail) => sentMail = mail)
                .Returns(Task.CompletedTask);

            var function = new SendEmailFunction(
                config,
                requestReader,
                () => smtpClientMock.Object);

            var result = await function.Run(Mock.Of<HttpRequest>(), Mock.Of<ILogger>());

            var redirectUrl = GetRedirectUrl(result);

            Assert.Equal("email-sent.com", redirectUrl);

            Assert.NotNull(sentMail);
        }

        [Fact]
        public async Task ShouldReturnOkWithSuppliedRedirectUrl()
        {
            var config = new FunctionConfiguration
            {
                Recipient = "to@test.com",
                FallbackSubject = "You got mail!",
                UseRedirectResponse = false,
                AllowUserRedirectUrls = true
            };

            var requestReader = CreateRequestReader(
                "name", "Captain Test",
                "email", "test@test.com",
                "message", "Test message!",
                "redirectUrl", "user-email-sent.com");

            var sentMail = default(MailMessage);

            var smtpClientMock = new Mock<ISmtpClient>();
            smtpClientMock
                .Setup(sc => sc.SendAsync(It.IsAny<MailMessage>()))
                .Callback((MailMessage mail) => sentMail = mail)
                .Returns(Task.CompletedTask);

            var function = new SendEmailFunction(
                config,
                requestReader,
                () => smtpClientMock.Object);

            var result = await function.Run(Mock.Of<HttpRequest>(), Mock.Of<ILogger>());

            var redirectUrl = GetRedirectUrl(result);

            Assert.Equal("user-email-sent.com", redirectUrl);

            Assert.NotNull(sentMail);
        }

        [Fact]
        public async Task ShouldReturnRedirectToFixedUrl()
        {
            var config = new FunctionConfiguration
            {
                Recipient = "to@test.com",
                FallbackSubject = "You got mail!",
                UseRedirectResponse = true,
                SuccessRedirectUrl = "email-sent.com"
            };

            var requestReader = CreateRequestReader(
                "name", "Captain Test",
                "email", "test@test.com",
                "message", "Test message!");

            var sentMail = default(MailMessage);

            var smtpClientMock = new Mock<ISmtpClient>();
            smtpClientMock
                .Setup(sc => sc.SendAsync(It.IsAny<MailMessage>()))
                .Callback((MailMessage mail) => sentMail = mail)
                .Returns(Task.CompletedTask);

            var function = new SendEmailFunction(
                config,
                requestReader,
                () => smtpClientMock.Object);

            var result = await function.Run(Mock.Of<HttpRequest>(), Mock.Of<ILogger>());

            var redirectResult = Assert.IsType<RedirectResult>(result);

            Assert.Equal("email-sent.com", redirectResult.Url);
            Assert.False(redirectResult.Permanent);

            Assert.NotNull(sentMail);
        }

        [Fact]
        public async Task ShouldReturnRedirectToSuppliedUrl()
        {
            var config = new FunctionConfiguration
            {
                Recipient = "to@test.com",
                FallbackSubject = "You got mail!",
                UseRedirectResponse = true,
                AllowUserRedirectUrls = true
            };

            var requestReader = CreateRequestReader(
                "name", "Captain Test",
                "email", "test@test.com",
                "message", "Test message!",
                "redirectUrl", "user-email-sent.com");

            var sentMail = default(MailMessage);

            var smtpClientMock = new Mock<ISmtpClient>();
            smtpClientMock
                .Setup(sc => sc.SendAsync(It.IsAny<MailMessage>()))
                .Callback((MailMessage mail) => sentMail = mail)
                .Returns(Task.CompletedTask);

            var function = new SendEmailFunction(
                config,
                requestReader,
                () => smtpClientMock.Object);

            var result = await function.Run(Mock.Of<HttpRequest>(), Mock.Of<ILogger>());

            var redirectResult = Assert.IsType<RedirectResult>(result);

            Assert.Equal("user-email-sent.com", redirectResult.Url);
            Assert.False(redirectResult.Permanent);

            Assert.NotNull(sentMail);
        }

        [Fact]
        public async Task ShouldReturnRedirectToFallbackUrl()
        {
            var config = new FunctionConfiguration
            {
                Recipient = "to@test.com",
                FallbackSubject = "You got mail!",
                UseRedirectResponse = true,
                AllowUserRedirectUrls = true,
                SuccessRedirectUrl = "email-sent.com"
            };

            var requestReader = CreateRequestReader(
                "name", "Captain Test",
                "email", "test@test.com",
                "message", "Test message!");

            var sentMail = default(MailMessage);

            var smtpClientMock = new Mock<ISmtpClient>();
            smtpClientMock
                .Setup(sc => sc.SendAsync(It.IsAny<MailMessage>()))
                .Callback((MailMessage mail) => sentMail = mail)
                .Returns(Task.CompletedTask);

            var function = new SendEmailFunction(
                config,
                requestReader,
                () => smtpClientMock.Object);

            var result = await function.Run(Mock.Of<HttpRequest>(), Mock.Of<ILogger>());

            var redirectResult = Assert.IsType<RedirectResult>(result);

            Assert.Equal("email-sent.com", redirectResult.Url);
            Assert.False(redirectResult.Permanent);

            Assert.NotNull(sentMail);
        }

        [Fact]
        public async Task ShouldReturnBadRequestIfSuppliedUrlRequiredAndNotSupplied()
        {
            var config = new FunctionConfiguration
            {
                Recipient = "to@test.com",
                FallbackSubject = "You got mail!",
                UseRedirectResponse = true,
                AllowUserRedirectUrls = true
            };

            var requestReader = CreateRequestReader(
                "name", "Captain Test",
                "email", "test@test.com",
                "message", "Test message!");

            var function = new SendEmailFunction(
                config,
                requestReader,
                Mock.Of<ISmtpClient>);

            var result = await function.Run(Mock.Of<HttpRequest>(), Mock.Of<ILogger>());

            var badRequestResult = Assert.IsType<BadRequestErrorMessageResult>(result);

            Assert.Equal("Missing redirect URL.", badRequestResult.Message);
        }

        [Fact]
        public async Task ShouldReturnIntervalServerErrorIfUnhandledException()
        {
            var config = new FunctionConfiguration
            {
                Recipient = "to@test.com",
                FallbackSubject = "You got mail!",
                UseRedirectResponse = true,
                AllowUserRedirectUrls = true
            };

            var requestReader = CreateRequestReader(
                "name", "Captain Test",
                "email", "test@test.com",
                "message", "Test message!");

            var smtpClientMock = new Mock<ISmtpClient>();
            smtpClientMock
                .Setup(sc => sc.SendAsync(It.IsAny<MailMessage>()))
                .ThrowsAsync(new InvalidOperationException("BOOM"));

            var function = new SendEmailFunction(
                config,
                requestReader,
                () => smtpClientMock.Object);

            var result = await function.Run(Mock.Of<HttpRequest>(), Mock.Of<ILogger>());

            Assert.IsType<InternalServerErrorResult>(result);
        }

        #region Helper Members

        private static IRequestReader CreateRequestReader(params string[] formNamesAndValues)
        {
            var formMock = new Mock<IFormCollection>();

            for (var i = 0; i < formNamesAndValues.Length;)
            {
                var name = formNamesAndValues[i++];
                var value = new StringValues(formNamesAndValues[i++]);

                formMock.Setup(f => f.TryGetValue(name, out value)).Returns(true);
            }

            return CreateRequestReader(onReadForm => onReadForm.ReturnsAsync(formMock.Object));
        }

        private static IRequestReader CreateRequestReader(
            Action<IReturns<IRequestReader, Task<IFormCollection>>> onReadForm)
        {
            var requestReaderMock = new Mock<IRequestReader>();

            onReadForm.Invoke(requestReaderMock
                .Setup(rr => rr.ReadFormAsync(It.IsAny<HttpRequest>())));

            return requestReaderMock.Object;
        }

        private static string GetRedirectUrl(IActionResult result)
        {
            var okResult = Assert.IsType<OkObjectResult>(result);

            return JsonDocument
                .Parse(JsonSerializer.Serialize(okResult.Value))
                .RootElement
                .GetProperty("Redirect")
                .GetString();
        }

        #endregion
    }
}

namespace AgileObjects.Functions.Email.UnitTests
{
    using Configuration;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Xunit;

    public class WhenSettingUpDependencies
    {
        [Fact]
        public void ShouldRegisterFunctionDependenciesWithOkResponse()
        {
            var configuration = CreateConfiguration(
                "SmtpHost", "mail.test.com",
                "SmtpUsername", "username@test.com",
                "SmtpPassword", "P4ssW0rd!",
                "Recipient", "to@test.com",
                "IsSubjectRequired", "false",
                "FallbackSubject", "Email received!",
                "UseRedirectResponse", "false",
                "AllowUserRedirectUrls", "false",
                "SuccessRedirectUrl", string.Empty);

            var services = new ServiceCollection();

            services.AddSendEmailFunction(configuration);

            var serviceProvider = services
                .AddTransient<SendEmailFunction>()
                .BuildServiceProvider();

            var config = serviceProvider.GetService<FunctionConfiguration>();
            
            Assert.NotNull(config);
            Assert.Equal("to@test.com", config.Recipient);
            Assert.False(config.IsSubjectRequired);
            Assert.Equal("Email received!", config.FallbackSubject);
            Assert.False(config.UseRedirectResponse);
            Assert.True(config.UseOkResponse);
            Assert.False(config.AllowUserRedirectUrls);
            Assert.True(config.HasNoSuccessRedirectUrl);
            Assert.Equal(string.Empty, config.SuccessRedirectUrl);

            var function = serviceProvider.GetService<SendEmailFunction>();
            Assert.NotNull(function);
        }

        [Fact]
        public void ShouldRegisterFunctionDependenciesWithOkResponseAndRequiredSubject()
        {
            var configuration = CreateConfiguration(
                "SmtpHost", "mail.test.com",
                "SmtpUsername", "username@test.com",
                "SmtpPassword", "P4ssW0rd!",
                "Recipient", "to@test.com",
                "IsSubjectRequired", "true",
                "FallbackSubject", "Email received!",
                "UseRedirectResponse", "false",
                "AllowUserRedirectUrls", "false",
                "SuccessRedirectUrl", string.Empty);

            var serviceProvider = new ServiceCollection()
                .AddSendEmailFunction(configuration)
                .AddTransient<SendEmailFunction>()
                .BuildServiceProvider();

            var config = serviceProvider.GetService<FunctionConfiguration>();
            
            Assert.NotNull(config);
            Assert.Equal("to@test.com", config.Recipient);
            Assert.True(config.IsSubjectRequired);

            var function = serviceProvider.GetService<SendEmailFunction>();
            Assert.NotNull(function);
        }

        [Fact]
        public void ShouldRegisterFunctionDependenciesWithRedirectResponse()
        {
            var configuration = CreateConfiguration(
                "SmtpHost", "mail.test.com",
                "SmtpUsername", "username@test.com",
                "SmtpPassword", "P4ssW0rd!",
                "Recipient", "to@test.com",
                "IsSubjectRequired", "false",
                "FallbackSubject", "Email received!",
                "UseRedirectResponse", "true",
                "AllowUserRedirectUrls", "true",
                "SuccessRedirectUrl", "email-sent.com");

            var serviceProvider = new ServiceCollection()
                .AddSendEmailFunction(configuration)
                .BuildServiceProvider();

            var config = serviceProvider.GetService<FunctionConfiguration>();
            
            Assert.NotNull(config);
            Assert.Equal("to@test.com", config.Recipient);
            Assert.False(config.IsSubjectRequired);
            Assert.Equal("Email received!", config.FallbackSubject);
            Assert.True(config.UseRedirectResponse);
            Assert.False(config.UseOkResponse);
            Assert.True(config.AllowUserRedirectUrls);
            Assert.False(config.HasNoSuccessRedirectUrl);
            Assert.Equal("email-sent.com", config.SuccessRedirectUrl);
        }

        [Fact]
        public void ShouldRegisterFunctionDependenciesWithMinimalSettings()
        {
            var configuration = CreateConfiguration(
                "SmtpHost", "mail.test.com",
                "SmtpUsername", "username@test.com",
                "SmtpPassword", "P4ssW0rd!",
                "Recipient", "to@test.com",
                "IsSubjectRequired", null,
                "FallbackSubject", null,
                "UseRedirectResponse", null,
                "AllowUserRedirectUrls", null,
                "SuccessRedirectUrl", null);

            var serviceProvider = new ServiceCollection()
                .AddSendEmailFunction(configuration)
                .BuildServiceProvider();

            var config = serviceProvider.GetService<FunctionConfiguration>();
            
            Assert.NotNull(config);
            Assert.Equal("to@test.com", config.Recipient);
            Assert.False(config.IsSubjectRequired);
            Assert.Equal("Email received", config.FallbackSubject);
            Assert.False(config.UseRedirectResponse);
            Assert.True(config.UseOkResponse);
            Assert.False(config.AllowUserRedirectUrls);
            Assert.True(config.HasNoSuccessRedirectUrl);
            Assert.Null(config.SuccessRedirectUrl);
        }

        #region Helper Members

        private static IConfiguration CreateConfiguration(params string[] keysAndValues)
        {
            var configurationMock = new Mock<IConfiguration>();

            for (var i = 0; i < keysAndValues.Length; )
            {
                var key = keysAndValues[i++];
                var value = keysAndValues[i++];

                var configSectionMock = new Mock<IConfigurationSection>();
                configSectionMock.SetupGet(cs => cs.Value).Returns(value);

                configurationMock.SetupGet(cfg => cfg[key]).Returns(value);
                configurationMock.Setup(cfg => cfg.GetSection(key)).Returns(configSectionMock.Object);
            }

            return configurationMock.Object;
        }

        #endregion
    }
}
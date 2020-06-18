namespace AgileObjects.Functions.Email.Configuration
{
    using System;
    using System.Net;
    using Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Smtp;

    public static class SetupExtensions
    {
        public static IServiceCollection AddSendEmailFunction(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var mailServer = configuration["SmtpHost"];

            var credentials = new NetworkCredential(
                configuration["SmtpUsername"],
                configuration["SmtpPassword"]);

            var settings = new FunctionConfiguration
            {
                Recipient = configuration["Recipient"],
                IsSubjectRequired = configuration.GetValue("IsSubjectRequired", defaultValue: false),
                FallbackSubject = configuration["FallbackSubject"] ?? "Email received",
                UseRedirectResponse = configuration.GetValue("UseRedirectResponse", defaultValue: false),
                AllowUserRedirectUrls = configuration.GetValue("AllowUserRedirectUrls", defaultValue: false),
                SuccessRedirectUrl = configuration["SuccessRedirectUrl"],
                ReCaptchaV2Key = configuration["ReCaptchaV2Key"]
            };

            return services
                .AddSingleton(settings)
                .AddSingleton(DefaultRequestReader.Instance)
                .AddSingleton<Func<ISmtpClient>>(sp => () => new BclSmtpClient(mailServer, credentials));
        }
    }
}

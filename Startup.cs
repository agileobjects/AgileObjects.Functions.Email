using Microsoft.Azure.Functions.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(AgileObjects.Functions.Email.Startup))]

namespace AgileObjects.Functions.Email
{
    using System.Net;
    using Microsoft.Azure.Functions.Extensions.DependencyInjection;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    public class Startup : FunctionsStartup
    {
        private readonly IConfiguration _configuration;

        public Startup()
        {
            _configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddUserSecrets(typeof(Startup).Assembly, optional: true)
                .Build();
        }

        public override void Configure(IFunctionsHostBuilder builder)
        {
            var credentials = new NetworkCredential(
                _configuration["SmtpUsername"],
                _configuration["SmtpPassword"]);

            var settings = new SmtpSettings
            {
                Host = _configuration["SmtpHost"],
                Credentials = credentials,
                Recipient = _configuration["SmtpRecipient"]
            };

            builder.Services.AddSingleton(settings);
        }
    }
}
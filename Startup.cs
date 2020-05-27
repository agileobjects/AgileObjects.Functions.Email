using Microsoft.Azure.Functions.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(AgileObjects.Functions.Email.Startup))]

namespace AgileObjects.Functions.Email
{
    using Configuration;
    using Microsoft.Azure.Functions.Extensions.DependencyInjection;
    using Microsoft.Extensions.Configuration;

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
            builder.Services.AddSendEmailFunction(_configuration);
        }
    }
}
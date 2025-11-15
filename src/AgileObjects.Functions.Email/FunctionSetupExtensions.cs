using AgileObjects.Functions.Email.Configuration;
using AgileObjects.Functions.Email.Http;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AgileObjects.Functions.Email;

internal static class FunctionSetupExtensions
{
    private const string _reCaptchaHttpClientName = "ReCaptchaHttpClient";

    public static IServiceCollection AddSendEmailFunction(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services
            .Configure<FunctionSettings>(configuration)
            .ConfigureJsonSerializer()
            .AddSingleton(DefaultRequestReader.Instance)
            .AddReCaptchaHttpClient()
            .AddSingleton<ISmtpClient>(sp =>
            {
                var settings = sp
                    .GetRequiredService<IOptions<FunctionSettings>>()
                    .Value;

                var smtpClient = new SmtpClient();

                smtpClient.Connect(settings.SmtpHost, settings.SmtpPort);
                smtpClient.Authenticate(settings.SmtpUsername, settings.SmtpPassword);

                return smtpClient;
            });
    }

    private static IServiceCollection ConfigureJsonSerializer(
        this IServiceCollection services)
    {
        return services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain
                .Insert(0, FunctionAppSerializerContext.Default);
        });
    }

    private static IServiceCollection AddReCaptchaHttpClient(
        this IServiceCollection services)
    {
        services
            .AddHttpClient(_reCaptchaHttpClientName, httpClient =>
            {
                httpClient.BaseAddress = new("https://www.google.com");
            });

        return services;
    }

    public static HttpClient CreateReCaptchaHttpClient(
        this IHttpClientFactory httpClientFactory)
    {
        return httpClientFactory.CreateClient(_reCaptchaHttpClientName);
    }
}
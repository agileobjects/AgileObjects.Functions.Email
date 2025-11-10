using AgileObjects.Functions.Email.Http;
using AgileObjects.Tezdi;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
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
                .TriggerFunctionPostAsync("SendEmail")
                .ConfigureAwait(false);

            // Assert
            response.Should().HaveStatusCode(BadRequest);
        });
    }

    #region Helper Members

    protected override void AddDefaultServices(
        IServiceCollection services)
    {
        services

        services.AddSendEmailFunction();
    }

    protected override void ConfigureFunction(
        IFunctionsWorkerApplicationBuilder functionBuilder)
    {
    }

    #endregion
}
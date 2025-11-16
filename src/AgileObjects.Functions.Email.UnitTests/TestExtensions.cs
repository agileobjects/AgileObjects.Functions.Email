using AgileObjects.Tezdi;
using FluentAssertions;
using FluentAssertions.Primitives;
using MailKit.Net.Smtp;
using MimeKit;
using Moq;

namespace AgileObjects.Functions.Email.UnitTests;

internal static class TestExtensions
{
    public static bool Equals(
        this InternetAddressList addresses,
        string name,
        string emailAddress)
    {
        var address = addresses
            .AsEnumerable()
            .Should()
            .ContainSingle().Subject
            .Should().BeOfType<MailboxAddress>().Subject;

        address.Name.Should().Be(name);
        address.Address.Should().Be(emailAddress);

        return true;
    }

    public static void HaveLocationHeader(
        this HttpResponseMessageAssertions httpResponseAssertions,
        string expectedLocation)
    {
        httpResponseAssertions
            .Subject.Headers.Location
            .Should().Be(new Uri(expectedLocation));
    }

    public static Task HaveEmptyContentAsync(
        this HttpResponseMessageAssertions httpResponseAssertions)
    {
        return httpResponseAssertions.HaveContentAsync(string.Empty);
    }

    public static async Task HaveContentAsync(
        this HttpResponseMessageAssertions httpResponseAssertions,
        string expectedContent)
    {
        (await httpResponseAssertions
            .Subject.Content
            .ReadAsStringAsync()
            .ConfigureAwait(false))
            .Should().Be(expectedContent);
    }

    public static void VerifyEmailNotSent(
        this ITezdiContext tezdiContext)
    {
        tezdiContext.VerifyEmailSent(Times.Never);
    }

    public static void VerifyEmailSent(
        this ITezdiContext tezdiContext,
        Func<Times>? times = null)
    {
        tezdiContext.Verify<ISmtpClient>(
            smtp => smtp.SendAsync(
                It.IsAny<MimeMessage>(),
                It.IsAny<CancellationToken>()),
            times);
    }
}
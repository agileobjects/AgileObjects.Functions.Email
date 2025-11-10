using System.Diagnostics.CodeAnalysis;
using System.Net.Mail;
using System.Text.Json;
using AgileObjects.Functions.Email.Configuration;
using AgileObjects.Functions.Email.Http;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using MimeKit;

namespace AgileObjects.Functions.Email;

public sealed class SendEmailFunction
{
    private readonly ILogger<SendEmailFunction> _logger;
    private readonly IOptions<FunctionSettings> _settingsAccessor;
    private readonly IRequestReader _requestReader;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISmtpClient _smtpClient;

    public SendEmailFunction(
        ILogger<SendEmailFunction> logger,
        IOptions<FunctionSettings> settingsAccessor,
        IRequestReader requestReader,
        IHttpClientFactory httpClientFactory,
        ISmtpClient smtpClient)
    {
        _logger = logger;
        _settingsAccessor = settingsAccessor;
        _requestReader = requestReader;
        _httpClientFactory = httpClientFactory;
        _smtpClient = smtpClient;
    }

    private FunctionSettings Settings => _settingsAccessor.Value;

    [Function("SendEmail")]
    public async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest request)
    {
        _logger.LogTrace($"{nameof(SendEmailFunction)} triggered");

        var cxlToken = request.HttpContext.RequestAborted;

        IFormCollection form;

        try
        {
            form = await _requestReader
                .ReadFormAsync(request, cxlToken)
                .ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            return new BadRequestObjectResult("Invalid form data.");
        }

        if (!TryGetEmailDetails(form, out var mail, out var errorMessage))
        {
            return new BadRequestObjectResult(errorMessage);
        }

        if (!await RecaptchaOkAsync(form, cxlToken).ConfigureAwait(false))
        {
            return new BadRequestObjectResult("ReCAPTCHA verification failed. Please try again.");
        }

        try
        {
            await _smtpClient
                .SendAsync(mail, cxlToken)
                .ConfigureAwait(false);

            _logger.LogInformation("Email sent");

            string? redirectUrl;

            if (Settings.UseOkResponse)
            {
                return TryGetRedirectUrl(form, out redirectUrl)
                    ? new OkObjectResult(new RedirectResponse { Redirect = redirectUrl })
                    : new NoContentResult();
            }

            return TryGetRedirectUrl(form, out redirectUrl)
                ? new RedirectResult(redirectUrl)
                : new BadRequestObjectResult("Missing redirect URL");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Message not sent. An unspecified error occurred.");
            throw;
        }
    }

    private bool TryGetEmailDetails(
        IFormCollection form,
        [NotNullWhen(true)] out MimeMessage? mail,
        [NotNullWhen(false)] out string? errorMessage)
    {
        var subject = default(StringValues);

        var subjectRequired = Settings.IsSubjectRequired;

        if (!form.TryGetValue("name", out var name) ||
            !form.TryGetValue("email", out var email) ||
            (subjectRequired && !form.TryGetValue("subject", out subject)) ||
            !form.TryGetValue("message", out var message))
        {
            mail = null;
            errorMessage = "Missing email details.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(name) ||
            string.IsNullOrWhiteSpace(email) ||
           (subjectRequired && string.IsNullOrWhiteSpace(subject)) ||
            string.IsNullOrWhiteSpace(message))
        {
            mail = null;
            errorMessage = "Blank email details.";
            return false;
        }

        if (EmailInvalid(email))
        {
            mail = null;
            errorMessage = $"Invalid from email address '{email}'.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            subject = Settings.FallbackSubject;
        }

        mail = new();

        mail.From.Add(new MailboxAddress(name, email));
        mail.To.Add(new MailboxAddress("Steve", Settings.Recipient));
        mail.Subject = subject;
        mail.Body = new TextPart("plain") { Text = message };

        errorMessage = null;
        return true;
    }

    private static bool EmailInvalid(string? email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return true;
        }

        try
        {
            _ = new MailAddress(email);
            return false;
        }
        catch (FormatException)
        {
            return true;
        }
    }

    private Task<bool> RecaptchaOkAsync(
        IFormCollection form,
        CancellationToken cancellationToken)
    {
        if (!Settings.VerifyReCaptcha)
        {
            return Task.FromResult(true);
        }

        var userResponse = form["g-recaptcha-response"];

        return string.IsNullOrWhiteSpace(userResponse)
            ? Task.FromResult(false)
            : VerifyRecaptchaAsync(userResponse, cancellationToken);
    }

    private async Task<bool> VerifyRecaptchaAsync(
        string? userResponse,
        CancellationToken cancellationToken)
    {
        var requestContent = new FormUrlEncodedContent(new Dictionary<string, string?>
        {
            ["secret"] = Settings.ReCaptchaV2Key,
            ["response"] = userResponse
        });

        var response = await _httpClientFactory
            .CreateReCaptchaHttpClient()
            .PostAsync(
                "/recaptcha/api/siteverify",
                requestContent,
                cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var responseContent = await response
            .Content
            .ReadAsStringAsync(cancellationToken)
            .ConfigureAwait(false);

        if (string.IsNullOrEmpty(responseContent))
        {
            return false;
        }

        var responseObject = JsonSerializer
            .Deserialize<ReCaptchaResponse>(
                responseContent,
                FunctionAppSerializerContext.Default.ReCaptchaResponse);

        return responseObject?.Success == true;
    }

    private bool TryGetRedirectUrl(
        IFormCollection form,
        [NotNullWhen(true)] out string? url)
    {
        if (Settings.AllowUserRedirectUrls &&
            form.TryGetValue("redirectUrl", out var urlValue) &&
           !string.IsNullOrEmpty(urlValue))
        {
            url = urlValue!;
            return true;
        }

        if (Settings.HasNoSuccessRedirectUrl)
        {
            url = null;
            return false;
        }

        url = Settings.SuccessRedirectUrl!;
        return true;
    }
}
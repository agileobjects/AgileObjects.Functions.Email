namespace AgileObjects.Functions.Email
{
    using System;
    using System.Net.Mail;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Configuration;
    using Http;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.Extensions.Logging;
    using Smtp;

    public class SendEmailFunction
    {
        private static readonly string _functionName = typeof(SendEmailFunction).FullName;

        private readonly FunctionConfiguration _configuration;
        private readonly IRequestReader _requestReader;
        private readonly Func<ISmtpClient> _smtpClientFactory;

        public SendEmailFunction(
            FunctionConfiguration configuration,
            IRequestReader requestReader,
            Func<ISmtpClient> smtpClientFactory)
        {
            _configuration = configuration;
            _requestReader = requestReader;
            _smtpClientFactory = smtpClientFactory;
        }

        [FunctionName("SendEmail")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest request,
            ILogger log)
        {
            log.LogTrace(_functionName + " triggered");

            var form = await _requestReader.ReadFormAsync(request);

            if (!TryGetEmailDetails(form, out var mail, out var errorMessage))
            {
                return new BadRequestErrorMessageResult(errorMessage);
            }

            var client = _smtpClientFactory.Invoke();

            try
            {
                using (client)
                {
                    await client.SendAsync(mail);
                }

                log.LogInformation("Email sent.");

                string redirectUrl;

                if (_configuration.UseOkResponse)
                {
                    if (TryGetRedirectUrl(form, out redirectUrl))
                    {
                        return new OkObjectResult(new
                        {
                            Redirect = redirectUrl
                        });
                    }

                    return new OkResult();
                }

                if (TryGetRedirectUrl(form, out redirectUrl))
                {
                    return new RedirectResult(redirectUrl);
                }

                return new BadRequestErrorMessageResult("Missing redirect URL.");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Message not sent. An unspecified error occurred.");

                return new InternalServerErrorResult();
            }
        }

        private bool TryGetEmailDetails(IFormCollection form, out MailMessage mail, out string errorMessage)
        {
            var subjectRequired = _configuration.IsSubjectRequired;

            if (!form.TryGetValue("name", out var name) ||
                !form.TryGetValue("email", out var email) ||
                (subjectRequired && !form.TryGetValue("subject", out var subject)) ||
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
                subject = _configuration.FallbackSubject;
            }

            mail = new MailMessage(
                $"{name} {email}",
                _configuration.Recipient,
                subject.ToString(),
                message);

            errorMessage = null;
            return true;
        }

        private static bool EmailInvalid(string email)
        {
            try
            {
                new MailAddress(email);
                return false;
            }
            catch (FormatException)
            {
                return true;
            }
        }

        private bool TryGetRedirectUrl(IFormCollection form, out string url)
        {
            if (_configuration.AllowUserRedirectUrls &&
                form.TryGetValue("redirectUrl", out var urlValue))
            {
                url = urlValue;
                return true;
            }

            if (_configuration.HasNoSuccessRedirectUrl)
            {
                url = null;
                return false;
            }

            url = _configuration.SuccessRedirectUrl;
            return true;
        }
    }
}

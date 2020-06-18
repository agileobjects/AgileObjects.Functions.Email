namespace AgileObjects.Functions.Email
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
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
    using Newtonsoft.Json;
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

            IFormCollection form;

            try
            {
                form = await _requestReader.ReadFormAsync(request);
            }
            catch (InvalidOperationException)
            {
                return new BadRequestErrorMessageResult("Invalid form data");
            }

            if (!TryGetEmailDetails(form, out var mail, out var errorMessage))
            {
                return new BadRequestErrorMessageResult(errorMessage);
            }

            if (!await RecaptchaOk(form))
            {
                return new BadRequestErrorMessageResult("ReCAPTCHA verification failed. Please try again.");
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

        private Task<bool> RecaptchaOk(IFormCollection form)
        {
            if (!_configuration.VerifyReCaptcha)
            {
                return Task.FromResult(true);
            }

            var userResponse = form["g-recaptcha-response"];

            if (string.IsNullOrWhiteSpace(userResponse))
            {
                return Task.FromResult(false);
            }

            return VerifyRecaptcha(userResponse);
        }

        private async Task<bool> VerifyRecaptcha(string userResponse)
        {
            var requestContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["secret"] = _configuration.ReCaptchaV2Key,
                ["response"] = userResponse
            });

            var response = await new HttpClient()
                .PostAsync("https://www.google.com/recaptcha/api/siteverify", requestContent);

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var responseContent = await response
                .Content
                .ReadAsStringAsync();

            if (string.IsNullOrEmpty(responseContent))
            {
                return false;
            }

            var responseObject = JsonConvert.DeserializeObject<ReCaptchaResponse>(responseContent);

            return responseObject?.Success == true;
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

        private class ReCaptchaResponse
        {
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            [JsonProperty(PropertyName = "success")]
            public bool Success { get; set; }
        }
    }
}

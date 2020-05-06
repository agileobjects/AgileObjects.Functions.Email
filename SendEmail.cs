namespace AgileObjects.Functions.Email
{
    using System;
    using System.Net.Mail;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.Extensions.Logging;

    public class SendEmail
    {
        private readonly SmtpSettings _settings;

        public SendEmail(SmtpSettings settings)
        {
            _settings = settings;
        }

        [FunctionName("SendEmail")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest request,
            ILogger log)
        {
            log.LogTrace("AgileObjects.Functions.Email.SendEmail triggered");

            var form = await request.ReadFormAsync();

            if (!TryGetEmailDetails(form, out var mail, out var errorMessage))
            {
                return new BadRequestErrorMessageResult(errorMessage);
            }

            var client = new SmtpClient(_settings.Host)
            {
                EnableSsl = false,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = _settings.Credentials
            };

            try
            {
                using (client)
                {
                    client.Send(mail);
                }

                log.LogInformation("Email sent.");

                return new OkResult();
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Message not sent. An unspecified error occurred.");

                return new InternalServerErrorResult();
            }
        }

        private bool TryGetEmailDetails(IFormCollection form, out MailMessage mail, out string errorMessage)
        {
            if (!form.TryGetValue("name", out var name) ||
                !form.TryGetValue("email", out var email) ||
                !form.TryGetValue("message", out var message))
            {
                mail = null;
                errorMessage = "Missing email details.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(message))
            {
                mail = null;
                errorMessage = "Blank email details.";
                return false;
            }

            try
            {
                new MailAddress(email);
            }
            catch (FormatException)
            {
                mail = null;
                errorMessage = "Invalid from email.";
                return false;
            }

            mail = new MailMessage(
                $"{name} {email}",
                _settings.Recipient,
                "Email from agileobjects.co.uk",
                message);

            errorMessage = null;
            return true;
        }
    }
}

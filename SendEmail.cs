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
        private static readonly string _functionName = typeof(SendEmail).FullName;

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
            log.LogTrace(_functionName + " triggered");

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
            var subjectRequired = _settings.IsSubjectRequired;

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

            if (EmailInvalid(email, out mail, out errorMessage))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(subject))
            {
                subject = _settings.FallbackSubject;
            }

            mail = new MailMessage(
                $"{name} {email}",
                _settings.Recipient,
                subject.ToString(),
                message);

            errorMessage = null;
            return true;
        }

        private static bool EmailInvalid(
            string email, 
            out MailMessage mail, 
            out string errorMessage)
        {
            mail = null;
            
            try
            {
                new MailAddress(email);
                errorMessage = null;
                return false;
            }
            catch (FormatException)
            {
                errorMessage = "Invalid from email.";
                return true;
            }
        }
    }
}

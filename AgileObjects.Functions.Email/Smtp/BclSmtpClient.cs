namespace AgileObjects.Functions.Email.Smtp
{
    using System.Net;
    using System.Net.Mail;
    using System.Threading.Tasks;

    internal class BclSmtpClient : ISmtpClient
    {
        private readonly SmtpClient _smtpClient;

        public BclSmtpClient(string mailServer, ICredentialsByHost credentials)
        {
            _smtpClient = new SmtpClient(mailServer)
            {
                EnableSsl = false,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = credentials
            };
        }

        public Task SendAsync(MailMessage mail)
            => _smtpClient.SendMailAsync(mail);

        public void Dispose()
            => _smtpClient.Dispose();
    }
}
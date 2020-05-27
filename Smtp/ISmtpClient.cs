namespace AgileObjects.Functions.Email.Smtp
{
    using System;
    using System.Net.Mail;
    using System.Threading.Tasks;

    public interface ISmtpClient : IDisposable
    {
        Task SendAsync(MailMessage mail);
    }
}

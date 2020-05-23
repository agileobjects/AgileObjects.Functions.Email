namespace AgileObjects.Functions.Email
{
    using System.Net;

    public class SmtpSettings
    {
        public string Host { get; set; }
        
        public ICredentialsByHost Credentials { get; set; }
        
        public string Recipient { get; set; }

        public bool IsSubjectRequired { get; set; }

        public string FallbackSubject { get; set; }
    }
}
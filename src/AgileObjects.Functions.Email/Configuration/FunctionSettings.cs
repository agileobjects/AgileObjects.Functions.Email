namespace AgileObjects.Functions.Email.Configuration;

public sealed class FunctionSettings
{
    public FunctionSettings()
    {
        SmtpHost = string.Empty;
        SmtpUsername = string.Empty;
        SmtpPassword = string.Empty;

        Recipient = string.Empty;
        FallbackSubject = "Email received";
    }

    public string SmtpHost { get; set; }

    public int SmtpPort { get; set; }

    public string SmtpUsername { get; set; }

    public string SmtpPassword { get; set; }

    public string Recipient { get; set; }

    public bool IsSubjectRequired { get; set; }

    public string FallbackSubject { get; set; }

    public bool UseRedirectResponse { get; set; }

    public bool UseOkResponse => !UseRedirectResponse;

    public bool AllowUserRedirectUrls { get; set; }

    public bool HasNoSuccessRedirectUrl => string.IsNullOrEmpty(SuccessRedirectUrl);

    public string? SuccessRedirectUrl { get; set; }

    public bool VerifyReCaptcha => !string.IsNullOrEmpty(ReCaptchaV2Key);

    public string? ReCaptchaV2Key { get; set; }
}
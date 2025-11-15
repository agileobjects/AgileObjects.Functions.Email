using System.Text.Json.Serialization;

namespace AgileObjects.Functions.Email;

internal sealed class ReCaptchaResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
}
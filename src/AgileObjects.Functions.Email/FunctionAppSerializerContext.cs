using System.Text.Json.Serialization;

namespace AgileObjects.Functions.Email;

[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(ReCaptchaResponse))]
[JsonSerializable(typeof(RedirectResponse))]
internal sealed partial class FunctionAppSerializerContext : JsonSerializerContext
{
}
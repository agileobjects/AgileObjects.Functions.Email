using Microsoft.AspNetCore.Http;

namespace AgileObjects.Functions.Email.Http;

internal sealed class DefaultRequestReader : IRequestReader
{
    public static readonly IRequestReader Instance = new DefaultRequestReader();

    public Task<IFormCollection> ReadFormAsync(
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        return request.ReadFormAsync(cancellationToken);
    }
}
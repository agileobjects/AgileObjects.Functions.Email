using Microsoft.AspNetCore.Http;

namespace AgileObjects.Functions.Email.Http;

public interface IRequestReader
{
    Task<IFormCollection> ReadFormAsync(
        HttpRequest request,
        CancellationToken cancellationToken);
}
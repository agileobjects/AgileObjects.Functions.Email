namespace AgileObjects.Functions.Email.Http
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;

    internal class DefaultRequestReader : IRequestReader
    {
        public static readonly IRequestReader Instance = new DefaultRequestReader();

        public Task<IFormCollection> ReadFormAsync(HttpRequest request)
            => request.ReadFormAsync();
    }
}

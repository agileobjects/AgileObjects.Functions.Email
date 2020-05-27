namespace AgileObjects.Functions.Email.Http
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;

    public interface IRequestReader
    {
        Task<IFormCollection> ReadFormAsync(HttpRequest request);
    }
}
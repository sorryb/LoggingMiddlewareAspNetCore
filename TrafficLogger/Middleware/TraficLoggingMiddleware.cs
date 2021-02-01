using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace TrafficLogger.Middleware
{
    public class TraficLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TraficLoggingMiddleware> _logger;

        public TraficLoggingMiddleware(RequestDelegate next, ILogger<TraficLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            var request = await ReadRequest(context.Request);
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await _next(context);


            var response = await ReadResponse(context.Response);
            _logger.LogInformation($"The Request is :'{request}' and the Response is :'{response}' .");

            await responseBody.CopyToAsync(originalBodyStream);
        }

        private static async Task<string> ReadRequest(HttpRequest request)
        {
            var requestBody = request.Body;

            request.EnableBuffering();

            var bufferBody = new byte[Convert.ToInt32(request.ContentLength)];

            await request.Body.ReadAsync(bufferBody.AsMemory(0, bufferBody.Length)).ConfigureAwait(false);

            var bodyAsText = Encoding.UTF8.GetString(bufferBody);

            request.Body = requestBody;

            return $"path-'{request.Path}'; query string-'{request.QueryString}'; body-'{bodyAsText}' schema-'{request.Scheme}'; host-'{request.Host}'; ";
        }

        private static async Task<string> ReadResponse(HttpResponse response)
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            string responseBody = await new StreamReader(response.Body).ReadToEndAsync();
            response.Body.Seek(0, SeekOrigin.Begin);

            return $"response body - '{responseBody}'; status code - '{response.StatusCode}'; content type - {response.ContentType}";
        }
    }
}

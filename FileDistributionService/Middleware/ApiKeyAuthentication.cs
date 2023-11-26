using FileDistributionService;
using Microsoft.AspNetCore.Http;

namespace FileDistributionService.Middleware
{
    public class ApiKeyAuthentication
    {
        private readonly RequestDelegate _requestDelegate;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FileValidation> _logger;

        public ApiKeyAuthentication(RequestDelegate requestDelegate, IConfiguration configuration, ILogger<FileValidation> logger)
        {
            _configuration = configuration;
            _requestDelegate = requestDelegate;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context, ApplicationContext _applicationContext)
        {
            var apiKeyName = _configuration.GetSection("Authentication:ApiKeyName").Value;

            if (!context.Request.Headers.TryGetValue(apiKeyName, out var key))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync($"{apiKeyName} missing");
                return;
            }

            var apiKey = _configuration.GetSection("Authentication:ApiKey").Value;

            if (!apiKey.Equals(key))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync($"Invalid {apiKeyName}");
                return;
            }

            await _requestDelegate(context);
        }
    }
}

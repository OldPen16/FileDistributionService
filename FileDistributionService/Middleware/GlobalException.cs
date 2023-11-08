using System.Text.Json;

namespace FileDistributionService.Middleware
{
    public class GlobalException
    {
        private readonly RequestDelegate _requestDelegate;
        private readonly ILogger<GlobalException> _logger;

        public GlobalException(RequestDelegate requestDelegate, ILogger<GlobalException> logger)
        {
            _requestDelegate = requestDelegate;
            _logger = logger;
        }

        /// Catch exception that occurs anywhere within the application and response with custom message  
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _requestDelegate(context);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An unhandled exception occurred.");

                var response = new
                {
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = "Sorry! Error occurred while processing your request."
                };

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
        }
    }
}
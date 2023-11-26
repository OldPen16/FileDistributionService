using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Text.Json;

namespace FileDistributionService.Middleware
{
    public class FileException : Exception
    {
        public FileException()
            : base()
        {

        }
    }
    public class DatabaseException : Exception
    {
        public DatabaseException()
            : base()
        {

        }
    }
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
            catch (FileException validationException)
            {
                _logger.LogError(validationException, "File exception.");

                var response = new Response 
                {
                    StatusCode = StatusCodes.Status500InternalServerError,
                    StatusMessage = "Sorry! File exception occurred."
                };

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
            catch (DatabaseException databaseException)
            {
                _logger.LogError(databaseException, "Database exception.");

                var response = new Response
                {
                    StatusCode = StatusCodes.Status500InternalServerError,
                    StatusMessage = "Sorry! Database exception occurred."
                };

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "An unknown exception occurred.");

                var response = new Response
                {
                    StatusCode = StatusCodes.Status500InternalServerError,
                    StatusMessage = "Sorry! Error occurred while processing your request."
                };

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
        }
    }
}

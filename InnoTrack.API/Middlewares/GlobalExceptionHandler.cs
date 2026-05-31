using InnoTrack.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace InnoTrack.API.Middlewares
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;
        private readonly IHostEnvironment _env;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        public async ValueTask<bool> TryHandleAsync
            (HttpContext httpContext,
             Exception exception,
             CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "Exception occured: {Message}", exception.Message);

            var (statusCode, title) = exception switch
            {
                KeyNotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
                UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),

                ArgumentException => (StatusCodes.Status400BadRequest, "Bad Request"),

                InvalidOperationException => (StatusCodes.Status409Conflict, "Conflict"),

                AppException appEx => (appEx.StatusCode, appEx.GetType().Name.Replace("Exception", string.Empty)),

                _ => (StatusCodes.Status500InternalServerError, "Server Error")
            };

            //var detail = _env.IsDevelopment()
            //    ? exception.Message
            //    : "An unexpected error occurred. Please contact support.";

            var detail = (_env.IsDevelopment() || statusCode != StatusCodes.Status500InternalServerError)
                ? exception.Message
                : "An unexpected error occurred. Please contact support.";
                
            //var detail = exception.Message;

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail
            };

            httpContext.Response.StatusCode = statusCode;
            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true;
        }
    }
}

namespace Catalog.API.Exceptions
{
    using FluentValidation;
    using Microsoft.AspNetCore.Diagnostics;
    public class CustomExceptionHandler : IExceptionHandler
    {

        private readonly ILogger<CustomExceptionHandler> _logger;

        public CustomExceptionHandler(
            ILogger<CustomExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            _logger.LogError(
                exception,
                "Excepción capturada");

            var statusCode = StatusCodes.Status500InternalServerError;
            if (exception is ValidationException)
            {
                statusCode = StatusCodes.Status400BadRequest;
            }

            httpContext.Response.StatusCode = statusCode;

            await httpContext.Response.WriteAsJsonAsync(
                new
                {
                    Title = exception.GetType().Name,
                    Status = statusCode,
                    Detail = exception.Message
                },

                cancellationToken);

            return true;
        }
    }
}

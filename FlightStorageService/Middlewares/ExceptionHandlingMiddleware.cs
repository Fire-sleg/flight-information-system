using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Net;
using System.Text.Json;

namespace FlightStorageService.Middlewares
{
    public sealed class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning(
                    "Request cancelled by client. {Method} {Path}",
                    context.Request.Method,
                    context.Request.Path);

                context.Response.StatusCode = 499;
            }
            catch (SqlException ex)
            {
                await HandleException(
                    context,
                    ex,
                    HttpStatusCode.InternalServerError,
                    "Database error occurred");
            }
            catch (ArgumentException ex)
            {
                await HandleException(
                    context,
                    ex,
                    HttpStatusCode.BadRequest,
                    ex.Message);
            }
            catch (Exception ex)
            {
                await HandleException(
                    context,
                    ex,
                    HttpStatusCode.InternalServerError,
                    "Unexpected server error");
            }
        }

        private async Task HandleException(
            HttpContext context,
            Exception exception,
            HttpStatusCode status,
            string title)
        {
            _logger.LogError(
                exception,
                "Unhandled exception. {Method} {Path} TraceId={TraceId}",
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier);

            if (context.Response.HasStarted)
                return;

            var problem = new ProblemDetails
            {
                Status = (int)status,
                Title = title,
                Instance = context.Request.Path,
                Extensions =
                {
                    ["traceId"] = context.TraceIdentifier
                }
            };

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = problem.Status.Value;

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(problem));
        }
    }
}

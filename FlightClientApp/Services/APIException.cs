using FlightClientApp.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace FlightClientApp.Services
{
    public class APIException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public ProblemDetails? Problem { get; }

        public APIException(HttpStatusCode statusCode, ProblemDetails? problem, string? fallbackMessage = null)
            : base(problem?.Detail ?? problem?.Title ?? fallbackMessage ?? $"API error {(int)statusCode}")
        {
            StatusCode = statusCode;
            Problem = problem;
        }
    }
}

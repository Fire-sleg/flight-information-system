using FlightStorageService.Middlewares;
using FlightStorageService.Models;
using FlightStorageService.Repositories;
using FlightStorageService.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.SuppressModelStateInvalidFilter = true;
    });

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Flight Information API",
        Version = "v1",
        Description = """
        REST API for flight search.

        Limitations:
        - Data is only available for flights within the next 7 days
        - All dates are UTC
        - Rate limit: 12 requests / 12 seconds; queue limit: 4
        """,
        Contact = new OpenApiContact
        {
            Name = "Flight API Team"
        }
    });

    // ProblemDetails schema
    options.MapType<ProblemDetails>(() => new OpenApiSchema
    {
        Type = "object"
    });
});

// CORS
builder.Services.Configure<CorsSettings>(
    builder.Configuration.GetSection("CorsSettings"));

builder.Services.AddCors(options =>
{
    var cors = builder.Configuration
        .GetSection("CorsSettings")
        .Get<CorsSettings>() ?? throw new InvalidOperationException("CorsSettings missing");

    options.AddPolicy("ui", policy =>
    {
        policy.WithOrigins(cors.AllowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// HTTP Logging
builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields =
        HttpLoggingFields.RequestMethod |
        HttpLoggingFields.RequestPath |
        HttpLoggingFields.ResponseStatusCode |
        HttpLoggingFields.Duration;
});

// Rate limiting 
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.PermitLimit = 12;
        opt.Window = TimeSpan.FromSeconds(12);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 4;
    });
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = 429;

        await context.HttpContext.Response.WriteAsync(
            "Too many requests. Please try again in a few seconds.",
            cancellationToken);

        context.HttpContext.Response.Headers.Append("Retry-After", "12");  
    };
});

// Cache
builder.Services.AddMemoryCache();

// DI
builder.Services.AddScoped<IFlightRepository, FlightRepository>();
builder.Services.AddScoped<IFlightService, FlightService>();

//HealthCheck
builder.Services.AddHealthChecks();

var app = builder.Build();

// Middlewares
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpLogging();

app.UseCors("ui");

app.UseRateLimiter();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready");


app.MapControllers()
   .RequireRateLimiting("fixed");

app.Run();

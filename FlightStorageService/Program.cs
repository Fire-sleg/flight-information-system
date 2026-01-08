using FlightStorageService.Middlewares;
using FlightStorageService.Models;
using FlightStorageService.Repositories;
using FlightStorageService.Services;
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
        REST API для пошуку авіарейсів.

        Обмеження:
        - Дані доступні лише для рейсів у межах наступних 7 днів
        - Усі дати — UTC
        - Rate limit: 60 запитів / хвилину
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
            "Занадто багато запитів. Будь ласка, спробуйте знову через кілька секунд.",
            cancellationToken);

        context.HttpContext.Response.Headers.Append("Retry-After", "12");  
    };
});

// Cache
builder.Services.AddMemoryCache();

// DI
builder.Services.AddScoped<IFlightRepository, FlightRepository>();
builder.Services.AddScoped<IFlightService, FlightService>();

var app = builder.Build();

// Middlewares

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    ctx.Response.Headers["Referrer-Policy"] = "no-referrer";
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpLogging();

app.UseHttpsRedirection();

app.UseCors("ui");

app.UseRateLimiter();

app.MapControllers()
   .RequireRateLimiting("fixed");

app.Run();

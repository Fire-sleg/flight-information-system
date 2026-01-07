using FlightStorageService.Models;
using FlightStorageService.Repositories;
using FlightStorageService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHttpClient();

builder.Services.AddCors(options =>
{
    var corsSettings = builder.Configuration.GetSection("CorsSettings").Get<CorsSettings>();

    options.AddPolicy("ui", policy =>
    {
        policy.WithOrigins(corsSettings.AllowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllers();

builder.Services.AddMemoryCache(o => o.SizeLimit = 10_000);
builder.Services.AddOpenApi();

builder.Services.AddScoped<IFlightRepository, FlightRepository>();
builder.Services.AddScoped<IFlightService, FlightService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseHttpLogging();

app.UseHttpsRedirection();
app.UseCors("ui");

app.UseAuthorization();

app.MapControllers();

app.Run();

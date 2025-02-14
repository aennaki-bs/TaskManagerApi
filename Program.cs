using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using TaskManagerAPI.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(); // Enables Swagger for API testing

var connectionString = "Server=localhost;Database=TaskManagerDB;Trusted_Connection=True;TrustServerCertificate=True;";
builder.Services.AddDbContext<TaskDbContext>(options =>
    options.UseSqlServer(connectionString));


var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); // Enables Swagger in development mode
    app.UseSwaggerUI(); // Provides a UI to test APIs
}

// Uncomment the next line if you want HTTPS redirection (optional)
// app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

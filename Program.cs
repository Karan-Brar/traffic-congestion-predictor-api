using DotNetEnv;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Cors;

var builder = WebApplication.CreateBuilder(args);

DotNetEnv.Env.Load();

string hereApiKey = Environment.GetEnvironmentVariable("HERE_API_KEY");

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});
builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<TrafficService>(sp =>
    new TrafficService(sp.GetRequiredService<HttpClient>(), hereApiKey)
);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.MapGet("/api/traffic/{city}", async (string city, TrafficService trafficService) =>
{
    var cityCenter = await trafficService.GetCenterForCityAsync(city);
    double latMin = cityCenter.Latitude - 0.09;
    double latMax = cityCenter.Latitude + 0.09;
    double lngMin = cityCenter.Longitude - 0.09;
    double lngMax = cityCenter.Longitude + 0.09;
    double gridSize = 0.02;

    var gridCells = trafficService.GenerateGrid(latMin, latMax, lngMin, lngMax, gridSize);

    var trafficData = await trafficService.GetTrafficForCityGridAsync(gridCells);

    if (trafficData == null)
    {
        return Results.NotFound(new { message = "No traffic data found for the specified city." });
    }

    return Results.Ok(trafficData);
}).WithName("GetTrafficData").WithOpenApi();

app.MapGet("/api/city-centre/{city}", async (string city, TrafficService trafficService) =>
{
    var centreData = await trafficService.GetCenterForCityAsync(city);

    if (centreData == null)
    {
        return Results.NotFound(new { message = "Failed to fetch centre for the specified city." });
    }

    return Results.Ok(centreData);
}).WithName("GetCityCentreData").WithOpenApi();


app.Run();


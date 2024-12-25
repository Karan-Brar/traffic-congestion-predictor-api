using DotNetEnv;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

DotNetEnv.Env.Load();

string googleMapsApiKey = Environment.GetEnvironmentVariable("GOOGLE_MAPS_API_KEY");
string googlePlacesApiKey = Environment.GetEnvironmentVariable("GOOGLE_PLACES_API_KEY");
string googleDirectionsApiKey = Environment.GetEnvironmentVariable("GOOGLE_DIRECTIONS_API_KEY");

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<TrafficService>(sp =>
    new TrafficService(sp.GetRequiredService<HttpClient>(), googleMapsApiKey, googlePlacesApiKey, googleDirectionsApiKey)
);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/api/traffic/key-areas/{city}", async (string city, TrafficService trafficService) =>
{
    var trafficData = await trafficService.GetTrafficForCityAsync(city);

    if (trafficData == null)
    {
        return Results.NotFound(new { message = "No traffic data found for the specified city." });
    }

    return Results.Ok(trafficData);
}).WithName("GetTrafficData").WithOpenApi();


app.Run();


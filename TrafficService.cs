using System.Collections.Generic;
using System.Configuration;
using System.Net.Cache;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class TrafficService
{
    private readonly HttpClient _httpClient;
    private readonly string _googleMapsAPIKey;
    private readonly string _googlePlacesAPIKey;
    private readonly string _googleDirectionsAPIKey;


    public TrafficService(HttpClient httpClient, IConfiguration configuration,string googleMapsApiKey,string googlePlacesApiKey,string googleDirectionsApiKey)
    {
        _httpClient = httpClient;
        _googleMapsAPIKey = configuration["googleMapsApiKey"];
        _googlePlacesAPIKey = configuration["googlePlacesApiKey"];
        _googleDirectionsAPIKey = configuration["googleDirectionsApiKey"];
    }

    public async Task<List<TrafficData>> GetTrafficForCityAsync(string city)
    {

    }
}

public class TrafficData
{
    public string AreaName { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int TrafficLevel { get; set; }
}
using System.Collections.Generic;
using System.Configuration;
using System.Net.Cache;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text.Json;

public class TrafficService
{
    private readonly HttpClient _httpClient;
    private readonly string _googleMapsAPIKey;
    private readonly string _googlePlacesAPIKey;
    private readonly string _googleDirectionsAPIKey;


    public TrafficService(HttpClient httpClient,string googleMapsApiKey,string googlePlacesApiKey,string googleDirectionsApiKey)
    {
        _httpClient = httpClient;
        _googleMapsAPIKey = googleMapsApiKey;
        _googlePlacesAPIKey = googlePlacesApiKey;
        _googleDirectionsAPIKey = googleDirectionsApiKey;
    }

    public async Task<List<TrafficData>> GetTrafficForCityAsync(string city)
    {
        var placesResponse = await _httpClient.GetAsync(
           $"https://maps.googleapis.com/maps/api/place/textsearch/json?query=important+areas+in+{city}&key={_googlePlacesAPIKey}");

        var placesData = JsonDocument.Parse(await placesResponse.Content.ReadAsStringAsync());
        var keyAreas = placesData.RootElement.GetProperty("results")
            .EnumerateArray()
            .Select(place => new
            {
                Name = place.GetProperty("name").GetString(),
                Lat = place.GetProperty("geometry").GetProperty("location").GetProperty("lat").GetDouble(),
                Lng = place.GetProperty("geometry").GetProperty("location").GetProperty("lng").GetDouble()
            })
            .ToList();

        var trafficDataList = new List<TrafficData>();

        foreach(var area in keyAreas)
        {
            var trafficResponse = await _httpClient.GetAsync(
                $"https://maps.googleapis.com/maps/api/directions/json?origin={area.Lat},{area.Lng}&destination={area.Lat + 0.01},{area.Lng + 0.01}&departure_time=now&key={_googleDirectionsAPIKey}");

            var trafficJson = JsonDocument.Parse(await trafficResponse.Content.ReadAsStringAsync());

            var durationInTraffic = trafficJson.RootElement
                .GetProperty("routes")[0]
                .GetProperty("legs")[0]
                .GetProperty("duration_in_traffic").GetProperty("value").GetInt32();

            trafficDataList.Add(new TrafficData
            {
                AreaName = area.Name,
                Latitude = area.Lat,
                Longitude = area.Lng,
                TrafficLevel = durationInTraffic
            });
        }

        return trafficDataList;
    }
}

public class TrafficData
{
    public string AreaName { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int TrafficLevel { get; set; }
}
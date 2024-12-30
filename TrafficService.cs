using System.Collections.Generic;
using System.Configuration;
using System.Net.Cache;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text.Json;
using System.Linq;

public class TrafficService
{
    private readonly HttpClient _httpClient;
    //private readonly string _googleMapsAPIKey;
    //private readonly string _googlePlacesAPIKey;
    //private readonly string _googleDirectionsAPIKey;
    private readonly string _hereAPIKey;


    public TrafficService(HttpClient httpClient, string hereApiKey)
    {
        _httpClient = httpClient;
        //_googleMapsAPIKey = googleMapsApiKey;
        //_googlePlacesAPIKey = googlePlacesApiKey;
        //_googleDirectionsAPIKey = googleDirectionsApiKey;
        _hereAPIKey = hereApiKey;
    }

    public List<GridCell> GenerateGrid(double latMin, double latMax, double lngMin, double lngMax, double gridSize)
    {
        var gridCells = new List<GridCell>();

        for (double lat = latMin; lat <= latMax; lat += gridSize)
        {
            for (double lng = lngMin; lng <= lngMax; lng += gridSize)
            {
                gridCells.Add(new GridCell { Latitude = lat, Longitude = lng });
            }
        }

        return gridCells;
    }

    public async Task<CityCenterData> GetCenterForCityAsync(string city)
    {
        var centerResponse = await _httpClient.GetAsync(
            $"https://geocode.search.hereapi.com/v1/geocode?q={city}&apiKey={_hereAPIKey}");

        var centerData = JsonDocument.Parse(await centerResponse.Content.ReadAsStringAsync());

        //var lat = centerData.RootElement.GetProperty("results")[0].GetProperty("geometry").GetProperty("location").GetProperty("lat").GetDouble();
        //var lng = centerData.RootElement.GetProperty("results")[0].GetProperty("geometry").GetProperty("location").GetProperty("lng").GetDouble();

        var location = centerData.RootElement.GetProperty("items")[0].GetProperty("position");

        var cityCentreData = new CityCenterData {Latitude = location.GetProperty("lat").GetDouble(), Longitude = location.GetProperty("lng").GetDouble() };

        return cityCentreData;
    }

    public async Task<List<TrafficData>> GetTrafficForCityGridAsync(List<GridCell> gridCells)
    {
        var trafficDataList = new List<TrafficData>();

        foreach (var cell in gridCells)
        {
            var destinationLat = cell.Latitude + 0.01;
            var destinationLng = cell.Longitude + 0.01;

            var response = await _httpClient.GetAsync(
                $"https://router.hereapi.com/v8/routes?transportMode=car&origin={cell.Latitude},{cell.Longitude}&destination={destinationLat},{destinationLng}&return=summary&apiKey={_hereAPIKey}");

            var responseData = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

           
            if (responseData.RootElement.TryGetProperty("routes", out var routesElement) && routesElement.GetArrayLength() > 0)
            {
                
                var firstRoute = routesElement[0];

                if (firstRoute.TryGetProperty("sections", out var sectionsElement) && sectionsElement.GetArrayLength() > 0)
                {
                    var section = sectionsElement[0];

                    
                    if (section.TryGetProperty("summary", out var summaryElement))
                    {
                        
                        var duration = summaryElement.TryGetProperty("duration", out var durationElement)
                            ? durationElement.GetInt32()
                            : 0;

                        var baseDuration = summaryElement.TryGetProperty("baseDuration", out var baseDurationElement)
                            ? baseDurationElement.GetInt32()
                            : 0;

                        
                        var durationInTraffic = duration - baseDuration;

                        
                        trafficDataList.Add(new TrafficData
                        {
                            Latitude = cell.Latitude,
                            Longitude = cell.Longitude,
                            TrafficLevel = durationInTraffic
                        });
                    }
                    else
                    {
                        
                        trafficDataList.Add(new TrafficData
                        {
                            Latitude = cell.Latitude,
                            Longitude = cell.Longitude,
                            TrafficLevel = 0
                        });
                    }
                }
                else
                {
                    
                    trafficDataList.Add(new TrafficData
                    {
                        Latitude = cell.Latitude,
                        Longitude = cell.Longitude,
                        TrafficLevel = 0
                    });
                }
            }
            else
            {
                
                trafficDataList.Add(new TrafficData
                {
                    Latitude = cell.Latitude,
                    Longitude = cell.Longitude,
                    TrafficLevel = 0
                });
            }
        }

        return trafficDataList;
    }

    // Aggregation is under construction
    public async Task<List<TrafficData>> GetTrafficForCityAsync(string city, double latMin, double latMax, double lngMin, double lngMax, double gridSize)
    {
        double aggregationStep = 2;

        var gridCells = GenerateGrid(latMin, latMax, lngMin, lngMax, gridSize);

        var trafficData = await GetTrafficForCityGridAsync(gridCells);

        var aggregatedTrafficData = new List<TrafficData>();
        var supercellDict = new Dictionary<(int, int), List<TrafficData>>();

        // Group traffic data into supercells
        foreach (var data in trafficData)
        {
            // Calculate supercell indexes
            int supercellLatIndex = (int)((data.Latitude - latMin) / aggregationStep);
            int supercellLngIndex = (int)((data.Longitude - lngMin) / aggregationStep);

            var key = (supercellLatIndex, supercellLngIndex);

            // Add data to corresponding supercell
            if (!supercellDict.ContainsKey(key))
            {
                supercellDict[key] = new List<TrafficData>();
            }

            supercellDict[key].Add(data);
        }

        // Calculate aggregated traffic level for each supercell
        foreach (var entry in supercellDict)
        {
            var supercellData = entry.Value;
            var aggregatedTrafficLevel = supercellData.Average(t => t.TrafficLevel);

            var aggregatedCell = new TrafficData
            {
                Latitude = supercellData.Average(t => t.Latitude),
                Longitude = supercellData.Average(t => t.Longitude),
                TrafficLevel = aggregatedTrafficLevel
            };

            aggregatedTrafficData.Add(aggregatedCell);
        }

        return aggregatedTrafficData;
    }
}

public class TrafficData
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double TrafficLevel { get; set; }
}

public class CityCenterData
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class GridCell
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
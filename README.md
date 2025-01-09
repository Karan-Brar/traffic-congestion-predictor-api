This is a REST API that that provides traffic intensity levels for various points in a given city, used as a backend by the traffic monitoring dashboard repository - https://github.com/Karan-Brar/traffic-monitoring-dashboard

This is how the traffic data collection is implemented in this API -

1. The traffic data endpoint for the API will accept a city name.
2. The traffic data collection service will use the city name to get latitude and longitude for the centre point of the given city.
3. An internal method will use the long and lat position for the given city's centre and discover a 120+ points (lat, long) around the centre point with a defined distance interval.
4. The API will then make calls for all the discovered points to the [HERE API](https://www.here.com/developer) to get the time spent in traffic (in seconds) when going from a certain point to a slightly incremented version of the point on the map.
5. Using the returned traffic data we will the use the [Google Roads API](https://developers.google.com/maps/documentation/roads/overview) to snap all the points for which data is collected to the nearest point on an actual road for a more useful response.
6. Response is returned to client

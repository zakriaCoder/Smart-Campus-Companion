using System.Globalization;
using System.Text.Json;

namespace SmartCampus.Services;

public sealed class FreeMapService
{
    private readonly HttpClient httpClient;

    public FreeMapService(HttpClient httpClient)
    {
        this.httpClient = httpClient;
        this.httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("SmartCampusCompanion/1.0");
    }

    public async Task<RideRouteEstimate> EstimateRouteAsync(string pickup, string destination, CancellationToken cancellationToken = default)
    {
        pickup = NormalizePlace(pickup);
        destination = NormalizePlace(destination);

        try
        {
            var origin = await GeocodeAsync(pickup, cancellationToken);
            var drop = await GeocodeAsync(destination, cancellationToken);

            if (origin is null || drop is null)
                return EstimateFallback(pickup, destination, "Free map could not find one of the locations.");

            var route = await RouteAsync(origin.Value, drop.Value, cancellationToken);
            if (route is null)
                return EstimateFallback(pickup, destination, "Free OSRM routing is unavailable right now.");

            return new RideRouteEstimate(
                pickup,
                destination,
                Math.Round(route.Value.DistanceMeters / 1000, 1),
                Math.Max(1, (int)Math.Round(route.Value.DurationSeconds / 60)),
                true,
                "Free OpenStreetMap and OSRM route is active.",
                origin.Value.Lat,
                origin.Value.Lon,
                drop.Value.Lat,
                drop.Value.Lon,
                route.Value.Geometry);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            return EstimateFallback(pickup, destination, "Free map service is temporarily unavailable.");
        }
    }

    private async Task<MapPoint?> GeocodeAsync(string query, CancellationToken cancellationToken)
    {
        var url = "https://nominatim.openstreetmap.org/search"
            + $"?q={Uri.EscapeDataString(query)}"
            + "&format=json"
            + "&limit=1";

        using var response = await httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
            return null;

        var item = root[0];
        var lat = double.Parse(item.GetProperty("lat").GetString() ?? "0", CultureInfo.InvariantCulture);
        var lon = double.Parse(item.GetProperty("lon").GetString() ?? "0", CultureInfo.InvariantCulture);

        return new MapPoint(lat, lon);
    }

    private async Task<OsrmRoute?> RouteAsync(MapPoint origin, MapPoint drop, CancellationToken cancellationToken)
    {
        var coords = string.Create(
            CultureInfo.InvariantCulture,
            $"{origin.Lon},{origin.Lat};{drop.Lon},{drop.Lat}");

        var url = $"https://router.project-osrm.org/route/v1/driving/{coords}?overview=full&geometries=geojson";

        using var response = await httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var root = document.RootElement;
        if (!string.Equals(root.GetProperty("code").GetString(), "Ok", StringComparison.OrdinalIgnoreCase))
            return null;

        var route = root.GetProperty("routes")[0];
        var distance = route.GetProperty("distance").GetDouble();
        var duration = route.GetProperty("duration").GetDouble();
        var points = new List<MapPoint>();

        foreach (var coordinate in route.GetProperty("geometry").GetProperty("coordinates").EnumerateArray())
        {
            points.Add(new MapPoint(coordinate[1].GetDouble(), coordinate[0].GetDouble()));
        }

        return new OsrmRoute(distance, duration, points);
    }

    private static RideRouteEstimate EstimateFallback(string pickup, string destination, string note)
    {
        var seed = Math.Abs(HashCode.Combine(pickup.ToUpperInvariant(), destination.ToUpperInvariant()));
        var distance = Math.Round(7.5 + seed % 90 / 10.0, 1);
        var minutes = Math.Max(12, (int)Math.Round(distance * 2.2));

        return new RideRouteEstimate(
            pickup,
            destination,
            distance,
            minutes,
            false,
            note,
            33.6844,
            73.0479,
            33.7156,
            73.0266,
            new List<MapPoint>
            {
                new(33.6844, 73.0479),
                new(33.7156, 73.0266)
            });
    }

    private static string NormalizePlace(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Air University, E-9 Islamabad";

        value = value.Trim();
        return value.Contains("Islamabad", StringComparison.OrdinalIgnoreCase)
            ? value
            : $"{value}, Islamabad";
    }

    private readonly record struct OsrmRoute(double DistanceMeters, double DurationSeconds, IReadOnlyList<MapPoint> Geometry);
}

public readonly record struct MapPoint(double Lat, double Lon);

public sealed record RideRouteEstimate(
    string Pickup,
    string Destination,
    double DistanceKm,
    int DurationMin,
    bool UsedLiveRoute,
    string Note,
    double OriginLat,
    double OriginLon,
    double DestinationLat,
    double DestinationLon,
    IReadOnlyList<MapPoint> Geometry);

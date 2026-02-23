using System;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using CoordinateSharp;
using LenovoLegionToolkit.Lib.Settings;

namespace LenovoLegionToolkit.Lib.Utils;

public class SunriseSunset(SunriseSunsetSettings settings, HttpClientFactory httpClientFactory)
{
    public async Task<(Time?, Time?)> GetSunriseSunsetAsync(CancellationToken token = default)
    {
        var (sunrise, sunset) = (settings.Store.Sunrise, settings.Store.Sunset);
        if (settings.Store.LastCheckDateTime == DateTime.Today && sunrise is not null && sunset is not null)
            return (sunrise, sunset);

        var coordinate = await GetGeoLocationAsync(token).ConfigureAwait(false);

        if (coordinate is null)
            return (null, null);

        (sunrise, sunset) = CalculateSunriseSunset(coordinate);

        settings.Store.LastCheckDateTime = DateTime.Today;
        settings.Store.Sunrise = sunrise;
        settings.Store.Sunset = sunset;
        settings.SynchronizeStore();

        return (sunrise, sunset);
    }

    private async Task<Coordinate?> GetGeoLocationAsync(CancellationToken token)
    {
        try
        {
            using var httpClient = httpClientFactory.Create();
            var responseJson = await httpClient.GetStringAsync("http://ip-api.com/json?fields=lat,lon", token).ConfigureAwait(false);
            var responseJsonNode = JsonNode.Parse(responseJson);
            if (responseJsonNode is not null && double.TryParse(responseJsonNode["lat"]?.ToString(), out var lat) && double.TryParse(responseJsonNode["lon"]?.ToString(), out var lon))
                return new Coordinate(lat, lon, DateTime.UtcNow);
        }
        catch (Exception ex1)
        {
            try
            {
                TimeZoneInfo timeZoneInfo = TimeZoneInfo.Local;
                switch (timeZoneInfo.BaseUtcOffset.Hours)
                {
                    case 0: return new Coordinate(51.488879, -0.156489, DateTime.UtcNow);// London, United Kingdom
                    case 1: return new Coordinate(38.1, 9.2, DateTime.UtcNow);//  In the Mediterranean Sea between Sardinia and Tunisia
                    case 2: return new Coordinate(22.7, 32.2, DateTime.UtcNow);// Middle of the Lake Nasser (بحيرة ناصر) near the boarder of Egypt and Sudan
                    case 3: return new Coordinate(34.3, 42.2, DateTime.UtcNow);// Middle of the Hadithah Dam Lake (بحيرة سد حديثة) in north Eastern Iraq
                    case 4: return new Coordinate(35.9, 54.7, DateTime.UtcNow);// Middle of the Haj Aligholi Salt Lake (دریاچه نمک حاج علیقلی) in northern Iran
                    case 5: return new Coordinate(25.5, 77.88, DateTime.UtcNow);//Middle of the Madikheda Reservoir (मड़ीखेड़ा या अटल सागर बाँध) in central India 
                    case 6: return new Coordinate(23.8, 89.77, DateTime.UtcNow);//Middle of the Padma River in central Bangladesh
                    case 7: return new Coordinate(7.7, 104.9, DateTime.UtcNow);// Off of the southern coast of Vietnam
                    case 8: return new Coordinate(31.5, 117.5, DateTime.UtcNow);//Middle of Chao Lake (巢湖) in central China
                    case 9: return new Coordinate(20.4, 135.1, DateTime.UtcNow);//In the Philippine Sea
                    case 10: return new Coordinate(-18.5, 150, DateTime.UtcNow);//In the Coral Sea off the near the coast of Australia
                    case 11: return new Coordinate(-10, 164.4, DateTime.UtcNow);//In the Pacific Ocean near the Solomon Islands
                    case 12: return new Coordinate(-40.5,174, DateTime.UtcNow);// Middle of Cook Strait in the middle of New Zealand
                    case 13: return new Coordinate(-4.9, -169, DateTime.UtcNow);//Middle of nowhere in the Pacific Ocean (Why are you here? Even more so why are you looking at this?) 
                    case 14: return new Coordinate(-3.9, -155, DateTime.UtcNow);//Middle of nowhere in the Pacific Ocean
                    
                    case -1: return new Coordinate(10, -18.5, DateTime.UtcNow);// In the Atlantic Ocean off the west coast of Africa
                    case -2: return new Coordinate(10, -27.5, DateTime.UtcNow);// Middle of the Atlantic Ocean between Africa and South American
                    case -3: return new Coordinate(-25.3, -54.5, DateTime.UtcNow);//Middle of the Paraná River at the board of Brazil and Paraguay
                    case -4: return new Coordinate(-3.7, -66.3, DateTime.UtcNow);//Middle of the Amazon Rainforest in Brazil 
                    case -5: return new Coordinate(39.2, -76.3, DateTime.UtcNow);//In Chesapeake Bay Maryland, United States of America
                    case -6: return new Coordinate(39.2, -95.47, DateTime.UtcNow);//On Perry Lake Kansas, United States of America
                    case -7: return new Coordinate(41, -109.56, DateTime.UtcNow);// On Green River on the boarder between Utah and Wyoming
                    case -8: return new Coordinate(39.1, -120.1, DateTime.UtcNow);//On Lake Tahoe on the boarder between California and Utah
                    case -9: return new Coordinate(61.1, -150.6, DateTime.UtcNow);//Off the coast of Anchorage Alaska, United States of America
                    case -10: return new Coordinate(20.5, -157, DateTime.UtcNow);// Off the coast of the Hawaii, United States of America
                    case -11: return new Coordinate(16, -171, DateTime.UtcNow);// Middle of the Pacific Ocean
                    case -12: return new Coordinate(16, -177, DateTime.UtcNow);// Middle of the Pacific Ocean
                    
                    default: return new Coordinate(52.45214434133201, -1.733180131094635, DateTime.UtcNow);// Go to Birmingham
                }
            }
            catch (Exception ex2)
            {
                Log.Instance.Trace($"Failed to get geolocation.", ex1);
                Log.Instance.Trace($"Failed to get geolocation from timezone backup {ex2}");
            }
        }

        return null;
    }

    private static (Time?, Time?) CalculateSunriseSunset(Coordinate coordinate)
    {
        var sunrise = coordinate.CelestialInfo.SunRise;
        var sunset = coordinate.CelestialInfo.SunSet;

        if (sunrise is null || sunset is null)
            return (null, null);

        return (new Time(sunrise.Value.Hour, sunrise.Value.Minute), new Time(sunset.Value.Hour, sunset.Value.Minute));
    }
}

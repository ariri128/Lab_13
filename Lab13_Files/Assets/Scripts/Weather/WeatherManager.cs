using UnityEngine;
using System;
using System.Collections;
using System.Globalization;
using UnityEngine.Networking;

/*
 * Talks to the OpenWeatherMap "Current weather data" API and hands the raw response back through a callback
*/

public class WeatherManager
{
    private const string WeatherApi = "https://api.openweathermap.org/data/2.5/weather";

    private readonly string _apiKey;
    private readonly string _units;

    public WeatherManager(string apiKey, string units)
    {
        _apiKey = apiKey;
        _units = units;
    }

    // Sends a GET request, waits for it to finish, then sends the response text back through the callback
	// Errors are logged and the callback is not called
    private IEnumerator CallAPI(string url, Action<string> callback)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.LogError($"network problem: {request.error}");
            }
            else if (request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"response error: {request.responseCode} (401 usually means the API key is wrong or not active yet)");
            }
            else if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"request failed: {request.error}");
            }
            else
            {
                callback?.Invoke(request.downloadHandler.text);
            }
        }
    }

    // Requests the current weather for a city as JSON, using its latitude/longitude
    // (OpenWeather has deprecated lookups by city name)
    public IEnumerator GetWeatherJSON(CityLocation city, Action<string> callback)
    {
        return CallAPI(BuildUrl(city, "json"), callback);
    }

    // Same request as GetWeatherJSON but asks for XML, matching the lab's starter code
    public IEnumerator GetWeatherXML(CityLocation city, Action<string> callback)
    {
        return CallAPI(BuildUrl(city, "xml"), callback);
    }

    // Builds the request URL. InvariantCulture keeps decimals as "28.53", never "28,53"
    private string BuildUrl(CityLocation city, string mode)
    {
        string lat = city.Latitude.ToString(CultureInfo.InvariantCulture);
        string lon = city.Longitude.ToString(CultureInfo.InvariantCulture);
        return $"{WeatherApi}?lat={lat}&lon={lon}&units={_units}&mode={mode}&appid={_apiKey}";
    }
}
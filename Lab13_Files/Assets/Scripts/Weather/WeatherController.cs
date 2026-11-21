using UnityEngine;
using System;
using System.Collections.Generic;

/*
 * Owns the WeatherManager, keeps the list of cities and the current selection, and announces new weather through the WeatherUpdated event (Observer pattern)
 * Anything that reacts to weather (sky, sun, UI, analytics) subscribes to that event
*/

public class WeatherController : MonoBehaviour
{
    [Header("OpenWeatherMap")]
    [Tooltip("Text file that contains only your API key. Keep it out of GitHub (.gitignore).")]
    [SerializeField] private TextAsset _apiKeyFile;
    [Tooltip("metric = Celsius, imperial = Fahrenheit, standard = Kelvin")]
    [SerializeField] private string _units = "metric";

    [Header("Cities")]
    [SerializeField]
    private CityLocation[] _cities =
    {
        new CityLocation("Orlando", 28.5384f, -81.3789f),
        new CityLocation("Reykjavik", 64.1466f, -21.9426f),
        new CityLocation("Tokyo", 35.6762f, 139.6503f),
        new CityLocation("Sydney", -33.8688f, 151.2093f),
        new CityLocation("Dubai", 25.2048f, 55.2708f)
    };
    [HideInInspector] [SerializeField] private int _selectedCityIndex;

    private WeatherManager _weatherManager;
    private int _latestRequestId;

    public event Action<CityLocation, WeatherData> WeatherUpdated;

    public IReadOnlyList<CityLocation> Cities => _cities;
    public int SelectedCityIndex => _selectedCityIndex;
    public string Units => _units;

    // Reads the API key from the key file and creates the WeatherManager
    private void Awake()
    {
        if (_apiKeyFile == null || string.IsNullOrWhiteSpace(_apiKeyFile.text))
        {
            Debug.LogError("WeatherController: assign a TextAsset that contains your OpenWeatherMap API key.", this);
            return;
        }

        _weatherManager = new WeatherManager(_apiKeyFile.text.Trim(), _units);
    }

    private void Start()
    {
        SelectCity(_selectedCityIndex);
    }

    // Switches to the city at the given index and requests its weather
    // Called by the UI dropdown and by the custom inspector
    public void SelectCity(int index)
    {
        if (_cities == null || index < 0 || index >= _cities.Length)
        {
            Debug.LogWarning($"WeatherController: city index {index} is out of range.", this);
            return;
        }

        _selectedCityIndex = index;

        if (_weatherManager == null)
        {
            return;
        }

        CityLocation city = _cities[index];
        int requestId = ++_latestRequestId;
        StartCoroutine(_weatherManager.GetWeatherJSON(city, data => OnJSONDataLoaded(data, city, requestId)));
    }

    // Re-requests the current city's weather from the component's right-click menu
    [ContextMenu("Refresh Weather")]
    private void RefreshWeather()
    {
        SelectCity(_selectedCityIndex);
    }

    // Parses the JSON and tells subscribers about it. Responses from an older request (the user switched cities while it was loading) are ignored
    private void OnJSONDataLoaded(string data, CityLocation city, int requestId)
    {
        if (requestId != _latestRequestId)
        {
            return;
        }

        Debug.Log(data);

        WeatherData weather = JsonUtility.FromJson<WeatherData>(data);
        if (weather == null || weather.main == null || weather.sys == null)
        {
            Debug.LogError("WeatherController: could not parse the weather response.", this);
            return;
        }

        WeatherUpdated?.Invoke(city, weather);
    }
}
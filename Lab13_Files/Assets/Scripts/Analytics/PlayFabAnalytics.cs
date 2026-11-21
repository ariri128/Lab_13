using UnityEngine;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;

/*
 * PlayFab integration: logs in anonymously with this device's ID, then sends a PlayStream event every time a city's weather loads
 * Events show up in Game Manager under Players > (your player) > PlayStream
*/

public class PlayFabAnalytics : MonoBehaviour
{
    [SerializeField] private WeatherController _weatherController;
    [SerializeField] private SceneWeatherApplier _sceneWeatherApplier;
    [SerializeField] private string _cityEventName = "city_weather_viewed";

    private bool _isLoggedIn;
    private Dictionary<string, object> _pendingEventBody;

    private void OnEnable()
    {
        if (_weatherController != null)
        {
            _weatherController.WeatherUpdated += OnWeatherUpdated;
        }
    }

    private void OnDisable()
    {
        if (_weatherController != null)
        {
            _weatherController.WeatherUpdated -= OnWeatherUpdated;
        }
    }

    // Logs in with a custom ID - CreateAccount makes a new player the first time
    private void Start()
    {
        if (string.IsNullOrEmpty(PlayFabSettings.staticSettings.TitleId))
        {
            Debug.LogError("PlayFabAnalytics: set your Title ID in the PlayFab settings asset first.", this);
            return;
        }

        LoginWithCustomIDRequest request = new LoginWithCustomIDRequest
        {
            CustomId = SystemInfo.deviceUniqueIdentifier,
            CreateAccount = true
        };
        PlayFabClientAPI.LoginWithCustomID(request, OnLoginSuccess, OnPlayFabError);
    }

    // Sends any event that arrived before login finished (usually the first city)
    private void OnLoginSuccess(LoginResult result)
    {
        _isLoggedIn = true;
        Debug.Log($"PlayFab login OK. PlayFabId: {result.PlayFabId}");

        if (_pendingEventBody != null)
        {
            SendCityEvent(_pendingEventBody);
            _pendingEventBody = null;
        }
    }

    // Packs the city's weather into an event body and sends it (or holds it until login)
    private void OnWeatherUpdated(CityLocation city, WeatherData data)
    {
        Dictionary<string, object> body = new Dictionary<string, object>
        {
            { "city", city.DisplayName },
            { "temperature", data.main.temp },
            { "condition", data.PrimaryCondition != null ? data.PrimaryCondition.main : "unknown" },
            { "sky", _sceneWeatherApplier != null ? _sceneWeatherApplier.EvaluateCondition(data).ToString() : "unknown" },
            { "cityLocalTime", data.GetCityTime().ToString("HH:mm") }
        };

        if (_isLoggedIn)
        {
            SendCityEvent(body);
        }
        else
        {
            _pendingEventBody = body;
        }
    }

    private void SendCityEvent(Dictionary<string, object> body)
    {
        WriteClientPlayerEventRequest request = new WriteClientPlayerEventRequest
        {
            EventName = _cityEventName,
            Body = body
        };
        PlayFabClientAPI.WritePlayerEvent(request, result => Debug.Log($"PlayFab event sent: {_cityEventName} ({body["city"]})"), OnPlayFabError);
    }

    private void OnPlayFabError(PlayFabError error)
    {
        Debug.LogError($"PlayFab error: {error.GenerateErrorReport()}");
    }
}
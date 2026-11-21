using UnityEngine;
using System.Collections.Generic;
using TMPro;

/*
 * Simple on-screen UI: a dropdown to pick the city and a label that shows the city's temperature, conditions, local time and time difference from this PC
*/

public class CityWeatherUI : MonoBehaviour
{
    [SerializeField] private WeatherController _weatherController;
    [SerializeField] private SceneWeatherApplier _sceneWeatherApplier;
    [SerializeField] private TMP_Dropdown _cityDropdown;
    [SerializeField] private TMP_Text _infoLabel;
    [SerializeField] private string _loadingText = "Loading weather...";

    private void OnEnable()
    {
        if (_weatherController != null)
        {
            _weatherController.WeatherUpdated += OnWeatherUpdated;
        }

        if (_cityDropdown != null)
        {
            _cityDropdown.onValueChanged.AddListener(OnCitySelected);
        }
    }

    private void OnDisable()
    {
        if (_weatherController != null)
        {
            _weatherController.WeatherUpdated -= OnWeatherUpdated;
        }

        if (_cityDropdown != null)
        {
            _cityDropdown.onValueChanged.RemoveListener(OnCitySelected);
        }
    }

    // Fills the dropdown with the city names from the WeatherController
    private void Start()
    {
        if (_weatherController == null || _cityDropdown == null)
        {
            Debug.LogError("CityWeatherUI: assign the WeatherController and the city dropdown.", this);
            return;
        }

        List<string> names = new List<string>();
        foreach (CityLocation city in _weatherController.Cities)
        {
            names.Add(city.DisplayName);
        }

        _cityDropdown.ClearOptions();
        _cityDropdown.AddOptions(names);
        _cityDropdown.SetValueWithoutNotify(_weatherController.SelectedCityIndex);
        SetLabel(_loadingText);
    }

    private void OnCitySelected(int index)
    {
        SetLabel(_loadingText);
        _weatherController.SelectCity(index);
    }

    // Updates the label, and keeps the dropdown in sync when the city was changed from the Inspector instead of the UI
    private void OnWeatherUpdated(CityLocation city, WeatherData data)
    {
        if (_cityDropdown != null)
        {
            _cityDropdown.SetValueWithoutNotify(_weatherController.SelectedCityIndex);
        }

        string unitSymbol = _weatherController.Units == "imperial" ? "°F" : _weatherController.Units == "metric" ? "°C" : "K";
        string description = data.PrimaryCondition != null ? data.PrimaryCondition.description : "unknown";
        string sky = _sceneWeatherApplier != null ? _sceneWeatherApplier.CurrentCondition.ToString() : "-";
        double hours = data.GetHoursFromLocal();

        SetLabel($"{city.DisplayName}, {data.sys.country}\n" +
                 $"{data.main.temp:0.#}{unitSymbol} - {description}\n" +
                 $"Local time: {data.GetCityTime():HH:mm} ({hours:+0.#;-0.#;0}h from you)\n" +
                 $"Sky: {sky}");
    }

    private void SetLabel(string text)
    {
        if (_infoLabel != null)
        {
            _infoLabel.text = text;
        }
    }
}
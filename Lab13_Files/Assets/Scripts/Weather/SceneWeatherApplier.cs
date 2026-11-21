using UnityEngine;
using System;

/*
 * The eight chosen skyboxes in the Fantasy Skybox FREE pack
*/

public enum SkyCondition
{
    Day,
    Night,
    Sunny,
    Cloudy,
    Rainy,
    Snowy,
    Sunrise,
    Sunset
}

// The skybox material, sun brightness and sun color used for one SkyCondition
[Serializable]
public class SkyProfile
{
    public SkyCondition Condition;
    public Material Skybox;
    [Range(0f, 2f)] public float SunIntensity = 1f;
    public Color SunColor = Color.white;

    public SkyProfile()
    {
    }

    public SkyProfile(SkyCondition condition, float sunIntensity, Color sunColor)
    {
        Condition = condition;
        SunIntensity = sunIntensity;
        SunColor = sunColor;
    }
}

// Listens for new weather and updates the scene: picks a skybox from the weather code and the city's sunrise/sunset, sets the sun's intensity and color (tinted by temperature), and rotates the sun to match the city's time of day
public class SceneWeatherApplier : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WeatherController _weatherController;
    [SerializeField] private Light _sun;

    [Header("Sky Profiles (drag the matching skybox material into each)")]
    [SerializeField]
    private SkyProfile[] _profiles =
    {
        new SkyProfile(SkyCondition.Day, 1.0f, new Color(1f, 0.96f, 0.88f)),
        new SkyProfile(SkyCondition.Night, 0.15f, new Color(0.55f, 0.65f, 1f)),
        new SkyProfile(SkyCondition.Sunny, 1.3f, new Color(1f, 0.95f, 0.8f)),
        new SkyProfile(SkyCondition.Cloudy, 0.6f, new Color(0.85f, 0.88f, 0.92f)),
        new SkyProfile(SkyCondition.Rainy, 0.4f, new Color(0.7f, 0.75f, 0.85f)),
        new SkyProfile(SkyCondition.Snowy, 0.7f, new Color(0.9f, 0.95f, 1f)),
        new SkyProfile(SkyCondition.Sunrise, 0.8f, new Color(1f, 0.7f, 0.45f)),
        new SkyProfile(SkyCondition.Sunset, 0.8f, new Color(1f, 0.55f, 0.35f))
    };

    [Header("Time of Day")]
    [Tooltip("Minutes either side of sunrise/sunset that count as Sunrise/Sunset.")]
    [SerializeField] private float _twilightMinutes = 45f;
    [Tooltip("Sun X rotation used at night (moonlight from above).")]
    [SerializeField] private float _nightSunAngle = 50f;
    [Tooltip("Brightness multiplier for rain/snow skies when it is night in the city.")]
    [SerializeField] [Range(0f, 1f)] private float _nightLightFactor = 0.25f;

    [Header("Temperature Tint")]
    [SerializeField] private float _coldTemperature = -5f;
    [SerializeField] private float _hotTemperature = 35f;
    [SerializeField]
    private Gradient _temperatureTint = new Gradient
    {
        colorKeys = new[]
        {
            new GradientColorKey(new Color(0.7f, 0.85f, 1f), 0f),
            new GradientColorKey(Color.white, 0.5f),
            new GradientColorKey(new Color(1f, 0.75f, 0.5f), 1f)
        }
    };
    [SerializeField] [Range(0f, 1f)] private float _temperatureInfluence = 0.35f;

    public SkyCondition CurrentCondition { get; private set; }

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

    // Applies the skybox, sun light and sun angle for the new weather
    private void OnWeatherUpdated(CityLocation city, WeatherData data)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        bool isDaytime = IsDaytime(data, now);
        CurrentCondition = ClassifySky(data, now, isDaytime);

        SkyProfile profile = FindProfile(CurrentCondition);
        if (profile == null)
        {
            Debug.LogWarning($"SceneWeatherApplier: no profile set up for {CurrentCondition}.", this);
            return;
        }

        if (profile.Skybox != null)
        {
            RenderSettings.skybox = profile.Skybox;
            DynamicGI.UpdateEnvironment();
        }
        else
        {
            Debug.LogWarning($"SceneWeatherApplier: the {CurrentCondition} profile has no skybox material.", this);
        }

        if (_sun == null)
        {
            Debug.LogWarning("SceneWeatherApplier: no sun (Directional Light) assigned.", this);
            return;
        }

        bool isWeatherSky = CurrentCondition == SkyCondition.Rainy || CurrentCondition == SkyCondition.Snowy;
        float daylight = isWeatherSky && !isDaytime ? _nightLightFactor : 1f;

        float temperatureT = Mathf.InverseLerp(_coldTemperature, _hotTemperature, data.main.temp);
        Color tint = _temperatureTint.Evaluate(temperatureT);

        _sun.intensity = profile.SunIntensity * daylight;
        _sun.color = Color.Lerp(profile.SunColor, profile.SunColor * tint, _temperatureInfluence);
        _sun.transform.rotation = Quaternion.Euler(GetSunAngle(data, now, isDaytime), _sun.transform.eulerAngles.y, 0f);
        RenderSettings.sun = _sun;
    }

    // Works out the SkyCondition for this weather right now
    // Other scripts (UI, analytics) call this so they never depend on which script received the event first
    public SkyCondition EvaluateCondition(WeatherData data)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return ClassifySky(data, now, IsDaytime(data, now));
    }

    // Turns the OpenWeather condition code and the time of day into a SkyCondition
    // Codes: 2xx thunderstorm, 3xx drizzle, 5xx rain, 6xx snow, 7xx fog/haze, 800 clear, 801-802 few/scattered clouds, 803-804 broken/overcast
    private SkyCondition ClassifySky(WeatherData data, long now, bool isDaytime)
    {
        int code = data.PrimaryCondition != null ? data.PrimaryCondition.id : 800;
        long twilight = (long)(_twilightMinutes * 60f);

        if (code >= 600 && code < 700)
        {
            return SkyCondition.Snowy;
        }

        if (code < 600)
        {
            return SkyCondition.Rainy;
        }

        if (data.sys.sunrise > 0 && Math.Abs(now - data.sys.sunrise) <= twilight)
        {
            return SkyCondition.Sunrise;
        }

        if (data.sys.sunset > 0 && Math.Abs(now - data.sys.sunset) <= twilight)
        {
            return SkyCondition.Sunset;
        }

        if (!isDaytime)
        {
            return SkyCondition.Night;
        }

        if (code == 800)
        {
            return SkyCondition.Sunny;
        }

        return code >= 803 || code < 800 ? SkyCondition.Cloudy : SkyCondition.Day;
    }

    // True when the current time is between the city's sunrise and sunset
    private bool IsDaytime(WeatherData data, long now)
    {
        return data.sys.sunrise > 0 && data.sys.sunset > 0 && now >= data.sys.sunrise && now <= data.sys.sunset;
    }

    // Sun X rotation: 0° at sunrise, 90° (straight down) at midday, 180° at sunset
    private float GetSunAngle(WeatherData data, long now, bool isDaytime)
    {
        if (!isDaytime)
        {
            return _nightSunAngle;
        }

        float progress = Mathf.InverseLerp(data.sys.sunrise, data.sys.sunset, now);
        return Mathf.Lerp(0f, 180f, progress);
    }

    private SkyProfile FindProfile(SkyCondition condition)
    {
        if (_profiles == null)
        {
            return null;
        }

        foreach (SkyProfile profile in _profiles)
        {
            if (profile != null && profile.Condition == condition)
            {
                return profile;
            }
        }

        return null;
    }
}
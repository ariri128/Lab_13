using System;

/*
 * Shape of the OpenWeatherMap JSON response, parsed with JsonUtility
 * Field names are lowercase on purpose: JsonUtility matches them to the JSON keys exactly, so they can't follow the usual PascalCase rule
*/

[Serializable]
public class WeatherData
{
    public WeatherCondition[] weather;
    public MainInfo main;
    public SysInfo sys;
    public int timezone;
    public string name;

    public WeatherCondition PrimaryCondition =>
        weather != null && weather.Length > 0 ? weather[0] : null;

    // The city's current clock time, worked out from the PC clock (DateTime.Now): remove this PC's UTC offset, then add the city's offset from the API
    public DateTime GetCityTime()
    {
        DateTime now = DateTime.Now;
        TimeSpan localOffset = TimeZoneInfo.Local.GetUtcOffset(now);
        return now - localOffset + TimeSpan.FromSeconds(timezone);
    }

    // How many hours the city is ahead of (+) or behind (-) this PC
    public double GetHoursFromLocal()
    {
        TimeSpan localOffset = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now);
        return (TimeSpan.FromSeconds(timezone) - localOffset).TotalHours;
    }
}

[Serializable]
public class WeatherCondition
{
    public int id;
    public string main;
    public string description;
}

[Serializable]
public class MainInfo
{
    public float temp;
    public float feels_like;
    public int humidity;
}

[Serializable]
public class SysInfo
{
    public string country;
    public long sunrise;
    public long sunset;
}
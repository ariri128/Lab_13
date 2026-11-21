using UnityEngine;
using System;

/*
 * One selectable city: the name shown in the UI plus the coordinates sent to the API
*/

[Serializable]
public struct CityLocation
{
    [SerializeField] private string _displayName;
    [SerializeField] private float _latitude;
    [SerializeField] private float _longitude;

    public string DisplayName => _displayName;
    public float Latitude => _latitude;
    public float Longitude => _longitude;

    public CityLocation(string displayName, float latitude, float longitude)
    {
        _displayName = displayName;
        _latitude = latitude;
        _longitude = longitude;
    }
}
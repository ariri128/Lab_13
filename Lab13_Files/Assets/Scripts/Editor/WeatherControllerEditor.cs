using UnityEngine;
using UnityEditor;

/*
 * Custom Inspector for WeatherController: draws the default fields, then adds a city popup and a refresh button
 * Changing the city in Play Mode updates the scene straight away
*/

[CustomEditor(typeof(WeatherController))]
public class WeatherControllerEditor : Editor
{
    private SerializedProperty _selectedCityIndex;

    private void OnEnable()
    {
        _selectedCityIndex = serializedObject.FindProperty("_selectedCityIndex");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        WeatherController controller = (WeatherController)target;
        if (controller.Cities == null || controller.Cities.Count == 0 || _selectedCityIndex == null)
        {
            return;
        }

        string[] cityNames = new string[controller.Cities.Count];
        for (int i = 0; i < cityNames.Length; i++)
        {
            cityNames[i] = controller.Cities[i].DisplayName;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("City Switcher", EditorStyles.boldLabel);

        serializedObject.Update();
        int currentIndex = Mathf.Clamp(_selectedCityIndex.intValue, 0, cityNames.Length - 1);
        int newIndex = EditorGUILayout.Popup("Current City", currentIndex, cityNames);

        if (newIndex != currentIndex)
        {
            if (Application.isPlaying)
            {
                controller.SelectCity(newIndex);
            }
            else
            {
                _selectedCityIndex.intValue = newIndex;
                serializedObject.ApplyModifiedProperties();
            }
        }

        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            if (GUILayout.Button("Refresh Weather (Play Mode)"))
            {
                controller.SelectCity(controller.SelectedCityIndex);
            }
        }
    }
}
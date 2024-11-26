using UnityEngine;

public class LightManager : MonoBehaviour
{
    [Header("Singleton")]
    public static LightManager Instance;

    [field: Header("Lighting Profile")]
    [field: SerializeField] public LightingProfile Profile{ get; private set;}

    [Header("Light Components")]
    [SerializeField] private Transform lightTransform;
    [SerializeField] private Light directionalLight;

    #region Initialization Methods

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else Destroy(Instance);
    }
    #endregion

    public void EvaluateTime(float timeOfDay)
    {
        // Rotate the light based on the time of day
        float lightAngle = Mathf.Lerp(0f, 360f, timeOfDay) - 90f;

        // Determine if it's day or night
        bool isDay = IsDay(timeOfDay);

        // Evaluate lighting properties
        directionalLight.color = Profile.LightingColorOverDay.Evaluate(timeOfDay);
        directionalLight.intensity = Profile.LightIntensityOverDay.Evaluate(timeOfDay);

        // Light flipping for time of day
        lightTransform.rotation = Quaternion.Euler(isDay ? lightAngle : lightAngle + 180f, 0f, 0f);
        RenderSettings.skybox.SetInt("_IsDay", isDay ? 1 : 0);
    }

    private bool IsDay(float timeOfDay)
    {
        float dayStart = Profile.DayStart;
        float dayEnd = Profile.DayEnd;

        return timeOfDay >= dayStart && timeOfDay <= dayEnd;
    }
}

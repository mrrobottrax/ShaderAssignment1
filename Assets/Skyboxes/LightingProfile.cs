using UnityEngine;

[CreateAssetMenu(fileName = "Lighting Profile", menuName = "Lighting Profile", order = 1)]
public class LightingProfile : ScriptableObject
{
    [field: Header("Day and Night Settings")]
    [field: SerializeField, Range(0f, 1f)] public float DayStart { get; private set; } = 0.25f; // 6:00 AM
    [field: SerializeField, Range(0f, 1f)] public float DayEnd { get; private set; } = 0.75f;   // 6:00 PM

    [field: SerializeField] public Gradient LightingColorOverDay { get; private set; }
    [field: SerializeField] public Gradient ShadowColorOverDay { get; private set; }
    [field: SerializeField] public AnimationCurve LightIntensityOverDay { get; private set; }
}

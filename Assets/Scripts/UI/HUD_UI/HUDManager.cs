using UnityEngine;

public class HUDManager : MonoBehaviour
{
    [field: SerializeField] public HUD_HealthBar HealthBar { get; private set; }
    [field: SerializeField] public HUD_AmmoDisplay AmmoDisplay { get; private set; }
}

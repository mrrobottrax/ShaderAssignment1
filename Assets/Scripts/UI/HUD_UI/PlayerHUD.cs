using UnityEngine;

public class PlayerHUD : MonoBehaviour
{
    [field: SerializeField] public HUD_O2Bar O2Bar { get; private set; }
    [field: SerializeField] public HUD_AmmoDisplay AmmoDisplay { get; private set; }
}

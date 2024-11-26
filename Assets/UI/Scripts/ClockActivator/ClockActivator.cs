using UnityEngine;

public class ClockActivator : MonoBehaviour
{
    [field: SerializeField] public bool IsActive { get; private set; }

    public void SetActivatorActive(bool isActive)
    {
        IsActive = isActive;
    }
}

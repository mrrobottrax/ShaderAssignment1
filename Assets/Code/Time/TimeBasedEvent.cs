using UnityEngine;
using UnityEngine.Events;

public class TimeBasedEvent : MonoBehaviour
{
    [Header("Event Time")]
    [SerializeField] private int eventMin;
    [SerializeField] private int eventHr;

    [Header("Event")]
    [SerializeField] private UnityEvent unityEvent;

    private void Start()
    {
        CycleManager.Instance.OnTimeAdvance += EvaluateTime;
    }

    private void OnDisable()
    {
        CycleManager.Instance.OnTimeAdvance -= EvaluateTime;
    }

    private void EvaluateTime(int min, int hr)
    {
        if (eventMin == min && eventHr == hr)
            unityEvent?.Invoke();
    }
}

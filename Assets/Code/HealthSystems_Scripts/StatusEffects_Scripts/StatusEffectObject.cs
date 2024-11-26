using UnityEngine;

[System.Serializable]
public class StatusEffectObject
{
    private StatusEffectData_Base effectData;
    private ITickingStatusEffectData timingData;

    [Header("System")]
    private float durationRemaining;
    private float secondsUntilTick;

    public StatusEffectObject(StatusEffectData_Base statusEffectData, ITickingStatusEffectData tickingData)
    {
        effectData = statusEffectData;
        timingData = tickingData;

        durationRemaining = timingData.Duration;

        secondsUntilTick = 0f;
    }

    #region Timer Methods

    /// <summary>
    /// Sets the total duration of time remaining before the effect is over
    /// </summary>
    public void SetDurationRemaining(float value)
    {
        durationRemaining = value;
    }

    /// <summary>
    /// Sets the time remaining before the next tick
    /// </summary>
    public void SetSecondsUntilTick(float value)
    {
        secondsUntilTick = value;
    }
    #endregion


    #region Helper Methods
    public StatusEffectData_Base GetEffectBaseData()
    {
        return effectData;
    }

    public ITickingStatusEffectData GetEffectTimingData()
    {
        return timingData;
    }


    /// <summary>
    /// Returns the total of time duration remaining before the effect is over
    /// </summary>
    public float GetDurationRemaining()
    {
        return durationRemaining;
    }

    /// <summary>
    /// Returns the time remaining before the next tick
    /// </summary>
    public float GetSecondsUntilTick()
    {
        return secondsUntilTick;
    }

    #endregion
}

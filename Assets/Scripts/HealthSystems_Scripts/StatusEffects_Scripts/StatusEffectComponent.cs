using System.Collections.Generic;
using UnityEngine;

public class StatusEffectComponent : MonoBehaviour
{
    private List<StatusEffectObject> activeEffects = new List<StatusEffectObject>();
    private IHealthComponent healthBase;

    private void Awake()
    {
        healthBase = GetComponent<IHealthComponent>();
    }

    /// <summary>
    /// This method adds a status effect to the active effects list
    /// </summary>
    public void AddStatusEffect(StatusEffectData_Base statusEffect)
    {
        if (statusEffect == null)
            return;

        // Check if the effect data is a ticking effect
        if(statusEffect is ITickingStatusEffectData tickingEffect)
        {
            // Check if the effect can be stacked and if there are already instances of this effect type present in the active effects list
            if(!tickingEffect.IsStackable)
                foreach (StatusEffectObject activeEffect in activeEffects)
                {
                    // Check if the type is the same or derived from StatusEffect
                    if (typeof(StatusEffectObject).IsAssignableFrom(activeEffect.GetType()))
                        return;
                }

            // Create a new instance of the StatusEffect based on the data
            StatusEffectObject effect = tickingEffect.CreateEffectInstance();
            activeEffects.Add(effect);

            // Enable the update loop when a ticking effect is added
            enabled = true;
        }

        // Apply the status effect to the entity
        statusEffect.OnApplyEffect(healthBase);
    }

    /// <summary>
    /// This method adds an array of status effects to the active effects list
    /// </summary>
    public void AddStatusEffect(StatusEffectData_Base[] statusEffect)
    {
        if (statusEffect != null)
            foreach (StatusEffectData_Base i in statusEffect)
            {
                AddStatusEffect(i);
            }
    }

    /// <summary>
    /// This method applies on remove effects to entity then removes the effect from the active effects list
    /// </summary>
    public void RemoveStatusEffect(StatusEffectObject statusEffect)
    {
        if (activeEffects.Contains(statusEffect))
        {
            statusEffect.GetEffectBaseData().OnRemoveEffect(healthBase);

            activeEffects.Remove(statusEffect);
        }
    }

    /// <summary>
    /// This method updates all of the active status effects
    /// </summary>
    private void TickStatusEffects()
    {
        for (int i = 0; i < activeEffects.Count; i++)
        {
            // Cache the effect object and its data
            StatusEffectObject effect = activeEffects[i];
            ITickingStatusEffectData effectData = effect.GetEffectTimingData();

            // Cahce tick speed & duration
            float tickSpeed = effectData.TickSpeed;
            float duration = effectData.Duration;

            // Calculate time until the next tick
            float newTickDur = effect.GetSecondsUntilTick();
            newTickDur -= Time.deltaTime;
            newTickDur = Mathf.Clamp(newTickDur, 0, tickSpeed);

            // Check if the amount of time between ticks has been met
            if (newTickDur <= 0f)
            {
                // Tick effect and reset timer
                effectData.TickEffect(healthBase);
                effect.SetSecondsUntilTick(tickSpeed);
            }
            else // Continue to count down
                effect.SetSecondsUntilTick(newTickDur);

            // Tick the effect if possible
            if (!effectData.IsInfinite)
            {
                // Calculate the new effect duration
                float newDuration = effect.GetDurationRemaining();
                newDuration -= Time.deltaTime;
                newDuration = Mathf.Clamp(newDuration, 0, duration);

                // Remove the effect if the new effect duration is below zero
                if (newDuration <= 0f)
                {
                    // Remove the status effect
                    RemoveStatusEffect(effect);

                    // Continue to avoid a null ref
                    continue;
                }

                // Set the new effect duration if above zero seconds remaining
                effect.SetDurationRemaining(newDuration);
            }
        }

        // Disable update loop when unneeded.
        if (activeEffects.Count <= 0)
            enabled = false;
    }

    private void Update()
    {
        TickStatusEffects();
    }
}

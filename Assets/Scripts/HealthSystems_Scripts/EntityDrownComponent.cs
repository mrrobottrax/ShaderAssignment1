using UnityEngine;

[RequireComponent(typeof(Entity_Base))]
public class EntityDrownComponent : MonoBehaviour
{
    [Header("Drowning Parameters")]
    [SerializeField] private float _secondsBeforeDrowning = 10f;
    [SerializeField] private float _secondsBetweenTick = .25f;
    [SerializeField] private float _tickRateMultiplier = 1f;
    [SerializeField] private int _healthRemovedOnTick = 1;

    [Header("Components")]
    private Entity_Base entity;

    [Header("System")]
    private bool isUnderwater = false;
    private float timeUnderwater;
    private float timeSincePrevTick;

    private void Awake()
    {
        entity = GetComponent<Entity_Base>();
    }

    #region Submerged State Methods

    /// <summary>
    /// This method should be called by the entity when it becomes submerged
    /// </summary>
    public void EntitySubmerged()
    {
        isUnderwater = true;
    }

    /// <summary>
    /// This method should be called by the entity when it no longer underwater
    /// </summary>
    public void EntityAboveWater()
    {
        isUnderwater = false;
        timeUnderwater = 0;
        timeSincePrevTick = 0;
    }
    #endregion

    #region Update Methods
    private void Update()
    {
        // When the player is underwater count up
        if (isUnderwater)
        {
            timeUnderwater += Time.deltaTime;

            // Once they run out of air begin the damage tickdown
            if (timeUnderwater > _secondsBeforeDrowning)
            {
                timeSincePrevTick += Time.deltaTime * _tickRateMultiplier;

                // Once the time elapsed since the last tick reaches the threshold, reset clock and deal damage
                if(timeSincePrevTick > _secondsBetweenTick)
                {
                    entity.RemoveHealth(_healthRemovedOnTick);
                    timeSincePrevTick = 0;
                }
            }
        }
    }
    #endregion
}

using UnityEngine;

[RequireComponent(typeof(Entity_Base))]
public class EntityFallDamageComponent : MonoBehaviour
{
    [Header("Fall Damage Parameters")]
    [SerializeField] private float _velocityThreshold = 15f;
    [SerializeField] private float _timeThreshold = 0.7f;
    [SerializeField] private float _timeMultiplier = 2.2f;

    [Header("Components")]
    private Entity_Base entity;
    private Rigidbody rigidBody;

    [Header("System")]
    private bool isGrounded = true;
    private float timeAirborne;
    private float yVel;

    private void Awake()
    {
        entity = GetComponent<Entity_Base>();
        rigidBody = GetComponent<Rigidbody>();
    }

    #region Grounded State Methods
    public void LeaveGround()
    {
        isGrounded = false;
    }

    /// <summary>
    /// This method calculates how much damage to apply to an entity when they land
    /// </summary>
    public void Landed()
    {
        isGrounded = true;

        // Ensure the entity is falling downwards
        if (yVel < 0)
        {
            yVel = Mathf.Abs(yVel);// Make sure yVel is positive for damage calc

            // Calculate damage recived
            if (timeAirborne >= _timeThreshold && yVel >= _velocityThreshold)
            {
                // Apply damage
                float airbornDiff = (timeAirborne - _timeThreshold) * _timeMultiplier;
                int damageTaken = (int)(yVel * airbornDiff);
                entity.RemoveHealth(damageTaken);
            }
        }

        timeAirborne = 0;
    }
    #endregion

    #region Update Methods
    private void Update()
    {
        if (!isGrounded)
        {
            timeAirborne += Time.deltaTime;
        }
    }

    /// <summary>
    /// This method needs to be updated by the class that also handels landing states.
    /// It caches the velocity the moment right before the entity is considered grounded.
    /// Alternate solutions break if the entities velocity is too fast
    /// </summary>
    public void UpdateYVel()
    {
        yVel = rigidBody.velocity.y;
    }
    #endregion
}

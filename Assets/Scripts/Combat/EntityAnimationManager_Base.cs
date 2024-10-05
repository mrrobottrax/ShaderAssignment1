using UnityEngine;

/// <summary>
/// This abstract class should be implemented by classes that are meant to handle an entities animations.
/// </summary>
[RequireComponent(typeof(Animator))]
public abstract class EntityAnimationManager_Base : NetworkBehaviour
{
    [field: Header("Components")]
    [field: SerializeField] public Entity_Base Entity { get; private set; }
    [field: SerializeField] public Animator Animator { get; private set; }

    [field: Header("Entity Attacks")]
    [field: SerializeField] protected AttackList entityAttacks { get; private set; }

    #region Initialization Methods

    protected virtual void Awake()
    {
        Animator = GetComponent<Animator>();
    }

    #endregion

    #region Animation Events

    /// <summary>
    /// When implemented, this method should always grab a CombatPacket and sign it with this instances entity data.
    /// </summary>
    /// <remarks>
    /// This method needs to be called by an animation event at the start of an attack animation.
    /// </remarks>
    public abstract void StartAttack_AnimationEvent();

    /// <summary>
    /// When implemented, this method is where the manager should update the entities ongoing packet with their attack data.
    /// </summary>
    /// <remarks>
    /// This method needs to be called by an animation event on the frame of an attack animation that should cause damage or spawn a projectile.
    /// The event that calls this method should always come after the attack has started.
    /// </remarks>
    /// <param name="attackGroup">The group that the attack is in</param>
    public abstract void TryAction_AnimationEvent(string attackGroup);

    /// <summary>
    /// When implemented, this method should reset the AnimationManager to a state where a new attack can begin
    /// </summary>
    /// <remarks>
    /// This method needs to be called by an animation event at the end of an attack animation.
    /// </remarks>
    public virtual void FinishAttack_AnimationEvent()
    {
        Entity.FinishAttack();
    }
    #endregion

    /// <summary>
    /// Returns true if this managers animator contains a variable with a string.
    /// </summary>
    protected bool HasParameter(string parameterName)
    {
        foreach (var param in Animator.parameters)
        {
            if (param.name == parameterName)
            {
                return true;
            }
        }
        return false;
    }
}

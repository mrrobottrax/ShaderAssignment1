using System;

public interface ICombatPacketCompleter
{
    public event Action OnAttackCompleted;

    /// <summary>
    /// Called when an attack packet is completed
    /// </summary>
    public abstract void CompleteAttack();
}

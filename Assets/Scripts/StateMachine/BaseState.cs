using System;

public abstract class BaseState<EnumState> where EnumState : Enum
{
    public BaseState(EnumState key)
    {
        statesKey = key;
    }

    private EnumState statesKey;

    /// <summary>
    /// This method is used when a state is entered, initialization occurs here.
    /// </summary>
    public abstract void EnterState();

    /// <summary>
    /// This method is used when a state is exited, cleanup occurs here.
    /// </summary>
    public abstract void ExitState();

    /// <summary>
    /// This method is should be used to update logic of things like input and state changes.
    /// </summary>
    public abstract void UpdateState();

    /// <summary>
    /// This method is should be used to update the physics based logic of a state.
    /// </summary>
    public abstract void FixedUpdateState();


    /// <summary>
    /// Returns this states key
    /// </summary>
    public EnumState ReturnStateKey()
    {
        return statesKey;
    }
}

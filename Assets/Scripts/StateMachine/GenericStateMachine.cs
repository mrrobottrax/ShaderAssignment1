using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class GenericStateMachine<EnumState> : MonoBehaviour where EnumState : Enum
{
    // This dictionary contains all of the fine states of the state machine, the states are defined by the concrete state machine.
    protected Dictionary<EnumState, BaseState<EnumState>> finiteStates = new Dictionary<EnumState, BaseState<EnumState>>();

    [Header("System")]
    protected BaseState<EnumState> currentState = null;
    protected bool isTransitioningStates = false;

    protected virtual void Update()
    {
        // If the system is not transitioning states, and the condition to end this state has not been met, perform an update.
        if (!isTransitioningStates)
            currentState?.UpdateState();
    }

    protected virtual void FixedUpdate()
    {
        if (!isTransitioningStates)
            currentState?.FixedUpdateState();
    }


    /// <summary>
    /// This method handles the transition from the current state into a new state.
    /// </summary>
    public void SetState(EnumState key)
    {
        if(isTransitioningStates == false)
        {
            BaseState<EnumState> nextState = finiteStates[key];

            if(currentState == null || (currentState != null && !nextState.Equals(currentState)))
            {
                isTransitioningStates = true;
                currentState?.ExitState();

                currentState = nextState;

                currentState.EnterState();
                isTransitioningStates = false;
            }
        }
    }

    public BaseState<EnumState> ReturnState()
    {
        return currentState;
    }
}

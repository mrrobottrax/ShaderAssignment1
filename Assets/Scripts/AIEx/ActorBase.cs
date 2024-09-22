using UnityEngine;

public abstract class ActorBase : MonoBehaviour
{

    void Start()
    {
        ActorManager.Instance.AddActor(this);
    }

    /// <summary>
    /// Updates the actor when an tick has been called
    /// </summary>
    public void TickUpdate()
    {

    }

}

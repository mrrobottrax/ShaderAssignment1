using UnityEngine;

public class EventSystemManager : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(this);
    }
}

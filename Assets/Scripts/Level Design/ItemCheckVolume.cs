using UnityEngine;

public class ItemCheckVolume : MonoBehaviour
{
    public static ItemCheckVolume Instance;

    [field: SerializeField] public Vector3 Center { get; private set; }
    [field: SerializeField] public Vector3 Size { get; private set; }

    private void Awake()
    {
        Instance = this;
    }


    #region Debug Methods
#if DEBUG

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position + Center, Size);
    }
#endif
    #endregion
}

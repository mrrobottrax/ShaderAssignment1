using UnityEngine;

public class Train : MonoBehaviour
{
    public static Train Instance;

    [field: SerializeField] public Vector3 Center { get; private set; }
    [field: SerializeField] public Vector3 Size { get; private set; }

    private Animator animator;

    #region Initialization Methods

    private void Awake()
    {
        Instance = this;
        animator = GetComponent<Animator>();
    }
    #endregion

    public void SetOreRampOpen(bool isOpen)
    {
        animator.SetBool("IsOpen", isOpen);
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

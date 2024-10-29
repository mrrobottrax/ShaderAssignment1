using UnityEngine;

public class Train : MonoBehaviour
{
    public static Train Instance;

    [field: SerializeField] public Vector3 Center { get; private set; }
    [field: SerializeField] public Vector3 Size { get; private set; }

    [field: Header("Interactions")]
    [field: SerializeField] public UnityEventInteractable Open_Interactable { get; private set; }
    [field: SerializeField] public UnityEventInteractable Close_Interactable { get; private set; }


    private Animator animator;

    #region Initialization Methods

    private void Awake()
    {
        Instance = this;
        animator = GetComponent<Animator>();

        SetOreRampOpen(false);
    }
    #endregion

    public void SetOreRampOpen(bool isOpen)
    {
        animator.SetBool("IsOpen", isOpen);

        Open_Interactable.interactionEnabled = !isOpen;
        Close_Interactable.interactionEnabled = isOpen;
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

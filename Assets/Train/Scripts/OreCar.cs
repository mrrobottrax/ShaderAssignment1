using UnityEngine;

public class OreCar : TrainCar
{
    public static OreCar Instance;

    [field: Header("Components")]
    [SerializeField] private Animator animator;

    [field: Header("Interactions")]
    [field: SerializeField] public UnityEventInteractable[] Open_Interactable { get; private set; }
    [field: SerializeField] public UnityEventInteractable[] Close_Interactable { get; private set; }

    [field: Header("Ore Cart Validation")]
    [field: SerializeField] public Vector3 Center { get; private set; }
    [field: SerializeField] public Vector3 Size { get; private set; }

    #region Initialization Methods

    protected override void Awake()
    {
        base.Awake();

        if (Instance == null)
            Instance = this;
        else Destroy(this);

        SetOreRampOpen(false);
    }
    #endregion

    public void SetOreRampOpen(bool isOpen)
    {
        if (animator != null)
        {
            animator.SetBool("IsOpen", isOpen);

            foreach (UnityEventInteractable i in Open_Interactable)
            {
                i.interactionEnabled = !isOpen;
            }

            foreach (UnityEventInteractable i in Close_Interactable)
            {
                i.interactionEnabled = isOpen;
            }
        }
    }

    #region Debug Methods
#if DEBUG

    private void OnDrawGizmosSelected()
    {
        Matrix4x4 originalMatrix = Gizmos.matrix;

        Gizmos.color = Color.green;
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);// Set position and rotation of the cube in world space
        Gizmos.DrawWireCube(Center, Size);// Draw the cube with its local position and size

        Gizmos.matrix = originalMatrix;// Ensure other gizmos are not affected
    }
#endif
    #endregion
}

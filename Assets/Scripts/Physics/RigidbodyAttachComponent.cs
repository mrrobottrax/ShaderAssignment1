using UnityEngine;

/// <summary>
/// This component should be put on a dynamic rigidbody that needs to stay attached (move with) a larger rigidbody.
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof (CustomGravityComponent))]
public class RigidbodyAttachComponent : MonoBehaviour
{
    [Header("Search Parameters")]
    [SerializeField] private float _attachRange = 3f;
    [SerializeField] private Transform _checkPosition;
    [SerializeField] private LayerMask _searchLayers;

    [Header("Component")]
    private Rigidbody rb;
    private CustomGravityComponent rbGravity;

    [Header("System")]
    private MovingRigidbodyParent parent;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;

        rbGravity = GetComponent<CustomGravityComponent>();
        rbGravity.enabled = true;
    }

    private void OnDisable()
    {
        parent?.RemoveChild(rb);
        parent = null;
    }

    private void Update()
    {
        // Check if there is a moving object below the dynamic object
        RaycastHit hit;
        MovingRigidbodyParent movingRigidbody;

        if (Physics.Raycast(_checkPosition.position, Vector3.down, out hit,
            _attachRange, _searchLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform.GetComponent<MovingRigidbodyParent>())
            {
                // Cache the new parent
                movingRigidbody = hit.transform.GetComponent<MovingRigidbodyParent>();

                // Compare the current parent to the potential new parent
                if (parent != movingRigidbody)
                {
                    // Attempt to remove this rb from its previous parent if it has one
                    parent?.RemoveChild(rb);

                    // Add the rb to the new parent
                    parent = movingRigidbody;
                    parent.AddChild(rb);
                }
            }
        }
        else if (parent != null)
        {
            // Clear the parent if there is none in range
            parent.RemoveChild(rb);
            parent = null;
        }
    }
}

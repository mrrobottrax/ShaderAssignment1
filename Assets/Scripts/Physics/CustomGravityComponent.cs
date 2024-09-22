using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CustomGravityComponent : MonoBehaviour
{
    [Header("Components")]
    private Rigidbody rigidBody;

    [Header("System")]
    [SerializeField] private float _gravityScale = 1f;

    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody>();
        rigidBody.useGravity = false;
    }

    private void FixedUpdate()
    {
        // Apply custom gravity
        Vector3 gravity = Physics.gravity * _gravityScale;
        rigidBody.AddForce(gravity, ForceMode.Force);
    }

    public void SetGravityScale(float newScale)
    {
        _gravityScale = newScale;
    }
}


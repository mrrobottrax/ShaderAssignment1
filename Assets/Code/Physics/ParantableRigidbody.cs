using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ParantableRigidbody : MonoBehaviour
{
    public Rigidbody Rigidbody { get; private set; }

    private void Awake()
    {
        Rigidbody = GetComponent<Rigidbody>();
    }
}

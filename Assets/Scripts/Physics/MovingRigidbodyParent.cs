using System.Collections.Generic;
using UnityEngine;

public class MovingRigidbodyParent : MonoBehaviour
{
    [SerializeField] private Transform _childPoint;

    [SerializeField] private Rigidbody _rigidBody;
    private List<Rigidbody> childrenRBs = new List<Rigidbody>();

    private Vector3 prevFramePos;
    private Quaternion prevFrameRot;

    /// <summary>
    /// Adds a child to the list of attached rigidbodies
    /// </summary>
    public void AddChild(Rigidbody rb)
    {
        // Ensure the rb is not already in the list
        if (!_childPoint || childrenRBs.Contains(rb))
            return;

        childrenRBs.Add(rb);
    }

    /// <summary>
    /// Removes a child from the list of attached rigidbodies
    /// </summary>
    public void RemoveChild(Rigidbody rb)
    {
        if (!_childPoint || !childrenRBs.Contains(rb))
            return;

        childrenRBs.Remove(rb);
    }

    public void FixedUpdate()
    {
        // Manually interpolate the childs position based on the differece in position between the moving parents position this frame vs last frame.
        foreach (Rigidbody i in childrenRBs)
        {
            // Update each attached rigidbodies position based on the difference in parent position since the last frame
            i.transform.position += _rigidBody.position - prevFramePos;

            // Calculate the rotational delta between frames
            Quaternion deltaRotation = _rigidBody.rotation * Quaternion.Inverse(prevFrameRot);
            Quaternion yDeltaRotation = Quaternion.Euler(0, deltaRotation.eulerAngles.y, 0);

            // Apply the rotational difference to the child
            i.transform.rotation *= yDeltaRotation;
        }

        // Cache the moving parents position at the end of the frame to compare to the next frame
        prevFramePos = _rigidBody.position;
        prevFrameRot = _rigidBody.rotation;
    }
}

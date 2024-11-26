using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(Rigidbody))]
public class TrainCar : MonoBehaviour
{
    [Header("Physics")]
    [SerializeField] private float maxSpeed;
    [SerializeField] private float brakeForce;
    [SerializeField] private float stopThreshold = 0.5f;
    [SerializeField] private float brakeTimerThreshold = 2.0f;

    [field: Header("Components")]
    public Rigidbody rb { get; private set; }

    [Header("Rail System")]
    [field: SerializeField] protected SplineContainer railNetwork;

    [Header("System")]
    private Spline currentSpline;
    protected bool IsBraking { get; private set; }
    private float brakeTimer = 0.0f;
    protected bool IsStopped { get; private set; }

    #region Initialization Methods

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    #endregion

    #region Unity Callbacks

    protected virtual void FixedUpdate()
    {
        if (currentSpline == null || IsStopped) return;

        NativeSpline spline = new NativeSpline(currentSpline);

        // Get nearest point and tangent
        SplineUtility.GetNearestPoint(spline, transform.position, out float3 nearest, out float t);
        Vector3 tangentDir = Vector3.Normalize(spline.EvaluateTangent(t));
        Vector3 up = spline.EvaluateUpVector(t);

        // Calculate desired velocity to align with spline
        Vector3 desiredVelocity = tangentDir * maxSpeed;

        // Align position and rotation
        rb.MovePosition(Vector3.Lerp(rb.position, nearest, Time.fixedDeltaTime));
        Quaternion targetRotation = Quaternion.LookRotation(tangentDir, up);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime));

        // Apply velocity
        if (IsBraking)
            rb.velocity = Vector3.Lerp(rb.velocity, Vector3.zero, Time.fixedDeltaTime * brakeForce);
        else
            rb.velocity = Vector3.Lerp(rb.velocity, desiredVelocity, Time.fixedDeltaTime);

        // Clamp vel
        rb.velocity = Vector3.ClampMagnitude(rb.velocity, maxSpeed);

        // Stop train if velocity is below threshold
        if (IsBraking && rb.velocity.magnitude <= stopThreshold)
        {
            brakeTimer += Time.fixedDeltaTime;
            if (brakeTimer >= brakeTimerThreshold)
            {
                SetCarStopped(true);
                brakeTimer = 0.0f;
            }
        }
        else
        {
            brakeTimer = 0.0f;
        }
    }
    #endregion

    #region Car Control

    public virtual void SetBrakesActive(bool active)
    {
        IsBraking = active;

        // If a car has stopped, allow it to move
        if (IsBraking == false)
            SetCarStopped(false);
    }

    public virtual void SetCarStopped(bool stopped)
    {
        IsStopped = stopped;

        rb.constraints = IsStopped ? RigidbodyConstraints.FreezeAll : RigidbodyConstraints.None;

        if (IsStopped)
            rb.velocity = Vector3.zero;
    }

    #endregion

    #region Utility Methods

    public void SetCurrentRailNetwork(SplineContainer newRail)
    {
        enabled = newRail != null;

        if (enabled)
        {
            railNetwork = newRail;
            currentSpline = railNetwork.Splines[0];
        }
    }
    #endregion
}
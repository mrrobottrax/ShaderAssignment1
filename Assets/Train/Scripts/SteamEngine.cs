using UnityEngine;

public class SteamEngine : TrainCar
{
    [field: Header("Properties")]
    [SerializeField] private TrainCar[] attachedCars = new TrainCar[0];

    [field: Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private ParticleSystem smokeParticles;

    [field: Header("Movement Properties")]
    [SerializeField] private bool isAccelerating;
    [SerializeField] private float acceleration = 15;

    #region Initialization Methods

    private void Start()
    {
        Transform parent = transform.parent;

        if (parent != null)
        {
            // Try to get attached cars
            if (attachedCars.Length <= 0)
                attachedCars = parent.GetComponentsInChildren<TrainCar>();
        }

        // Etablish attached cars
        foreach (TrainCar i in attachedCars)
        {
            i.SetCurrentRailNetwork(railNetwork);
        }
    }
    #endregion

    #region Unity Callbacks

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        if (isAccelerating && !IsBraking && !IsStopped)
        {
            Throttle(acceleration);
        }
    }
    #endregion

    public override void SetBrakesActive(bool braking)
    {
        base.SetBrakesActive(braking);

        // If the engine is no longer breaking, allow each attached car to move
        foreach (TrainCar i in attachedCars)
            if (i != this)
                i.SetBrakesActive(braking);

        // Stop the smoke particles if the train breaks
        if (braking)
            smokeParticles?.Stop();
        else smokeParticles?.Play();
    }

    public override void SetCarStopped(bool stopped)
    {
        base.SetCarStopped(stopped);

        // Stop the smoke particles if the train stops
        if (stopped)
            smokeParticles?.Stop();
        else smokeParticles?.Play();

    }

    private void Throttle(float power)
    {
        Vector3 dir = power * transform.forward;
        rb.AddForce(dir);

        Debug.DrawRay(transform.position, dir * 100);
    }
}
using System;
using System.Collections;
using UnityEngine;

public class GlobalWindManager : MonoBehaviour
{
    /*
    [Header("Wind change parameters")]
    [SerializeField, Range(1, 1000)] float _directionTimerMin, _directionTimerMax;
    [SerializeField, Range(1, 90)] float _angleChange;
    [SerializeField, Range(0, 1)] float _chanceToChangeDir;

    [Header("Components")]
    [SerializeField] Transform windEmitter;

    [Header("System")]
    private float timeUntilNextChange;
    private bool useTimer;

    private float windAngle;
    private Vector3 windAxisAngleDir = new();

    private Coroutine dirChangeCoroutine;

    #region Initialization Methods

    private void Start()
    {
        // Start wind changing clock
        SetRandomWindActive(true);
    }
    #endregion

    #region Wind Change Methods

    /// <summary>
    /// This method calculates a random direction change in the wind and starts a lerp to that value
    /// </summary>
    private void RandomlyChangeDir()
    {
        timeUntilNextChange = UnityEngine.Random.Range(_directionTimerMin, _directionTimerMax);

        float random = UnityEngine.Random.Range(0f, 1f);
        if(random < _chanceToChangeDir)
        {
            float angleChange = UnityEngine.Random.Range(0f, 1f) < 0.5? _angleChange : -_angleChange;
            BeginDirChangeLerp(timeUntilNextChange, angleChange);
        }
    }

    private void BeginDirChangeLerp(float duration, float newAngle)
    {
        // Start lerp coroutine
        if (dirChangeCoroutine != null)
            StopCoroutine(dirChangeCoroutine);
        dirChangeCoroutine = StartCoroutine(DirChangeLerp(duration, newAngle));
    }

    private IEnumerator DirChangeLerp(float duration, float newAngle)
    {
        float time = 0;
        float startAngle = windAngle;
        newAngle = windAngle += newAngle;

        while (time < duration)
        {
            windAngle = Mathf.LerpAngle(startAngle, newAngle, time / duration);

            // Convert windAngle to radians
            float radians = Mathf.Deg2Rad * windAngle;

            // Calculate x and z components of the wind direction vector
            float x = Mathf.Sin(radians);
            float z = Mathf.Cos(radians);

            // Assign the wind direction vector
            windAxisAngleDir = new Vector3(x, 0, z);

            // Rotate wind emitter
            windEmitter.rotation = Quaternion.LookRotation(windAxisAngleDir, Vector3.up);

            time += Time.deltaTime;
            yield return null;
        }
    }
    #endregion

    #region Helper Methods

    /// <summary>
    /// Sets the state of the wind timer
    /// </summary>
    public void SetRandomWindActive(bool active)
    {
        useTimer = active;

        // Set wind timer
        if (active)
            timeUntilNextChange = UnityEngine.Random.Range(_directionTimerMin, _directionTimerMax);
    }

    /// <summary>
    /// Returns the normalized vector of the wind
    /// </summary>
    public Vector3 GetWindDir()
    {
        return windEmitter.forward;
    }
    #endregion

    private void Update()
    {
        // Simple timer until the next direction change
        if (useTimer && timeUntilNextChange > 0)
        {
            timeUntilNextChange -= Time.deltaTime;

            // Try to change dir
            if (timeUntilNextChange <= 0)
                RandomlyChangeDir();
        }
    }
    */
}

using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonCamera : MonoBehaviour, IInputHandler
{
	[Header("Transforms")]
	[SerializeField] new Transform camera;
	[SerializeField] Transform cameraParent;

	[Header("Components")]
    InputAction lookAction;
    PlayerController playerController;

    [Header("Camera Variables")]
	[SerializeField, Range(0.01f, 1)] float lookSenseYaw = .1f;
	[SerializeField, Range(0.01f, 1)] float lookSensePitch = .1f;
	[SerializeField] float pitchMin = -85;
	[SerializeField] float pitchMax = 85;
	[SerializeField] float foreheadSize = 0.2f; // Stupid name, sinks the camera town from the top of the head
	[SerializeField] float duckSpeed = 1;
	[SerializeField] float stepLerpSpeed = 5;
	[SerializeField] float maxStepLerpDist = 0.6f;

	[SerializeField] AnimationCurve vaultCurve;

	[Header("System")]
	public float pitch = 0;
	public float yaw = 0;
	float crouchProgress = 1;
	float stepOffset = 0; // Offset camera to smooth steps

	Vector3 lastPosition; // positions are in world space
	Vector3 position;

    #region Initialization Methods
    private void Awake()
	{
		playerController = GetComponent<PlayerController>();
	}

	private void Start()
	{
		lastPosition = cameraParent.transform.position;
		position = cameraParent.transform.position;

		EnableFirstPersonCamera(true);
    }
    #endregion

    #region Unity Callbacks
    void OnDestroy()
    {
        SetControlsSubscription(false);
    }

    private void Update()
	{
		if (lookAction != null)
		{
            // Look
            Vector2 mouseDelta = lookAction.ReadValue<Vector2>();
            yaw += mouseDelta.x * lookSenseYaw;
            pitch -= mouseDelta.y * lookSensePitch;

            pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

            cameraParent.localRotation = Quaternion.Euler(pitch, yaw, 0);

            // Interpolate between positions calculated in FixedUpdate
            // todo: don't interpolate when distance is too large
#pragma warning disable UNT0004
            float fract = (Time.time - Time.fixedTime) / Time.fixedDeltaTime;
            cameraParent.transform.position = Vector3.Lerp(lastPosition, position, fract);
#pragma warning restore UNT0004
        }
    }

	private void FixedUpdate()
	{
		float height;

		// Crouching
		crouchProgress += Time.fixedDeltaTime * duckSpeed * (playerController.IsCrouching ? -1 : 1);
		crouchProgress = Mathf.Clamp01(crouchProgress);

		height = Mathf.Lerp(playerController.crouchingHeight, playerController.standingHeight, crouchProgress) - foreheadSize;

		// Smooth stepping
		stepOffset -= 0.5f * stepOffset * Time.fixedDeltaTime * stepLerpSpeed;

		stepOffset = Mathf.Clamp(stepOffset, -maxStepLerpDist, maxStepLerpDist);
		height += stepOffset;

		stepOffset -= 0.5f * stepOffset * Time.fixedDeltaTime * stepLerpSpeed;

		// Update camera position
		lastPosition = position;
		position = cameraParent.parent.localToWorldMatrix * new Vector4(0, height, 0, 1);
	}
    #endregion

    #region Input Methods

    public void SetControlsSubscription(bool isInputEnabled)
    {
        if (isInputEnabled)
            Subscribe();
        else if (InputManager.Instance != null)
            Unsubscribe();
    }

    /// <summary>
    /// Enables or disables the first-person camera mode by locking and hiding the cursor,
    /// and updating the input controls subscription based on the specified state.
    /// </summary>
    /// <param name="isEnabled">If true, enables first-person camera; if false, disables first-person camera</param>
    public void EnableFirstPersonCamera(bool isEnabled)
    {
        // Lock and hide mouse
        Cursor.lockState = isEnabled ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !isEnabled;

        SetControlsSubscription(isEnabled);
    }

    public void Subscribe()
    {
        lookAction = InputManager.Instance.controls.Player.Look;
    }

    public void Unsubscribe()
    {
        lookAction = null;
    }
    #endregion

    #region Helper Methods
    public Transform GetCamera()
    {
        return camera.transform;
    }

	public Vector3 RotateVectorYaw(Vector2 vector)
	{
		Vector3 newVector = new();

		float c = Mathf.Cos(Mathf.Deg2Rad * yaw);
		float s = Mathf.Sin(Mathf.Deg2Rad * yaw);

		newVector.x = c * vector.x + s * vector.y;
		newVector.y = 0;
		newVector.z = -s * vector.x + c * vector.y;

		return newVector;
	}

	public Vector3 RotateVector(Vector2 vector)
	{
		Vector3 outVector = new(vector.x, 0, vector.y);

		return Quaternion.Euler(pitch, yaw, 0) * outVector;
	}
    #endregion

    #region Vaulting
    public void Step(float stepHeight)
	{
		stepOffset -= stepHeight;
	}

	IEnumerator VaultAnimation(Vector3 startPos, Vector3 endPos, bool isOnBoat)
	{
		float endTime = Time.time + playerController.vaultDuration;

		// Loop each FixedUpdate until animation complete
		float vaultProgress = 0;
		while (Time.time <= endTime)
		{
			yield return new WaitForFixedUpdate();

			// Vaulting interp
			vaultProgress += Time.fixedDeltaTime / playerController.vaultDuration;
			position = Vector3.Lerp(startPos, endPos, vaultCurve.Evaluate(vaultProgress));
		}
	}

	public void Vault(Vector3 positionDelta)
	{
        StartCoroutine(VaultAnimation(position, position + positionDelta, false));
    }
    #endregion
}

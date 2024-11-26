using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonCamera : MonoBehaviour, IInputHandler
{
	[field: Header("Components")]
	public Transform CameraTransform { get; private set; }
	public PlayerController m_followPlayer;

	[Header("Camera Variables")]
	[SerializeField, Range(0.01f, 1)] float m_lookSenseYaw = .1f;
	[SerializeField, Range(0.01f, 1)] float m_lookSensePitch = .1f;
	[SerializeField] float m_runTiltMax = 15f;
	[SerializeField] float m_runTiltMultiplier = 15f;
	[SerializeField] float m_pitchMin = -85;
	[SerializeField] float m_pitchMax = 85;
	[SerializeField] float m_foreheadSize = 0.2f; // Sinks the camera down from the top of the head
	[SerializeField] float m_duckSpeed = 1;
	[SerializeField] float m_stepLerpSpeed = 5;
	[SerializeField] float m_maxStepLerpDist = 0.6f;
	[SerializeField] float m_DefaultFOV = 90;
	[SerializeField] float m_SprintFOVMult = 1.2f;
	[SerializeField] float m_FOVChangeSpeed = 1;

	// System
	float m_pitch = 0;
	float m_yaw = 0;
	float m_standProgress = 1;
	float m_stepOffset = 0; // Offset camera to smooth out steps
	float m_FOV;

	Vector3 m_lastPosition; // Positions are in world space
	Vector3 m_position;
	float m_lastRoll;
	float m_roll;

	Camera m_Camera;

	InputAction m_lookAction;

	#region Unity Callbacks

	private void Start()
	{
		m_lastPosition = CalcCameraPos();
		m_position = m_lastPosition;
		CameraTransform = transform;
		m_Camera = GetComponent<Camera>();

		EnableFirstPersonCamera(true);
	}

	void OnDestroy()
	{
		EnableFirstPersonCamera(false);
	}

	private void Update()
	{
		if (m_lookAction != null)
		{
			// Look
			Vector2 mouseDelta = m_lookAction.ReadValue<Vector2>();
			m_yaw += mouseDelta.x * m_lookSenseYaw;
			m_pitch -= mouseDelta.y * m_lookSensePitch;

			m_pitch = Mathf.Clamp(m_pitch, m_pitchMin, m_pitchMax);
		}

		// Interpolate between positions calculated in FixedUpdate
		// todo: don't interpolate when distance is too large
#pragma warning disable UNT0004
		float fract = (Time.time - Time.fixedTime) / Time.fixedDeltaTime;
#pragma warning restore UNT0004

		transform.position = Vector3.Lerp(m_lastPosition, m_position, fract);

		float roll = Mathf.Lerp(m_lastRoll, m_roll, fract);
		transform.localRotation = Quaternion.Euler(m_pitch, m_yaw, roll);

		UpdateFOV();
	}

	void UpdateFOV()
	{
		float sprintFOV = m_DefaultFOV * m_SprintFOVMult;

		float t = (m_FOV - m_DefaultFOV) / (sprintFOV - m_DefaultFOV);
		float rateOfChange = Time.deltaTime / (sprintFOV - m_DefaultFOV) * m_FOVChangeSpeed;

		if (m_followPlayer.IsSprinting)
		{
			t += rateOfChange;
		}
		else
		{
			t -= rateOfChange;
		}

		//Debug.Log("___");
		//Debug.Log(t);
		t = Mathf.Clamp01(t);
		m_FOV = Mathf.Lerp(m_DefaultFOV, sprintFOV, t);
		//Debug.Log(m_FOV);

		m_Camera.fieldOfView = m_FOV;
	}

	private void FixedUpdate()
	{
		if (m_followPlayer == null)
		{
			m_lastPosition = m_position;
			m_lastRoll = m_roll;
			return;
		}

		// Update crouching
		if (m_followPlayer.IsGrounded)
		{
			m_standProgress += Time.fixedDeltaTime * m_duckSpeed * (m_followPlayer.IsCrouching ? -1 : 1);
			m_standProgress = Mathf.Clamp01(m_standProgress);
		}
		else
		{
			m_standProgress = (m_followPlayer.IsCrouching ? 0 : 1);
		}

		// Update smooth stepping
		m_stepOffset -= 0.5f * m_stepOffset * Time.fixedDeltaTime * m_stepLerpSpeed;
		m_stepOffset = Mathf.Clamp(m_stepOffset, -m_maxStepLerpDist, m_maxStepLerpDist);

		m_lastPosition = m_position;
		m_position = CalcCameraPos();

		m_stepOffset -= 0.5f * m_stepOffset * Time.fixedDeltaTime * m_stepLerpSpeed; // we do this in 2 parts for more accurate integration

		// Update camera tilt
		m_lastRoll = m_roll;
		m_roll = Mathf.Clamp(-Vector3.Dot(m_followPlayer.GetVelocity(), CameraTransform.right) * m_runTiltMultiplier, -m_runTiltMax, m_runTiltMax);
	}
	#endregion

	#region Input Methods

	public void Subscribe()
	{
		m_lookAction = InputManager.Instance.Player.Look;
	}

	public void Unsubscribe()
	{
		m_lookAction = null;
	}

	public void SetControlsSubscription(bool isInputEnabled)
	{
		if (isInputEnabled)
			Subscribe();
		else
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
	#endregion

	#region Helper Methods

	public Vector3 RotateVectorYaw(Vector2 vector)
	{
		Vector3 newVector = new();

		float c = Mathf.Cos(Mathf.Deg2Rad * m_yaw);
		float s = Mathf.Sin(Mathf.Deg2Rad * m_yaw);

		newVector.x = c * vector.x + s * vector.y;
		newVector.y = 0;
		newVector.z = -s * vector.x + c * vector.y;

		return newVector;
	}

	public Vector3 RotateVector(Vector2 vector)
	{
		Vector3 outVector = new(vector.x, 0, vector.y);

		return Quaternion.Euler(m_pitch, m_yaw, 0) * outVector;
	}

	#endregion

	public void Step(float stepHeight)
	{
		m_stepOffset -= stepHeight;
	}

	public Vector3 CalcCameraPos()
	{
		// Calculate view display height
		float height;

		height = Mathf.Lerp(
			m_followPlayer.MvmtData.m_crouchingHeight,
			m_followPlayer.MvmtData.m_standingHeight,
			m_standProgress
		) - m_foreheadSize;

		height += m_stepOffset;

		return m_followPlayer.GetPosition() + new Vector3(0, height, 0);
	}
}

using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonCamera : MonoBehaviour, IInputHandler
{
    [Header("Components")]
    public Transform CameraTransform;
	public PlayerController m_followPlayer;

	[Header("Camera Variables")]
	[SerializeField, Range(0.01f, 1)] float m_lookSenseYaw = .1f;
	[SerializeField, Range(0.01f, 1)] float m_lookSensePitch = .1f;
	[SerializeField] float m_pitchMin = -85;
	[SerializeField] float m_pitchMax = 85;
	[SerializeField] float m_foreheadSize = 0.2f; // Sinks the camera down from the top of the head
	[SerializeField] float m_duckSpeed = 1;
	[SerializeField] float m_stepLerpSpeed = 5;
	[SerializeField] float m_maxStepLerpDist = 0.6f;

	// System
	float m_pitch = 0;
	float m_yaw = 0;
	float m_standProgress = 1;
	float m_stepOffset = 0; // Offset camera to smooth out steps

	Vector3 m_lastPosition; // Positions are in world space
	Vector3 m_position;

	InputAction m_lookAction;

	#region Unity Callbacks

	private void Start()
	{
		m_lastPosition = CalcCameraPos();
		m_position = m_lastPosition;

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

			transform.localRotation = Quaternion.Euler(m_pitch, m_yaw, 0);
		}

		// Interpolate between positions calculated in FixedUpdate
		// todo: don't interpolate when distance is too large
#pragma warning disable UNT0004
		float fract = (Time.time - Time.fixedTime) / Time.fixedDeltaTime;
		transform.position = Vector3.Lerp(m_lastPosition, m_position, fract);
#pragma warning restore UNT0004
	}

	private void FixedUpdate()
	{
		if (m_followPlayer == null)
		{
			m_lastPosition = m_position;
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

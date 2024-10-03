using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody), typeof(BoxCollider), typeof(Animator))]
public class PlayerController : NetworkBehaviour
{
	// Constants
	const float k_hitEpsilon = 0.015f; // Min distance to wall
	const float k_groundCheckDist = 0.03f;
	const float k_stopEpsilon = 0.0001f; // Stop when <= this speed
	const int k_maxBumps = 8; // Max number of iterations per frame
	const int k_maxPlanes = 8; // Max number of planes to collide with at once

	[Header("Movement")]
	[SerializeField] PlayerMovementData m_movementData;
	public PlayerMovementData MvmtData { get => m_movementData; }

	// Components
	[Header("Components")]
	[SerializeField] FirstPersonCamera m_fpsCamera;
	Rigidbody m_rigidbody;
	BoxCollider m_collider;
	NetworkAnimator m_animator;

	// System
	public bool IsCrouching { get; private set; }
	public bool IsGrounded { get; private set; }

	Vector2 m_wishMoveDir;
	Vector3 m_velocity;
	Vector3 m_position;

	bool m_isCrouchPressed;
	bool m_isJumpPressed;

	int m_framesStuck = 0;
	bool m_justJumped;


	enum EMovementMode
	{
		Standard,
		Noclip
	}
	[Header("Debug")]
	[SerializeField] private EMovementMode m_movementMode;


	#region Initialization Methods

	void Awake()
	{
		m_rigidbody = GetComponent<Rigidbody>();
		m_collider = GetComponent<BoxCollider>();
		m_animator = GetComponent<NetworkAnimator>();

		m_rigidbody.isKinematic = true;
		m_rigidbody.freezeRotation = true;

		m_collider.layerOverridePriority = 100;
		m_collider.excludeLayers = ~m_movementData.m_layerMask;
		m_collider.includeLayers = m_movementData.m_layerMask;

		Assert.IsNotNull(m_movementData);
		Assert.IsNotNull(m_fpsCamera);

		UpdateCollider();
	}

	void Start()
	{
		SetControlsSubscription(true);
	}

	#endregion

	#region Unity Callbacks

	void OnDestroy()
	{
		SetControlsSubscription(false);
	}

	void FixedUpdate()
	{
		UpdateCollider();

		if (!IsOwner) return;

		if (InputManager.ControlMode != InputManager.ControlType.Player)
		{
			m_wishMoveDir = Vector2.zero;
		}

		switch (m_movementMode)
		{
			case EMovementMode.Standard:
				StandardMovement();
				break;

			case EMovementMode.Noclip:
				Accelerate(m_fpsCamera.RotateVector(m_wishMoveDir), m_movementData.m_noclipAcceleration, m_movementData.m_noclipSpeed);
				Friction(m_movementData.m_noclipFriction);
				m_position += m_velocity * Time.fixedDeltaTime;
				break;
		}

		m_rigidbody.MovePosition(m_position);
	}

	#endregion

	#region Input Methods

	public void SetControlsSubscription(bool isInputEnabled)
	{
		if (!IsOwner) return;

		if (isInputEnabled)
			Subscribe();
		else
			Unsubscribe();
	}

	void OnMoveInput(InputAction.CallbackContext context)
	{
		m_wishMoveDir = context.ReadValue<Vector2>();
	}

	void OnJumpInput(InputAction.CallbackContext context)
	{
		m_isJumpPressed = context.ReadValueAsButton();

		if (m_isJumpPressed && IsGrounded)
			Jump();
	}

	void OnCrouchInput(InputAction.CallbackContext context)
	{
		m_isCrouchPressed = context.ReadValueAsButton();
	}

	public void Subscribe()
	{
		InputManager.Instance.Player.Movement.performed += OnMoveInput;

		InputManager.Instance.Player.Jump.performed += OnJumpInput;
		InputManager.Instance.Player.Jump.canceled += OnJumpInput;

		InputManager.Instance.Player.Crouch.performed += OnCrouchInput;
		InputManager.Instance.Player.Crouch.canceled += OnCrouchInput;
	}

	public void Unsubscribe()
	{
		InputManager.Instance.Player.Movement.performed -= OnMoveInput;

		InputManager.Instance.Player.Jump.performed -= OnJumpInput;
		InputManager.Instance.Player.Jump.canceled -= OnJumpInput;

		InputManager.Instance.Player.Crouch.performed -= OnCrouchInput;
		InputManager.Instance.Player.Crouch.canceled -= OnCrouchInput;

		m_wishMoveDir = Vector2.zero;
	}
	#endregion

	#region Actions

	private void TryCrouch(bool isAttemptingCrouch)
	{
		if (isAttemptingCrouch)
		{
			IsCrouching = true;

			if (!IsGrounded)
			{
				m_position += Vector3.up * ((m_movementData.m_standingHeight - m_movementData.m_crouchingHeight) / 1.5f);
			}
		}
		else
		{
			// Make sure there is room
			// todo: box cast in air so we can uncrouch close to ground
			IsCrouching = false;
			if (!IsGrounded)
			{
				m_position -= Vector3.up * ((m_movementData.m_standingHeight - m_movementData.m_crouchingHeight) / 1.5f);
			}

			bool hasRoom = !CheckHull();

			if (!hasRoom)
			{
				IsCrouching = true;
				if (!IsGrounded)
				{
					m_position += Vector3.up * ((m_movementData.m_standingHeight - m_movementData.m_crouchingHeight) / 1.5f);
				}
			}
		}

		m_animator.SetBool("Crouched", IsCrouching);
	}

	private void Jump()
	{
		m_velocity.y += m_movementData.m_jumpForce;
		IsGrounded = false;
		m_justJumped = true;
	}

	#endregion

	#region Collision

	bool CastHull(Vector3 direction, float maxDist, out RaycastHit hitInfo)
	{
		float halfHeight = GetColliderHeight() / 2f;

		bool hit = Physics.BoxCast(m_position + Vector3.up * halfHeight,
			new Vector3(m_movementData.m_horizontalSize / 2f, halfHeight, m_movementData.m_horizontalSize / 2f),
			direction,
			out hitInfo,
			Quaternion.identity,
			maxDist,
			m_movementData.m_layerMask,
			QueryTriggerInteraction.Ignore
		);

		hitInfo.distance -= k_hitEpsilon / -Vector3.Dot(direction, hitInfo.normal); // Back up a little
		if (hitInfo.distance < 0) hitInfo.distance = 0;

		return hit;
	}

	bool CheckHull()
	{
		float halfHeight = GetColliderHeight() / 2f;
		return Physics.CheckBox(
			m_position + Vector3.up * halfHeight,
			new Vector3(m_movementData.m_horizontalSize / 2f, halfHeight, m_movementData.m_horizontalSize / 2f),
			Quaternion.identity,
			m_movementData.m_layerMask,
			QueryTriggerInteraction.Ignore
		);
	}

	bool StuckCheck()
	{
		float halfHeight = GetColliderHeight() / 2f;
		Collider[] colliders = Physics.OverlapBox(
			m_position + Vector3.up * halfHeight,
			new Vector3(m_movementData.m_horizontalSize / 2f - k_hitEpsilon, halfHeight - k_hitEpsilon, m_movementData.m_horizontalSize / 2f - k_hitEpsilon),
			Quaternion.identity,
			m_movementData.m_layerMask,
			QueryTriggerInteraction.Ignore
		);

		if (colliders.Length > 0)
		{
			++m_framesStuck;

			Debug.LogWarning("Player stuck!");

			if (m_framesStuck > 5)
			{
				Debug.Log("Wow, you're REALLY stuck.");
				m_velocity = Vector3.zero;
				m_position += Vector3.up * 0.5f;
			}

			if (Physics.ComputePenetration(
				m_collider,
				m_position,
				transform.rotation,
				colliders[0],
				colliders[0].transform.position,
				colliders[0].transform.rotation,
				out Vector3 dir,
				out float dist
			))
			{
				m_position += dir * (dist + k_hitEpsilon * 2.0f);
				m_velocity = Vector3.zero;
			}
			else
			{
				m_velocity = Vector3.zero;
				m_position += Vector3.up * 0.5f;
			}

			return true;
		}

		m_framesStuck = 0;

		return false;
	}

	void CollideAndSlide()
	{
		Vector3 originalVelocity = m_velocity;

		// When we collide with multiple planes at once (crease)
		Vector3[] planes = new Vector3[k_maxPlanes];
		int planeCount = 0;

		float time = Time.fixedDeltaTime; // The amount of time remaining in the frame, decreases with each iteration
		int bumpCount;
		for (bumpCount = 0; bumpCount < k_maxBumps; ++bumpCount)
		{
			float speed = m_velocity.magnitude;

			if (speed <= k_stopEpsilon)
			{
				m_velocity = Vector3.zero;
				break;
			}

			// Try to move in this direction
			Vector3 direction = m_velocity.normalized;
			float maxDist = speed * time;
			if (CastHull(direction, maxDist, out RaycastHit hit))
			{
				if (hit.distance > 0)
				{
					// Move rigibody to where it collided
					m_position += direction * hit.distance;

					// Decrease time based on how far it travelled
					float fraction = hit.distance / maxDist;

					if (fraction > 1)
					{
						Debug.LogWarning("Fraction too high");
						fraction = 1;
					}

					time -= fraction * Time.fixedDeltaTime;

					planeCount = 0;
				}

				if (planeCount >= k_maxPlanes)
				{
					Debug.LogWarning("Colliding with too many planes at once");
					m_velocity = Vector3.zero;
					break;
				}

				planes[planeCount] = hit.normal;
				++planeCount;

				// Clip velocity to each plane
				bool conflictingPlanes = false;
				for (int j = 0; j < planeCount; ++j)
				{
					m_velocity = Vector3.ProjectOnPlane(originalVelocity, planes[j]);

					// Check if the velocity is against any other planes
					for (int k = 0; k < planeCount; ++k)
					{
						if (j != k) // No point in checking the same plane we just clipped to
						{
							if (Vector3.Dot(m_velocity, planes[k]) < 0.002f) // Moving into the plane, BAD!
							{
								conflictingPlanes = true;
								break;
							}
						}
					}

					if (!conflictingPlanes) break; // Use the first good plane
				}

				// No good planes
				if (conflictingPlanes)
				{
					if (planeCount == 2)
					{
						// Cross product of two planes is the only direction to go
						Vector3 dir = Vector3.Cross(planes[0], planes[1]).normalized; // todo: maybe normalize is unnecessary?

						// Go in that direction
						m_velocity = dir * Vector3.Dot(dir, originalVelocity);
					}
					else
					{
						m_velocity = Vector3.zero;
						break;
					}
				}
			}
			else
			{
				// Move rigibody according to velocity
				m_position += direction * maxDist;
				break;
			}

			// Stop tiny oscillations
			if (Vector3.Dot(m_velocity, originalVelocity) <= 0)
			{
				m_velocity = Vector3.zero;
				break;
			}

			if (time <= 0)
			{
				Debug.Log("Outta time");
				break; // outta time
			}
		}

		if (bumpCount >= k_maxBumps)
		{
			Debug.LogWarning("Bumps exceeded");
		}
	}
	#endregion

	#region Utility

	void UpdateCollider()
	{
		float h = GetColliderHeight();
		m_collider.size = new Vector3(m_movementData.m_horizontalSize, h, m_movementData.m_horizontalSize);
		m_collider.center = new Vector3(0, h / 2.0f, 0);
	}

	public float GetColliderHeight()
	{
		return IsCrouching ? m_movementData.m_crouchingHeight : m_movementData.m_standingHeight;
	}

	public Vector3 GetPosition()
	{
		return m_position;
	}

	public Vector3 GetVelocity()
	{
		return m_velocity;
	}


	void GroundCheck()
	{
		if (m_justJumped)
		{
			IsGrounded = false;
			return;
		}

		// Allow for force to knock off the ground
		if (m_velocity.y > m_movementData.m_knockUpThreshold)
		{
			IsGrounded = false;
			return;
		}

		if (CastHull(-Vector3.up, k_groundCheckDist, out RaycastHit hit))
		{
			if (hit.normal.y > Mathf.Cos(Mathf.Deg2Rad * m_movementData.m_maxWalkableAngle))
			{
				IsGrounded = true;
			}
			else
			{
				IsGrounded = false;
			}
		}
		else
		{
			IsGrounded = false;
		}
	}

	void CategorizePosition()
	{
		GroundCheck();
	}
	#endregion

	#region Movement Methods

	void Friction(float friction)
	{
		float speed = m_velocity.magnitude;

		float control = Mathf.Max(speed, m_movementData.m_stopSpeed);

		float newSpeed = Mathf.Max(speed - (control * friction * Time.fixedDeltaTime), 0);

		if (speed != 0)
		{
			float mult = newSpeed / speed;
			m_velocity *= mult;
		}
	}

	void Accelerate(Vector3 direction, float acceleration, float maxSpeed)
	{
		float add = acceleration * maxSpeed * Time.fixedDeltaTime;

		// Clamp added velocity in acceleration direction
		float speed = Vector3.Dot(direction, m_velocity);

		if (speed + add > maxSpeed)
		{
			add = Mathf.Max(maxSpeed - speed, 0);
		}

		m_velocity += add * direction;
	}

	private void StandardMovement()
	{
		if (StuckCheck())
			return;

		CategorizePosition();

		Vector3 globalWishDir = m_fpsCamera.RotateVectorYaw(m_wishMoveDir);

		// Crouch / un-crouch
		if (m_isCrouchPressed)
		{
			if (!IsCrouching)
			{
				TryCrouch(true);
			}
		}
		else if (IsCrouching)
		{
			TryCrouch(false);
		}

		// Pick movement method
		if (IsGrounded)
		{
			GroundMove(globalWishDir);
		}
		else
		{
			AirMove(globalWishDir);
		}

		m_justJumped = false;
	}

	private void GroundMove(Vector3 moveDir)
	{
		m_velocity.y = 0;

		float moveSpeed = IsCrouching ? m_movementData.m_crouchingSpeed : m_movementData.m_walkingSpeed;
		float acceleration = m_movementData.m_acceleration;
		float friction = m_movementData.m_friction;

		// Friction
		Friction(friction);

		// Accelerate
		Accelerate(moveDir, acceleration, moveSpeed);
		float speed = m_velocity.magnitude;

		// Clamp Speed
		if (speed > moveSpeed)
		{
			float mult = moveSpeed / speed;
			m_velocity *= mult;
		}

		if (speed <= 0)
		{
			return;
		}

		Vector3 startVelocity = m_velocity;
		Vector3 startPosition = m_position;

		CollideAndSlide();
		Vector3 downVelocity = m_velocity;
		Vector3 downPosition = m_position;

		// Move up and try another move
		{
			m_velocity = startVelocity;
			m_position = startPosition;

			if (CastHull(Vector3.up, m_movementData.m_stepHeight, out RaycastHit hit))
			{
				m_position += Vector3.up * hit.distance;
			}
			else
			{
				m_position += Vector3.up * m_movementData.m_stepHeight;
			}
		}

		CollideAndSlide();

		// Move back down
		{
			if (CastHull(-Vector3.up, m_movementData.m_stepHeight * 2, out RaycastHit hit))
			{
				m_position -= Vector3.up * hit.distance;
			}
			else
			{
				m_position -= 2 * m_movementData.m_stepHeight * Vector3.up;
			}
		}

		GroundCheck();

		// Either reset to the on ground move results, or keep the step move results

		// If we stepped onto air, just do the regular move
		if (!IsGrounded)
		{
			m_position = downPosition;
			m_velocity = downVelocity;
		}
		// Otherwise, pick the move that goes the furthest
		else if (Vector3.Distance(startPosition, downPosition) >= Vector3.Distance(startPosition, m_position))
		{
			m_position = downPosition;
			m_velocity = downVelocity;
		}
		else
		{
			m_fpsCamera.Step(m_position.y - startPosition.y);
			m_velocity.y = Mathf.Max(m_velocity.y, downVelocity.y); // funny quake ramp jumps
		}
	}

	private void AirMove(Vector3 moveDir)
	{
		Accelerate(moveDir, m_movementData.m_airAcceleration, m_movementData.m_airSpeed);

		m_velocity.y -= (m_movementData.m_gravity * Time.fixedDeltaTime) / 2f;
		CollideAndSlide();
		m_velocity.y -= (m_movementData.m_gravity * Time.fixedDeltaTime) / 2f;
	}

	public void Teleport(Vector3 position)
	{
		m_position = position;
		transform.position = m_position;
	}

	#endregion
}
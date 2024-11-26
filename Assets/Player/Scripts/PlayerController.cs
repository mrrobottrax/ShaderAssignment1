using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody), typeof(BoxCollider), typeof(Animator))]
public class PlayerController : NetworkBehaviour
{
	// Constants
	const float k_hitEpsilon = 0.015f; // Min distance to wall
	const float k_groundCheckDist = 0.04f;
	const float k_stopEpsilon = 0.0001f; // Stop when <= this speed
	const int k_maxBumps = 8; // Max number of iterations per frame
	const int k_maxPlanes = 8; // Max number of planes to collide with at once

	[Header("Movement")]
	[SerializeField] private PlayerMovementData m_movementData;
	public PlayerMovementData MvmtData { get => m_movementData; }

	[Header("Components")]
	[SerializeField] FirstPersonCamera m_fpsCamera;
	[SerializeField] Transform m_chestHeight;
	[SerializeField] AudioSource m_StepSource;
	private PlayerStats playerStats;
	private Rigidbody m_rigidbody;
	private BoxCollider m_collider;
	private NetworkAnimator m_animator;

	[Header("Sounds")]
	[SerializeField, Range(0, 1)] float m_StepVolume = 0.5f;

	[field: Header("System")]
	public bool IsCrouching { get; private set; }
	public bool IsSprinting { get; private set; }
	public bool IsGrounded { get; private set; }

	Vector2 m_wishMoveDir;
	Vector3 m_velocity;
	Vector3 m_position;

	bool m_isCrouchPressed;
	bool isSprintPressed;
	bool m_isJumpPressed;

	int m_framesStuck = 0;
	bool m_justJumped;

	GameObject m_SurfaceObject;
	Vector3 m_LastFootStepPosition;
	AudioClip m_LastStepSound;

    private ParantableRigidbody parentRigidbody;
    Vector3 previousParentPosition;
    Quaternion previousParentRotation;

    // Vaulting
    private Coroutine vaulting;

	enum EMovementMode
	{
		Standard,
		Vaulting,
		Noclip
	}

	[Header("Debug")]
	[SerializeField] private EMovementMode m_movementMode;

	#region Initialization Methods

	void Awake()
	{
		playerStats = GetComponent<PlayerStats>();
		m_rigidbody = GetComponent<Rigidbody>();
		m_collider = GetComponent<BoxCollider>();
		m_animator = GetComponent<NetworkAnimator>();

		m_rigidbody.isKinematic = true;
		m_rigidbody.freezeRotation = true;

		m_collider.layerOverridePriority = 100;
		m_collider.excludeLayers = ~m_movementData.m_layerMask;
		m_collider.includeLayers = m_movementData.m_layerMask;

		m_LastFootStepPosition = m_position;

		Assert.IsNotNull(m_movementData);
		Assert.IsNotNull(m_fpsCamera);
		Assert.IsNotNull(m_StepSource);

		UpdateCollider();
	}

	void Start()
	{
		SetControlsSubscription(true);
	}

	#endregion

	#region Unity Callbacks

	private void OnDestroy()
	{
		SetControlsSubscription(false);
	}

	private void FixedUpdate()
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

	private void OnMoveInput(InputAction.CallbackContext context)
	{
		m_wishMoveDir = context.ReadValue<Vector2>();
	}

	private void OnJumpInput(InputAction.CallbackContext context)
	{
		m_isJumpPressed = context.ReadValueAsButton();

		if (m_isJumpPressed && IsGrounded)
			Jump();
	}

	private void OnCrouchInput(InputAction.CallbackContext context)
	{
		m_isCrouchPressed = context.ReadValueAsButton();
	}

	private void OnSprintInput(InputAction.CallbackContext context)
	{
		isSprintPressed = context.ReadValueAsButton();

		TrySprint(isSprintPressed);
	}

	public void Subscribe()
	{
		InputManager.Instance.Player.Movement.performed += OnMoveInput;

		InputManager.Instance.Player.Jump.performed += OnJumpInput;
		InputManager.Instance.Player.Jump.canceled += OnJumpInput;

		InputManager.Instance.Player.Crouch.performed += OnCrouchInput;
		InputManager.Instance.Player.Crouch.canceled += OnCrouchInput;

		InputManager.Instance.Player.Sprint.performed += OnSprintInput;
		InputManager.Instance.Player.Sprint.canceled += OnSprintInput;
	}

	public void Unsubscribe()
	{
		InputManager.Instance.Player.Movement.performed -= OnMoveInput;

		InputManager.Instance.Player.Jump.performed -= OnJumpInput;
		InputManager.Instance.Player.Jump.canceled -= OnJumpInput;

		InputManager.Instance.Player.Crouch.performed -= OnCrouchInput;
		InputManager.Instance.Player.Crouch.canceled -= OnCrouchInput;

		InputManager.Instance.Player.Sprint.performed -= OnSprintInput;
		InputManager.Instance.Player.Sprint.canceled -= OnSprintInput;

		m_wishMoveDir = Vector2.zero;
	}
	#endregion

	#region Actions

	private void TryCrouch(bool isAttemptingCrouch)
	{
		if (isAttemptingCrouch)
		{
			IsCrouching = true;
			IsSprinting = false;

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

	private void TrySprint(bool isAttemptingSprint)
	{
		if (isAttemptingSprint && playerStats.GetStamina() > 0 && m_wishMoveDir.magnitude > 0.05)
		{
			// Stop the player from crouching
			if (IsCrouching)
				TryCrouch(false);

			IsSprinting = true;

		}
		else
		{
			IsSprinting = false;
		}
	}

	private void Jump()
	{
		m_velocity.y += m_movementData.m_jumpForce;
		IsGrounded = false;
		m_justJumped = true;

		playerStats.SetStamina(playerStats.Stamina - playerStats.JumpStaminaReduction);
	}

	#endregion

	#region Collision

	private bool CastHull(Vector3 direction, float maxDist, out RaycastHit hitInfo)
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

	private bool CheckHull()
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

	private bool StuckCheck()
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

	private void CollideAndSlide()
	{
		Vector3 startVelocity = m_velocity;
		Vector3 velocityBeforePlanes = m_velocity;

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

					velocityBeforePlanes = m_velocity;
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
					m_velocity = Vector3.ProjectOnPlane(velocityBeforePlanes, planes[j]);

					if (planeCount == 1)
					{
						velocityBeforePlanes = m_velocity;
					}

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
						m_velocity = dir * Vector3.Dot(dir, m_velocity);
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
			if (Vector3.Dot(m_velocity, startVelocity) <= 0)
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

	private void UpdateCollider()
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

    private void ParentCheck()
    {
        Ray ray = new Ray(transform.position, Vector3.down * k_groundCheckDist);
        if (Physics.Raycast
        (
            ray,
            out RaycastHit hit,
            k_groundCheckDist,
            m_movementData.ParentingLayers,
            QueryTriggerInteraction.Ignore
        ))
        {
            if (hit.transform.TryGetComponent(out ParantableRigidbody parentingRB))
            {
                if (parentRigidbody == null || parentRigidbody != parentingRB)
				{
                    previousParentPosition = parentingRB.transform.position;
                    previousParentRotation = parentingRB.transform.rotation;
                    parentRigidbody = parentingRB;
                }
            }
            else if (parentRigidbody != null)
                parentRigidbody = null;
        }
    }

    private void GroundCheck()
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
				m_SurfaceObject = hit.collider.gameObject;
				ParentCheck();

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

		if (!IsGrounded)
		{
			// Additionally, check if any point on the player is on the ground
			// Source uses 4 box checks, but I'm really lazy so I'll use a raycast

			if (Physics.Raycast(m_position,
				Vector3.down,
				out RaycastHit hit2,
				k_groundCheckDist * 2,
				m_movementData.m_layerMask,
				QueryTriggerInteraction.Ignore
			))
			{
				if (hit2.normal.y > Mathf.Cos(Mathf.Deg2Rad * m_movementData.m_maxWalkableAngle))
				{
					IsGrounded = true;
					m_SurfaceObject = hit2.collider.gameObject;
				}
				else
				{
					IsGrounded = false;
				}
			}

			//Debug.DrawRay(m_position, Vector3.down * k_groundCheckDist, Color.yellow);
		}
	}

    private void CategorizePosition()
	{
		GroundCheck();
	}

	void PlayStepSound()
	{
		AudioClip sound = SurfaceProperties.GetStepSound(SurfaceType.Stone);
		if (m_SurfaceObject.TryGetComponent(out SurfaceData surfaceData))
		{
			SurfaceType surface = surfaceData.SurfaceMaterial;

			int attempts = 0;
			do
			{
				sound = SurfaceProperties.GetStepSound(surface);

				++attempts;

				if (attempts >= 20)
				{
					Debug.LogWarning("Bad RNG, playing the same footstep sound twice :(");
					break;
				}
			} while (sound == m_LastStepSound && SurfaceProperties.GetStepSoundCount(surface) > 1);
		}

		m_LastStepSound = sound;

		m_StepSource.pitch = Random.Range(0.9f, 1.1f);
		m_StepSource.PlayOneShot(sound, m_StepVolume);
	}

	#endregion

	#region Movement Methods

	private void Friction(float friction)
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

	private void Accelerate(Vector3 direction, float acceleration, float maxSpeed)
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

		VaultCheck();

		if (m_movementMode == EMovementMode.Standard)
		{
			CategorizePosition();

			Vector3 globalWishDir = m_fpsCamera.RotateVectorYaw(m_wishMoveDir);

			// Stop sprinting if the player runs out of stamina or stops moving
			if (playerStats.GetStamina() <= 0 || m_wishMoveDir.magnitude <= 0.05)
				TrySprint(false);

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

            // Adjust for parent movement and rotation if applicable
            if (parentRigidbody != null)
			{
                Vector3 parentDelta = parentRigidbody.transform.position - previousParentPosition;
                Quaternion rotationDelta = parentRigidbody.transform.rotation * Quaternion.Inverse(previousParentRotation);

                m_position = rotationDelta * (m_position - parentRigidbody.transform.position) + parentRigidbody.transform.position + parentDelta;

                // Cache parent updates for next frame
                previousParentPosition = parentRigidbody.transform.position;
                previousParentRotation = parentRigidbody.transform.rotation;
            }

            m_justJumped = false;
		}
	}

	private void GroundMove(Vector3 moveDir)
	{
		m_velocity.y = 0;

		// Pick movement speed based on current player state
		float moveSpeed = IsCrouching ? m_movementData.m_crouchingSpeed :
			(IsSprinting ? m_movementData.sprintingSpeed : m_movementData.m_walkingSpeed);

		if (IsSprinting)
			playerStats.SetStamina(playerStats.Stamina - playerStats.SprintStaminaReduction * Time.deltaTime);

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

			GroundCheck();
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

		// Footsteps
		if (Vector3.Distance(m_position, m_LastFootStepPosition) > m_movementData.m_FootStepDist)
		{
			m_LastFootStepPosition = m_position;
			PlayStepSound();
		}
	}

	private void AirMove(Vector3 moveDir)
	{
		Accelerate(moveDir, m_movementData.m_airAcceleration, m_movementData.m_airSpeed);

		m_velocity.y -= m_movementData.m_gravity * Time.fixedDeltaTime / 2f;
		CollideAndSlide();
		m_velocity.y -= m_movementData.m_gravity * Time.fixedDeltaTime / 2f;

		CategorizePosition();

		if (IsGrounded)
		{
			m_LastFootStepPosition = m_position;
		}
	}

	public void Teleport(Vector3 position)
	{
		m_position = position;
		transform.position = m_position;
		m_LastFootStepPosition = position;
	}

	#endregion

	#region Vault Check Methods

	/// <summary>
	/// This method is responsible for checking the environment for objects that the player can vault
	/// </summary>
	public void VaultCheck()
	{
		LayerMask vaultingLayers = MvmtData.VaultingLayers;
		float standingheight = MvmtData.m_standingHeight;
		float playerRadius = MvmtData.m_horizontalSize;

		Vector3 forwardXZ = m_fpsCamera.CameraTransform.forward;
		forwardXZ.y = 0;
		forwardXZ = forwardXZ.normalized;

		// Ensure the player is attempting to move forward and that there is an object infront of them at camera level
		if (m_wishMoveDir.y > 0 &&
			m_isJumpPressed &&
			Physics.Raycast(m_fpsCamera.CameraTransform.position, forwardXZ, out var firstHit, 1f, vaultingLayers))
		{
			// Check if nothing is above the players head
			if (!Physics.Raycast(m_chestHeight.transform.position, transform.up * standingheight, standingheight))
			{
				// Ensure there is free space on top of the object for the player to move to
				if (Physics.Raycast(firstHit.point +
				   (forwardXZ * playerRadius) + (Vector3.up * standingheight * 0.25f), // Length forwards + upwards
					Vector3.down, out var secondHit, standingheight)) // Raycast down to find where the player should stand
				{
					// Make sure there is place for the player to stand after the point ontop of the object is determined
					if (secondHit.collider != null && !Physics.Raycast(secondHit.point, Vector3.up, standingheight))
					{
						// Start the lerp
						StartVault(secondHit.point);
					}
				}
			}
		}
	}

	public void StartVault(Vector3 targetPosition)
	{
		if (vaulting != null)
			StopCoroutine(vaulting);

		vaulting = StartCoroutine(LerpVault(targetPosition));
	}

	/// <summary>
	/// This IEnumerator lerps the player between two points, evaluated by the vaultSpeedCurve. 
	/// These points will be updated to adjust for any changes in the objects position.
	/// </summary>
	public IEnumerator LerpVault(Vector3 targetPosition)
	{
		m_movementMode = EMovementMode.Vaulting;

		m_velocity = Vector3.zero;
		Vector3 startPos = m_position;

		float time = 0;
		while (time < MvmtData.VaultDuration)
		{
			// Lerp between the updated points having the curve evaluate the time
			Teleport(Vector3.Lerp(startPos, targetPosition, MvmtData.VaultSpeedCurve.Evaluate(time / MvmtData.VaultDuration)));

			time += Time.deltaTime;
			yield return null;
		}

		// Snap player to final pos
		Teleport(targetPosition);
		m_movementMode = EMovementMode.Standard;
	}
	#endregion
}
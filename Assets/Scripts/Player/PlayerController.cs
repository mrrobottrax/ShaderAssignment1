using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour, IInputHandler
{
	[field: Header("Walking Movement Values")]
	[SerializeField] float walkingSpeed = 5.5f;
	[SerializeField] float friction = 50;
	[SerializeField] float acceleration = 120;

	[field: Header("Crouching Movement Values")]
	[SerializeField] float crouchingSpeed = 2.5f;
	[SerializeField] float crouchingFriction = 15;
	[SerializeField] float crouchingAcceleration = 30;

	[field: Header("Ground Movement Values")]
	[SerializeField] float jumpForce = 6;
	[SerializeField] float knockUpThreshold = 2; // This much upwards velocity removes the grounded state
	[SerializeField] float stepHeight = 0.7f;
	[SerializeField] float maxWalkableAngle = 55;

	[field: Header("Air Movement Values")]
	[SerializeField] float airSpeed = 1;
	[SerializeField] float airAcceleration = 40;
	[SerializeField] float gravity = 16;

	[field: Header("Swim Movement Values")]
	[SerializeField] float swimSpeed = 3.5f;
	[SerializeField] float swimAcceleration = 10;
	[SerializeField] float swimFriction = 10;


	[field: Header("Collision Values")]
	[SerializeField] LayerMask layerMask = ~(1 << 3);
	[SerializeField] float horizontalSize = 1;
	[SerializeField] new Collider collider;
	public float standingHeight = 2;
	public float crouchingHeight = 1.2f;

	[field: Header("Vaulting")]
	[SerializeField] float maxVaultHeight = 2.5f;
	[SerializeField] float minVaultHeight = 1; // Disallow tiny vaults
	[SerializeField] float maxVaultDist = 0.3f; // Maximum distance to check forwards when vaulting
	public float vaultDuration = 0.5f; // Duration of vault animation

    [field: Header("Other Values")]
	[SerializeField] float climbSpeed = 3;

	[field: Header("Components")]
	FirstPersonCamera fpsCamera;
	PlayerActions playerActions;
	new Rigidbody rigidbody;

	[field: Header("System")]
	Vector2 wishMoveDir;
	Vector3 velocity;
	Vector3 position;

    private bool isMovementEnabled = true;
    private bool isGrounded;

    public bool IsCrouching { get; private set; }
    private bool isCrouchPressed;

    private bool isJumpPressed;

    // Interaction
    Transform interactableParent;
	Transform ladderBottom;
	Transform ladderTop;
	float ladderProgress = 0;
	Climbable_Interaction ladder; // todo: the ladder shouldn't be responsible for the player leaving the ladder

	const float hitEpsilon = 0.015f; // Min distance to wall
	const float groundCheckDist = 0.03f;
	const float stopEpsilon = 0.001f; // Stop when <= this speed
	const int maxBumps = 8; // Max number of iterations per frame
	const int maxPlanes = 5; // Max number of planes to collide with at once

	enum EMovementMode
	{
		Standard,
		Interacting,
		Climbing,
		Noclip
	}
	private EMovementMode movementMode;

	#region Initialization Methods

	void Awake()
	{
		rigidbody = GetComponent<Rigidbody>();
		fpsCamera = GetComponent<FirstPersonCamera>();
		playerActions = GetComponent<PlayerActions>();
	}


	void Start()
	{
		rigidbody.isKinematic = true;
		rigidbody.freezeRotation = true;

		position = transform.position;

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
		if (isMovementEnabled)
		{
			switch (movementMode)
			{
				case EMovementMode.Standard:
					Movement();
					break;
				case EMovementMode.Interacting:
					InteractionMovement();
					break;
				case EMovementMode.Climbing:
					ClimbingMovement();
					break;
			}
		}

        rigidbody.MovePosition(position);
        rigidbody.MoveRotation(Quaternion.identity);
    }

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.green;
		Gizmos.DrawWireCube(position + GetColliderHeight() * 0.5f * Vector3.up, new Vector3(horizontalSize, GetColliderHeight(), horizontalSize));
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

	void OnMoveInput(InputAction.CallbackContext context)
	{
		wishMoveDir = context.ReadValue<Vector2>();
	}

	void OnJumpInput(InputAction.CallbackContext context)
	{
		isJumpPressed = context.ReadValueAsButton();

		if (isJumpPressed && isGrounded)
			Jump();
	}

	void OnCrouchInput(InputAction.CallbackContext context)
	{
        isCrouchPressed = context.ReadValueAsButton();
	}

	public void Subscribe()
	{
		InputManager.Instance.controls.Player.Movement.performed += OnMoveInput;

		InputManager.Instance.controls.Player.Jump.performed += OnJumpInput;
        InputManager.Instance.controls.Player.Jump.canceled += OnJumpInput;

        InputManager.Instance.controls.Player.Crouch.performed += OnCrouchInput;
        InputManager.Instance.controls.Player.Crouch.canceled += OnCrouchInput;

        playerActions.SetControlsSubscription(true);
	}

	public void Unsubscribe()
	{
		InputManager.Instance.controls.Player.Movement.performed -= OnMoveInput;

		InputManager.Instance.controls.Player.Jump.performed -= OnJumpInput;
        InputManager.Instance.controls.Player.Jump.canceled -= OnJumpInput;

        InputManager.Instance.controls.Player.Crouch.performed -= OnCrouchInput;
        InputManager.Instance.controls.Player.Crouch.canceled -= OnCrouchInput;

        wishMoveDir = Vector2.zero;
		playerActions.SetControlsSubscription(false);
	}
    #endregion

    #region Actions

	/// <summary>
	/// Checks if the player can enter either a crouched or uncrouched state
	/// </summary>
	/// <param name="isAttemptingCrouch">What the player is attempting to do</param>
    private void TryCrouch(bool isAttemptingCrouch)
    {
        if (isAttemptingCrouch)
        {
            IsCrouching = true;
        }
        else
        {
            // Make sure there is room
            IsCrouching = false;
            bool hasRoom = !CheckHull();

            if (!hasRoom)
            {
                IsCrouching = true;
            }
        }
    }

    private void Jump()
	{
		velocity.y += jumpForce;
		isGrounded = false;
	}

	private IEnumerator VaultMovementFreeze()
	{
		isMovementEnabled = false;

		yield return new WaitForSeconds(vaultDuration);

		isMovementEnabled = true;
	}

	// Returns true on successful vault
	private bool Vault()
	{
		Vector3 startPosition = position;
		Vector3 startVelocity = velocity;

		// Move up
		if (CastHull(Vector3.up, maxVaultHeight, out RaycastHit hit))
		{
			position += hit.distance * Vector3.up;
		}
		else
		{
			position += maxVaultHeight * Vector3.up;
		}

		// Move forwards
		Vector3 forwards = fpsCamera.RotateVectorYaw(new Vector2(0, 1));
		if (CastHull(forwards, maxVaultDist, out hit))
		{
			position += hit.distance * forwards;
		}
		else
		{
			position += maxVaultDist * forwards;
		}

		// Move back down
		if (CastHull(-Vector3.up, maxVaultHeight, out hit))
		{
			position += hit.distance * -Vector3.up;
		}
		else
		{
			position += maxVaultHeight * -Vector3.up;
		}

		GroundCheck();

		float height = position.y - startPosition.y;
		if (isGrounded && height > minVaultHeight)
		{
			// Successful vault
			velocity = Vector3.zero;
			fpsCamera.Vault(position - startPosition);
			StartCoroutine(VaultMovementFreeze());
			return true;
		}

		position = startPosition;
		velocity = startVelocity;

		return false;
	}
	#endregion

	#region Movement Mode Setters

	public void BeginClimb(Transform ladderBottom, Transform ladderTop, Climbable_Interaction ladder)
	{
		this.ladderBottom = ladderBottom;
		this.ladderTop = ladderTop;
		this.ladder = ladder;

		// todo: this is a silly way of doing this
		// Calculate progress on ladder from 0 to 1
		float dist = Vector3.Distance(ladderBottom.position, ladderTop.position);

		Vector3 relativePosition = position - ladderBottom.position;
		Vector3 ladderUp = (ladderTop.position - ladderBottom.position).normalized;

		ladderProgress = Vector3.Dot(relativePosition, ladderUp) / dist;
		ladderProgress = Mathf.Clamp01(ladderProgress);

		velocity = Vector3.zero;
		movementMode = EMovementMode.Climbing;
	}

	public void DismountClimb()
	{
		movementMode = EMovementMode.Standard;

		// Cast up
		if (CastHull(Vector3.up, 0.1f, out RaycastHit hit))
		{
			position.y += hit.distance;
		}
		else
		{
			position.y += 0.1f;
		}

		// Cast forwards
		Vector3 forwards = fpsCamera.RotateVectorYaw(new Vector2(0, 1));
		if (CastHull(forwards, 0.1f, out hit))
		{
			position += forwards * hit.distance;
		}
		else
		{
			position += forwards * 0.1f;
		}

		// Cast down
		if (CastHull(Vector3.down, 0.1f, out hit))
		{
			position.y -= hit.distance;
		}
		else
		{
			position.y -= 0.1f;
		}
	}

	public void BeginUsingInvolvedInteractable(Transform parent = null, Vector3? offset = null, Quaternion? rotation = null)
	{
		interactableParent = parent;

		movementMode = EMovementMode.Interacting;
	}

	public void EndUsingInvolvedInteractable(Vector3? offset = null, Quaternion? rotation = null)
	{
		movementMode = EMovementMode.Standard;
	}

	#endregion

	#region Collision

	bool CastHull(Vector3 direction, float maxDist, out RaycastHit hitInfo)
	{
		float halfHeight = GetColliderHeight() / 2f;

		bool hit = Physics.BoxCast(position + Vector3.up * halfHeight,
			new Vector3(horizontalSize / 2f, halfHeight, horizontalSize / 2f),
			direction,
			out hitInfo,
			Quaternion.identity,
			maxDist,
			layerMask,
			QueryTriggerInteraction.Ignore
		);

		hitInfo.distance -= hitEpsilon / -Vector3.Dot(direction, hitInfo.normal); // Back up a little
		if (hitInfo.distance < 0) hitInfo.distance = 0;

		return hit;
	}

	bool CheckHull()
	{
		float halfHeight = GetColliderHeight() / 2f;
		return Physics.CheckBox(
			position + Vector3.up * halfHeight,
			new Vector3(horizontalSize / 2f, halfHeight, horizontalSize / 2f),
			Quaternion.identity,
			layerMask,
			QueryTriggerInteraction.Ignore
		);
	}

	void StuckCheck()
	{
		// todo: I know there's a function somewhere to find the vector that would move a collider out from whatever it's stuck in
		// it's Physics.ComputePenetration

		float halfHeight = GetColliderHeight() / 2f;
		Collider[] colliders = Physics.OverlapBox(
			position + Vector3.up * halfHeight,
			new Vector3(horizontalSize / 2f, halfHeight, horizontalSize / 2f),
			Quaternion.identity,
			layerMask,
			QueryTriggerInteraction.Ignore
		);

		if (colliders.Length > 0)
		{
			if (Physics.ComputePenetration(
				collider,
				position,
				Quaternion.identity,
				colliders[0],
				colliders[0].transform.position,
				colliders[0].transform.rotation,
				out Vector3 dir,
				out float dist
			))
			{
				position += dir * (dist + hitEpsilon * 1.5f);
			}
		}
	}

	void CollideAndSlide()
	{
		Vector3 originalVelocity = velocity;

		// When we collide with multiple planes at once (crease)
		Vector3[] planes = new Vector3[maxPlanes];
		int planeCount = 0;

		float time = Time.fixedDeltaTime; // The amount of time remaining in the frame, decreases with each iteration
		int bumpCount;
		for (bumpCount = 0; bumpCount < maxBumps; ++bumpCount)
		{
			float speed = velocity.magnitude;

			if (speed <= stopEpsilon)
			{
				velocity = Vector3.zero;
				break;
			}

			// Try to move in this direction
			Vector3 direction = velocity.normalized;
			float maxDist = speed * time;
			if (CastHull(direction, maxDist, out RaycastHit hit))
			{
				if (hit.distance > 0)
				{
					// Move rigibody to where it collided
					position += direction * hit.distance;

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

				if (planeCount >= maxPlanes)
				{
					Debug.LogWarning("Colliding with too many planes at once");
					velocity = Vector3.zero;
					break;
				}

				planes[planeCount] = hit.normal;
				++planeCount;

				// Clip velocity to each plane
				bool conflictingPlanes = false;
				for (int j = 0; j < planeCount; ++j)
				{
					velocity = Vector3.ProjectOnPlane(originalVelocity, planes[j]);

					// Check if the velocity is against any other planes
					for (int k = 0; k < planeCount; ++k)
					{
						if (j != k) // No point in checking the same plane we just clipped to
						{
							if (Vector3.Dot(velocity, planes[k]) < 0.002f) // Moving into the plane, BAD!
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
						velocity = dir * Vector3.Dot(dir, originalVelocity);
					}
					else
					{
						velocity = Vector3.zero;
						break;
					}
				}
			}
			else
			{
				// Move rigibody according to velocity
				position += direction * maxDist;
				break;
			}

			// Stop tiny oscillations
			if (Vector3.Dot(velocity, originalVelocity) <= 0)
			{
				velocity = Vector3.zero;
				break;
			}

			if (time <= 0)
			{
				Debug.Log("Outta time");
				break; // outta time
			}
		}

		if (bumpCount >= maxBumps)
		{
			Debug.LogWarning("Bumps exceeded");
		}
	}
	#endregion

	#region Utility

	public float GetColliderHeight()
	{
		return IsCrouching ? crouchingHeight : standingHeight;
	}

	void GroundCheck()
	{
		// Allow for force to knock off the ground
		if (velocity.y > knockUpThreshold)
		{
			isGrounded = false;
			return;
		}

		if (CastHull(-Vector3.up, groundCheckDist, out RaycastHit hit))
		{
			if (hit.normal.y > Mathf.Cos(Mathf.Deg2Rad * maxWalkableAngle))
			{
				isGrounded = true;
			}
			else
			{
				isGrounded = false;
			}
		}
		else
		{
			isGrounded = false;
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
		float speed = velocity.magnitude;
		float newSpeed = Mathf.Max(speed - friction * Time.fixedDeltaTime, 0);

		if (speed != 0)
		{
			float mult = newSpeed / speed;
			velocity *= mult;
		}
	}

	void Accelerate(Vector3 direction, float acceleration, float maxSpeed)
	{
		float add = acceleration * Time.fixedDeltaTime;

		// Clamp added velocity in acceleration direction
		float speed = Vector3.Dot(direction, velocity);

		if (speed + add > maxSpeed)
		{
			add = Mathf.Max(maxSpeed - speed, 0);
		}

		velocity += add * direction;
	}

	/// <summary>
	/// Standard player movement 
	/// </summary>
	private void Movement()
	{
		StuckCheck();
		CategorizePosition();

        Vector3 globalWishDir = fpsCamera.RotateVectorYaw(wishMoveDir);

        // Crouch / un-crouch
        if (isCrouchPressed)
        {
            if (!IsCrouching)
            {
                TryCrouch(true);
            }
        }
        else if (IsCrouching)
            TryCrouch(false);

        if (isGrounded)
        {
            GroundMove(globalWishDir);
        }
        else
        {
            AirMove(globalWishDir);
        }
    }

	private void GroundMove(Vector3 moveDir)
	{
		velocity.y = 0;

		float moveSpeed = IsCrouching ? crouchingSpeed : walkingSpeed;
		float acceleration = IsCrouching ? crouchingAcceleration : this.acceleration;
		float friction = IsCrouching ? crouchingFriction : this.friction;

		// Friction
		Friction(friction);

		// Accelerate
		Accelerate(moveDir, acceleration, moveSpeed);
		float speed = velocity.magnitude;

		// Clamp Speed
		if (speed > moveSpeed)
		{
			float mult = moveSpeed / speed;
			velocity *= mult;
		}

		if (speed <= 0)
		{
			return;
		}

		Vector3 startVelocity = velocity;
		Vector3 startPosition = position;

		CollideAndSlide();
		Vector3 downVelocity = velocity;
		Vector3 downPosition = position;

		// Move up and try another move
		{
			velocity = startVelocity;
			position = startPosition;

			if (CastHull(Vector3.up, stepHeight, out RaycastHit hit))
			{
				position += Vector3.up * hit.distance;
			}
			else
			{
				position += Vector3.up * stepHeight;
			}
		}

		CollideAndSlide();

		// Move back down
		{
			if (CastHull(-Vector3.up, stepHeight * 2, out RaycastHit hit))
			{
				position -= Vector3.up * hit.distance;
			}
			else
			{
				position -= 2 * stepHeight * Vector3.up;
			}
		}

		GroundCheck();

		// Either reset to the on ground move results, or keep the step move results

		// If we stepped onto air, just do the regular move
		if (!isGrounded)
		{
			position = downPosition;
			velocity = downVelocity;
		}
		// Otherwise, pick the move that goes the furthest
		else if (Vector3.Distance(startPosition, downPosition) >= Vector3.Distance(startPosition, position))
		{
			position = downPosition;
			velocity = downVelocity;
		}
		else
		{
			fpsCamera.Step(position.y - startPosition.y);
		}
	}

	private void AirMove(Vector3 moveDir)
	{
		Accelerate(moveDir, airAcceleration, airSpeed);

		velocity.y -= (gravity * Time.fixedDeltaTime) / 2f;
		CollideAndSlide();
		velocity.y -= (gravity * Time.fixedDeltaTime) / 2f;

		if (velocity.y > 0)
			Vault();
	}

	private void SwimmingMovement(Vector3 moveDir)
	{
		Friction(swimFriction);

		// Do look Movement
		Accelerate(moveDir, swimAcceleration, swimSpeed);

        // Get heightened control dir
        if (isJumpPressed && !isCrouchPressed)
            moveDir = Vector3.up;
        else if (!isJumpPressed && isCrouchPressed)
            moveDir = Vector3.down;

		// Do heightened control movement
        if (moveDir != Vector3.zero)
            Accelerate(moveDir, swimAcceleration, swimSpeed);

        CollideAndSlide();
	}
    #endregion

    #region External Movement Methods

    /// <summary>
    /// This movement type follows the interaction parent
    /// </summary>
    void InteractionMovement()
    {
        if (interactableParent)
            position = interactableParent.position;
    }

    /// <summary>
    /// This movement type follows the climbing parents climb axis
    /// </summary>
    void ClimbingMovement()
    {
        float climbSign = fpsCamera.pitch > 0 ? -1 : 1;
        float add = (wishMoveDir.y * climbSign * climbSpeed * Time.fixedDeltaTime) / Vector3.Distance(ladderBottom.position, ladderTop.position);
        ladderProgress += add;

        position = Vector3.Lerp(ladderBottom.position, ladderTop.position, ladderProgress);

        if (ladderProgress < 0 || ladderProgress > 1)
        {
            ladder.KickClimber();
        }
    }
    #endregion
}
using UnityEngine;

[CreateAssetMenu]
public class PlayerMovementData : ScriptableObject
{
	[field: Header("Walking Movement Values")]
	public float m_walkingSpeed = 5.5f;
	public float m_friction = 50;
	public float m_acceleration = 120;

	[field: Header("Crouching Movement Values")]
	public float m_crouchingSpeed = 2.5f;
	public float m_crouchingFriction = 15;
	public float m_crouchingAcceleration = 30;

	[field: Header("Ground Movement Values")]
	public float m_jumpForce = 6;

	[Tooltip("This much upwards velocity removes the grounded state.")]
	public float m_knockUpThreshold = 2;

	public float m_stepHeight = 0.7f;
	public float m_maxWalkableAngle = 55;

	[field: Header("Air Movement Values")]
	public float m_airSpeed = 1;
	public float m_airAcceleration = 40;
	public float m_gravity = 16;

	[field: Header("Collision Values")]
	public LayerMask m_layerMask = ~(1 << 3);
	public float m_horizontalSize = 1;
	public float m_standingHeight = 2;
	public float m_crouchingHeight = 1.2f;

	[field: Header("Debug")]
	public float m_noclipAcceleration = 20;
	public float m_noclipFriction = 10;
	public float m_noclipSpeed = 20;
}

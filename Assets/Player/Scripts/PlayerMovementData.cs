using UnityEngine;

//[CreateAssetMenu]
public class PlayerMovementData : ScriptableObject
{
	[field: Header("Ground Movement Values")]
    public float m_walkingSpeed = 5.5f;
    public float sprintingSpeed = 8f;
    public float m_crouchingSpeed = 2.5f;
	public float m_friction = 50;
	public float m_acceleration = 120;
	public float m_stopSpeed = 80;

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

    [field: Header("Parenting")]
    [field: SerializeField] public LayerMask ParentingLayers { get; private set; }

    [field: Header("Vaulting")]
    [field: SerializeField] public LayerMask VaultingLayers { get; private set; }
    [field: SerializeField] public float VaultDuration { get; private set; } = .4f;
    [field: SerializeField] public AnimationCurve VaultSpeedCurve { get; private set; }

    [field: Header("Collision Values")]
	public LayerMask m_layerMask = ~(1 << 3);
	public float m_horizontalSize = 1;
	public float m_standingHeight = 2;
	public float m_crouchingHeight = 1.2f;

	[field: Header("Debug")]
	public float m_noclipAcceleration = 20;
	public float m_noclipFriction = 10;
	public float m_noclipSpeed = 20;

	[field: Header("Sound")]
	public float m_FootStepDist = 1f;
}

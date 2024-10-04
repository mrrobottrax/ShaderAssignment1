using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "Attack", menuName = "AttackData")]
public class AttackData : ScriptableObject
{
    [field: Header("Attack Stats"), Space(10)]
    [field: SerializeField] public int AttackDamage { get; private set; }
    [field: SerializeField] public float AttackRange { get; private set; }
    [field: SerializeField] public float Knockback { get; private set; }
    [field: SerializeField] public float Cooldown { get; private set; }

    [field: Header("Attack Properties"), Space(10)]
    [field: SerializeField] public EAttackApplicationType ApplicationType { get; private set; }
    [field: SerializeField, Range(0, 90)] public float SwingRange { get; private set; }
    [field: SerializeField, Range(0, 180)] public float SwingClockwiseAngle { get; private set; }

    [field: Space(10)]

    [field: SerializeField] public LayerMask AffectedLayers { get; private set; }
    [field: SerializeField] public StatusEffectData_Base[] EffectsApplied { get; private set; }

    [field: SerializeField, Tooltip("When given a projectile, a copy of said projectile will be created on attack.")]
    public ProjectileData ProjectileCreated { get; private set; }

    [field: Space(10)]

    [field: SerializeField, Tooltip("Check a physical range for victems")] 
    public bool IsPhysicalAttack { get; private set; } = true;

    [field: Header("Physical Attack Properties"), Space(10)]
    [field: SerializeField] public EDamageType DamageType { get; private set; }

    // Stun at some point  [field: SerializeField] public float StunDuration { get; private set; }

    [field: Header("Physical FX"), Space(10)]

    [field: SerializeField, Tooltip("The magnitude of the screenshake created on impact")]
    public float ScreenShakeAmplitude { get; private set; }

    [field: SerializeField, Tooltip("The duration of the screenshake created on impact")]
    public float ScreenShakeDuration { get; private set; }
}

public enum EAttackApplicationType
{
    NoneOrProjectile,
    Positional,
    Swinging,
    Hitscan
}

#if UNITY_EDITOR
[CustomEditor(typeof(AttackData))]
public class AttackData_Editor : Editor
{
    SerializedProperty attackDamage;
    SerializedProperty attackRange;
    SerializedProperty knockback;
    SerializedProperty cooldown;
    SerializedProperty applicationType;
    SerializedProperty swingRange;
    SerializedProperty swingClockwiseAngle;
    SerializedProperty affectedLayers;
    SerializedProperty effectsApplied;
    SerializedProperty projectileCreated;
    SerializedProperty isPhysicalAttack;
    SerializedProperty damageType;
    SerializedProperty screenShakeAmplitude;
    SerializedProperty screenShakeDuration;

    void OnEnable()
    {
        attackDamage = serializedObject.FindProperty("<AttackDamage>k__BackingField");
        attackRange = serializedObject.FindProperty("<AttackRange>k__BackingField");
        knockback = serializedObject.FindProperty("<Knockback>k__BackingField");
        cooldown = serializedObject.FindProperty("<Cooldown>k__BackingField");
        applicationType = serializedObject.FindProperty("<ApplicationType>k__BackingField");
        swingRange = serializedObject.FindProperty("<SwingRange>k__BackingField");
        swingClockwiseAngle = serializedObject.FindProperty("<SwingClockwiseAngle>k__BackingField");
        affectedLayers = serializedObject.FindProperty("<AffectedLayers>k__BackingField");
        effectsApplied = serializedObject.FindProperty("<EffectsApplied>k__BackingField");
        projectileCreated = serializedObject.FindProperty("<ProjectileCreated>k__BackingField");
        isPhysicalAttack = serializedObject.FindProperty("<IsPhysicalAttack>k__BackingField");
        damageType = serializedObject.FindProperty("<DamageType>k__BackingField");
        screenShakeAmplitude = serializedObject.FindProperty("<ScreenShakeAmplitude>k__BackingField");
        screenShakeDuration = serializedObject.FindProperty("<ScreenShakeDuration>k__BackingField");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(attackDamage);
        EditorGUILayout.PropertyField(attackRange);
        EditorGUILayout.PropertyField(knockback);
        EditorGUILayout.PropertyField(cooldown);
        EditorGUILayout.PropertyField(applicationType);

        if (applicationType.enumValueIndex == (int)EAttackApplicationType.Swinging)
        {
            EditorGUILayout.PropertyField(swingRange);
            EditorGUILayout.PropertyField(swingClockwiseAngle);
        }

        EditorGUILayout.PropertyField(affectedLayers);
        EditorGUILayout.PropertyField(effectsApplied);
        EditorGUILayout.PropertyField(projectileCreated);
        EditorGUILayout.PropertyField(isPhysicalAttack);

        // Only show physical attack properties if IsPhysicalAttack is true
        if (isPhysicalAttack.boolValue)
        {
            EditorGUILayout.PropertyField(damageType);

            EditorGUILayout.PropertyField(screenShakeAmplitude);
            EditorGUILayout.PropertyField(screenShakeDuration);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif

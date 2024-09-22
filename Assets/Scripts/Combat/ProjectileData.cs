using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "Projectiles", menuName = "ProjectilesData", order = 0)]
public class ProjectileData : ScriptableObject
{
    [field: Header("Projectile Prefab"), Space(10)]
    [field: SerializeField] public Projectile ProjectilePrefab { get; private set; }

    [field: Header("Projectile Stats"), Space(10)]
    [field: SerializeField] public int Damage { get; private set; }
    [field: SerializeField] public float ProjectileLifetime { get; private set; } = 10f;

    [field: Header("Projectile Properties"), Space(10)]
    [field: SerializeField] public EDamageType DamageType { get; private set; }

    [field: Header("Hit Results"), Space(10)]
    [field: SerializeField] public StatusEffectData_Base[] AppliedEffects { get; private set; }

    [field: Space(10)]

    [field: SerializeField] public EProjectileResult HitResult { get; private set; }
    public enum EProjectileResult
    {
        None,// exist until despawn
        Damage, // Damage the first collision then stop
        Destroy,
        Explode,
    }

    [field: Space(10)]
    [field: SerializeField] public ExplosionData ExplosionData { get; private set; }

    /// <summary>
    /// This method creates then returns an instance of this data's projectile prefab
    /// </summary>
    public Projectile CreateProjectileInstance(CombatPacket combatPacket, Vector3 startPos, Vector3 dir, float force, LayerMask affectedLayers = default, Vector3 initVel = default)
    {
        // Instantiate then init the projectile
        Projectile projectile = Instantiate(ProjectilePrefab, null);

        /*
        // Set the packets damage type
        combatPacket.SetPacketDamageVars(damageType: DamageType, statusEffects: AppliedEffects);

        // Move the packet completer to the projectile
        combatPacket.SetPacketCompleter(projectile);
        */

        // Start the projectile
        projectile.Initialize(this, startPos, dir, force, combatPacket, affectedLayers);

        return projectile;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(ProjectileData))]
public class ProjectileDataEditor : Editor
{
    SerializedProperty projectilePrefab;
    SerializedProperty damage;
    SerializedProperty projectileLifetime;
    SerializedProperty damageType;
    SerializedProperty hitResult;
    SerializedProperty appliedEffects;
    SerializedProperty explosionData;

    void OnEnable()
    {
        // Cache the serialized properties
        projectilePrefab = serializedObject.FindProperty("<ProjectilePrefab>k__BackingField");
        damage = serializedObject.FindProperty("<Damage>k__BackingField");
        projectileLifetime = serializedObject.FindProperty("<ProjectileLifetime>k__BackingField");
        damageType = serializedObject.FindProperty("<DamageType>k__BackingField");
        hitResult = serializedObject.FindProperty("<HitResult>k__BackingField");
        appliedEffects = serializedObject.FindProperty("<AppliedEffects>k__BackingField");
        explosionData = serializedObject.FindProperty("<ExplosionData>k__BackingField");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Draw properties in the order they are declared in the script
        EditorGUILayout.PropertyField(projectilePrefab);

        EditorGUILayout.PropertyField(damage);
        EditorGUILayout.PropertyField(projectileLifetime);

        EditorGUILayout.PropertyField(damageType);

        EditorGUILayout.PropertyField(appliedEffects, true);
        EditorGUILayout.PropertyField(hitResult);

        // Only draw the ExplosionData field if HitResult is Explode
        if (hitResult.enumValueIndex == (int)ProjectileData.EProjectileResult.Explode)
        {
            EditorGUILayout.PropertyField(explosionData);
        }

        // Apply any changes to the serialized object
        serializedObject.ApplyModifiedProperties();
    }
}

#endif
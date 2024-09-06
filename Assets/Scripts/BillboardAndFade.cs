using UnityEditor;
using UnityEngine;

public class BillboardAndFade : MonoBehaviour
{
    [Header("Billboard")]
    [SerializeField] private SpriteRenderer _billboardSpriteRenderer;

    [field: Space(10)]

    [SerializeField] private bool _isBillboardAChild;
    [SerializeField] private Transform _billboardTransform;
    [SerializeField, Tooltip("Offset will move the billboard towards the camera")] 
    private float _billboardOffset;

    [Header("Properties")]
    [SerializeField] private float _fadeDistanceStart = 10f; // Distance at which the fading starts when getting closer
    [SerializeField] private float _fadeDistanceEnd = 20f; // Distance at which the object becomes fully transparent when getting further away

    [SerializeField] private float _fadeDistanceCloseStart = 5f; // Distance at which the fading starts when getting too close
    [SerializeField] private float _fadeDistanceEndClose = 0f; // Distance at which the object becomes fully transparent when getting too close

    [Header("Components")]

    private Transform mainCameraTransform;

    [Header("System")]
    private float fadeDistanceStartSqr;
    private float fadeDistanceEndSqr;
    private float fadeDistanceCloseStartSqr;
    private float fadeDistanceEndCloseSqr;

    void Start()
    {
        mainCameraTransform = Camera.main.transform;

        // Calculate sqr distances
        fadeDistanceStartSqr = _fadeDistanceStart * _fadeDistanceStart;
        fadeDistanceEndSqr = _fadeDistanceEnd * _fadeDistanceEnd;
        fadeDistanceCloseStartSqr = _fadeDistanceCloseStart * _fadeDistanceCloseStart;
        fadeDistanceEndCloseSqr = _fadeDistanceEndClose * _fadeDistanceEndClose;
    }

    void Update()
    {
        // Billboard the object
        Vector3 cameraPosition = mainCameraTransform.position;
        Vector3 directionToCamera = (cameraPosition - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(-directionToCamera, Vector3.up);

        // Adjust alpha based on distance
        float distanceSqr = (transform.position - cameraPosition).sqrMagnitude;
        float alpha = 1;

        // Move the billboard transform towards the camera if IsBillboardChild is enabled
        if (_isBillboardAChild && _billboardTransform != null)
            _billboardTransform.position = transform.position + directionToCamera * _billboardOffset;

        // Determine alpha
        if (distanceSqr < fadeDistanceCloseStartSqr)
        {
            alpha = Mathf.Clamp01((distanceSqr - fadeDistanceEndCloseSqr) / (fadeDistanceCloseStartSqr - fadeDistanceEndCloseSqr));
        }
        else if (distanceSqr > fadeDistanceStartSqr)
        {
            alpha = Mathf.Clamp01((fadeDistanceEndSqr - distanceSqr) / (fadeDistanceEndSqr - fadeDistanceStartSqr));
        }

        // Set new alpha based on distance
        Color color = _billboardSpriteRenderer.color;
        color.a = alpha;

        // Apply the new colour data to the sprite renderer
        _billboardSpriteRenderer.color = color;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        // Draw the start fade range
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, _fadeDistanceStart);

        // Draw the end fade range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _fadeDistanceEnd);

        // Draw the close fade range start
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, _fadeDistanceCloseStart);

        // Draw the close fade range end
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _fadeDistanceEndClose);
    }
#endif
}


[CustomEditor(typeof(BillboardAndFade))]
public class BillboardAndFadeEditor : Editor
{
    SerializedProperty billboardSpriteRenderer;

    SerializedProperty isBillboardAChild;
    SerializedProperty billboardTransform;
    SerializedProperty billboardOffset;

    SerializedProperty fadeDistanceStart;
    SerializedProperty fadeDistanceEnd;
    SerializedProperty fadeDistanceCloseStart;
    SerializedProperty fadeDistanceEndClose;

    void OnEnable()
    {
        // Cache the serialized properties
        billboardSpriteRenderer = serializedObject.FindProperty("_billboardSpriteRenderer");

        isBillboardAChild = serializedObject.FindProperty("_isBillboardAChild");
        billboardTransform = serializedObject.FindProperty("_billboardTransform");
        billboardOffset = serializedObject.FindProperty("_billboardOffset");

        fadeDistanceStart = serializedObject.FindProperty("_fadeDistanceStart");
        fadeDistanceEnd = serializedObject.FindProperty("_fadeDistanceEnd");
        fadeDistanceCloseStart = serializedObject.FindProperty("_fadeDistanceCloseStart");
        fadeDistanceEndClose = serializedObject.FindProperty("_fadeDistanceEndClose");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(billboardSpriteRenderer);
        EditorGUILayout.PropertyField(isBillboardAChild);

        if (isBillboardAChild.boolValue)
        {
            EditorGUILayout.PropertyField(billboardTransform);
            EditorGUILayout.PropertyField(billboardOffset);
        }

        EditorGUILayout.PropertyField(fadeDistanceStart);
        EditorGUILayout.PropertyField(fadeDistanceEnd);
        EditorGUILayout.PropertyField(fadeDistanceCloseStart);
        EditorGUILayout.PropertyField(fadeDistanceEndClose);

        serializedObject.ApplyModifiedProperties();
    }
}



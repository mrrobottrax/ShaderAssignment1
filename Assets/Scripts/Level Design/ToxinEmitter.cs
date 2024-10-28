using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class ToxinEmitter : MonoBehaviour
{
    [Header("Emitter Properties")]
    [SerializeField] private float _EmmisionRadius;
    [SerializeField] private int _toxinLevel;
    [SerializeField] private LayerMask _entityLayers;

    private void Awake()
    {
        SphereCollider sphereCollider = GetComponent<SphereCollider>();
        sphereCollider.isTrigger = true;
        sphereCollider.radius = _EmmisionRadius;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the collider is on the entity layer
        if (((1 << other.gameObject.layer) & _entityLayers) != 0)
        {
            PlayerStats player = other.GetComponent<PlayerStats>();
            if (player != null)
                player.AddToxinLevel(_toxinLevel);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Check if the collider is on the entity layer
        if (((1 << other.gameObject.layer) & _entityLayers) != 0)
        {
            PlayerStats player = other.GetComponent<PlayerStats>();
            if (player != null)
                player.RemoveToxinLevel(_toxinLevel);
        }
    }

    #region Debug Methods
#if DEBUG

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _EmmisionRadius);
    }
#endif
    #endregion
}
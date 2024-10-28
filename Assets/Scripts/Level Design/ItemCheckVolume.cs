using System.Linq;
using UnityEngine;

public class ItemCheckVolume : MonoBehaviour
{
    [SerializeField] private Vector3 _center;
    [SerializeField] private Vector3 _size;

    [SerializeField] private LayerMask _itemsLayer;

    public Item[] CheckItems()
    {
        Collider[] colliders = Physics.OverlapBox(_center, _size / 2, Quaternion.identity, _itemsLayer);

        // Ensure only items are passed
        Item[] items = colliders.Select(collider => collider.GetComponent<Item>())
            .Where(item => item != null)
            .ToArray();

        return items;
    }

    #region Debug Methods
#if DEBUG

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position + _center, _size);
    }
#endif
    #endregion
}

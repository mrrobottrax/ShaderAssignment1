using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OreVein : MonoBehaviour
{
    [SerializeField] private GameObject _ore;

    public void SpawnOre(Vector3 hitPos)
    {
        Instantiate(_ore, hitPos, Quaternion.identity, null);
    }
}

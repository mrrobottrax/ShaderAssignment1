using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OreVein : MonoBehaviour
{
    [SerializeField] private GameObject _ore;
    [SerializeField] private ParticleSystem _sparks;

    public void SpawnOre(Vector3 hitPos, Vector3 hitDirection)
    {
        Instantiate(_ore, hitPos, Quaternion.identity, null);
        _sparks.transform.position = hitPos;
        _sparks.transform.rotation = Quaternion.LookRotation(-hitDirection);
        _sparks.Play();
    }
}

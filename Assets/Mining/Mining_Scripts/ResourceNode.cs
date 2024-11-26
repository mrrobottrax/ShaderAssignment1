using UnityEngine;

public class ResourceNode : DestructableObject
{
    [Header("Loot")]
    [SerializeField, Range(0,1)] private float hitDropChance;
    [SerializeField] private float destructionDropSize;
    [SerializeField] private Item resourceDropped;

    [Header("FX")]
    [SerializeField] private ParticleSystem debrisParticles;
    [SerializeField] private ParticleSystem destructionParticles;

    public override void SetHealth(int value)
    {
        base.SetHealth(value);

        if(Health <= 0)
        {
            // Create multiple drops
            for (int i = 0; i < destructionDropSize; i++)
                Instantiate(resourceDropped, transform.position, Quaternion.identity, null);

            if (destructionParticles != null)
            {
                destructionParticles.transform.parent = null;
                destructionParticles.Play();
            }
        }
    }

    public override void TakeDamage(int amount, Vector3 hitPos, Vector3 hitDirection)
    {
        base.TakeDamage(amount, hitPos, hitDirection);

        // Drop one resource
        if (Random.Range(0, 1) < hitDropChance)
            Instantiate(resourceDropped, hitPos, Quaternion.identity, null);

        if (debrisParticles != null)
        {
            ParticleSystem debris = Instantiate(debrisParticles, hitPos, Quaternion.LookRotation(-hitDirection), null);
            debris.Play();
        }
    }
}

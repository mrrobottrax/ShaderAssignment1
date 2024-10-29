using UnityEngine;

public class MatBarrelInteraction : Interactable
{
    [SerializeField] EventInteraction[] _interactions;
    Interaction[] interactions;

    [field: SerializeField] private Material noLighting;
    [field: SerializeField] private Material ambientLighting;
    [field: SerializeField] private Material specularLighting;
    [field: SerializeField] private Material AmbientSpec;
    [field: SerializeField] private Material Custom;

    private void Awake()
    {
        // Copy from inspector friendly struct to classes
        interactions = new Interaction[_interactions.Length];
        for (int i = 0; i < interactions.Length; ++i)
        {
            int j = i; // This is a stupid hack but unfortunately it has to be here
            interactions[i] = new Interaction()
            {
                prompt = _interactions[i].prompt,
                sprite = _interactions[i].sprite,
                interact = (interactor) => _interactions[j].interact.Invoke(),
            };
        }
    }

    public override Interaction[] GetInteractions()
    {
        return interactions;
    }

    // Interaction Methods
    public void ApplyNoLighting()
    {
        // Code to apply no lighting material
        Debug.Log("Applying No Lighting Material");
        GetComponent<Renderer>().material = noLighting;
    }

    public void ApplyAmbientLighting()
    {
        // Code to apply ambient lighting material
        Debug.Log("Applying Ambient Lighting Material");
        GetComponent<Renderer>().material = ambientLighting;
    }

    public void ApplySpecularLighting()
    {
        // Code to apply specular lighting material
        Debug.Log("Applying Specular Lighting Material");
        GetComponent<Renderer>().material = specularLighting;
    }

    public void ApplyAmbientSpecLighting()
    {
        // Code to apply ambient and specular lighting material
        Debug.Log("Applying Ambient + Specular Lighting Material");
        GetComponent<Renderer>().material = AmbientSpec;
    }

    public void ApplyCustomLighting()
    {
        // Code to apply custom lighting material
        Debug.Log("Applying Custom Lighting Material");
        GetComponent<Renderer>().material = Custom;
    }

}

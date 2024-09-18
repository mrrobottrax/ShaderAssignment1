using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Polaroid : MonoBehaviour
{
    public RawImage polaroidImage;
    
    [SerializeField] RawImage vingette;

    void Start()
    {
        polaroidImage.color = Color.white;
        polaroidImage.color = new Color(polaroidImage.color.r, polaroidImage.color.g, polaroidImage.color.b, 0);
        vingette.gameObject.SetActive(true);
    }
    void Update()
    {
        if (polaroidImage.color.a < 255)
        {
            polaroidImage.color = new Color(polaroidImage.color.r, polaroidImage.color.g, polaroidImage.color.b, polaroidImage.color.a + (Time.deltaTime * 0.1f));
        }
    }

    public void SetImageTexture(Texture newTexture)
    {
        Material materialInstance = new Material(polaroidImage.material);

        // Change the texture on the material instance
        materialInstance.SetTexture("_Texture", newTexture);

        // Apply the new material to the RawImage
        polaroidImage.material = materialInstance;
    }
}

using TMPro;
using UnityEngine;

namespace CaptureCamera
{
    public class CameraCapture : MonoBehaviour
    {
        [SerializeField] UnityEngine.Camera captureCamera;

        [SerializeField] TextMeshProUGUI text;

        [SerializeField] LayerMask LayerMask;
    
        [SerializeField] private GameObject photoPrefab;

        [SerializeField] int resolutionWidth = 300; // Width of the captured image
        [SerializeField] int resolutionHeight = 300; // Height of the captured image

        [SerializeField] private float fadeMultiplyer;
        [SerializeField] private float castStartPoint;
        [SerializeField] private float cameraRadius;
    
        private WebCamTexture webcamTexture;
        void Start()
        {
            // Capture and convert the frame when the scene starts
            //Texture2D capturedTexture = CaptureFrame();
        
        
            // webcamTexture = new WebCamTexture(); //For selfie
            // webcamTexture.Play();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                TakeInGamePhoto();
            }
        }

        void TakeInGamePhoto()
        {
            GameObject photo = Instantiate(photoPrefab, transform.position, Quaternion.identity);
            
            photo.GetComponent<Polaroid>().polaroidImage.material.SetTexture("_Texture", CaptureFrame());
            photo.GetComponent<Polaroid>().SetImageTexture(CaptureFrame());
            
            FindObjectsInView();
        }

        void TakeOutOfGamePhoto()
        {
            //photo.GetComponent<Material>().SetTexture("_Texture", CaptureFrame());
            
            // Texture2D photo = new Texture2D(webcamTexture.width, webcamTexture.height); //for selfie
            // photo.SetPixels(webcamTexture.GetPixels());
            // photo.Apply();
            //
            // mat.SetTexture("_Texture", photo);
        }

        void FindObjectsInView()
        {
            //text.text = "";
            RaycastHit hit;

            Vector3 p1 = transform.position + (transform.forward * castStartPoint);
        
            Collider[] hitColliders = Physics.OverlapSphere(p1, cameraRadius, LayerMask);

            if (hitColliders.Length > 0)
            {
                foreach (Collider hitCollider in hitColliders)
                {
                    Debug.Log("Enveloped: " + hitCollider.name);
  
                    if (hitCollider.gameObject.GetComponent<CaptureCameraTarget>().IsObjectInView())
                    {
                        //text.text += hitCollider.name + " ";
                    }
                }
            }
            else
            {
                print("empty");
            }
        }

        Texture2D CaptureFrame()
        {
            // Set up a RenderTexture with the desired resolution
            RenderTexture renderTexture = new RenderTexture(resolutionWidth, resolutionHeight, 24);
            captureCamera.targetTexture = renderTexture;
        
            // Render the camera's view
            captureCamera.Render();
        
            // Set the active RenderTexture so we can read pixels from it
            RenderTexture.active = renderTexture;
        
            // Create a new Texture2D to store the captured image
            Texture2D capturedTexture = new Texture2D(resolutionWidth, resolutionHeight, TextureFormat.RGB24, false);
        
            // Read the pixels from the active RenderTexture
            capturedTexture.ReadPixels(new Rect(0, 0, resolutionWidth, resolutionHeight), 0, 0);
            capturedTexture.Apply();
        
            // Clean up
            captureCamera.targetTexture = null;
            RenderTexture.active = null;
            Destroy(renderTexture);
        
            return capturedTexture;
        }
    }
}


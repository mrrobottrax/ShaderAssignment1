using UnityEngine;

namespace CaptureCamera
{
    public class CaptureCameraComponent : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] Camera _camera;
        [SerializeField] LayerMask _targetLayers;

        [Header("Capture Properites")]
        [SerializeField] int cameraLensWidth = 300; // Width of the captured image
        [SerializeField] int cameraLensHeight = 300; // Height of the captured image

        [SerializeField] private float fadeMultiplyer;
        [SerializeField] private float castStartPoint;
        [SerializeField] private float cameraRadius;

        [Header("System")]
        [SerializeField] private Polaroid _photoPrefab;

        /// <summary>
        /// Prints a photo gameobject
        /// </summary>
        public GameObject PrintPhysicalPhoto(Vector3 pos, Quaternion rot)
        {
            Polaroid photo = Instantiate(_photoPrefab, pos, rot, null);

            // Apply frame as a texture to the created photo
            photo.polaroidImage.material.SetTexture("_Texture", CaptureFrame(cameraLensWidth, cameraLensHeight));
            photo.SetImageTexture(CaptureFrame(cameraLensWidth, cameraLensHeight));

            FindObjectsInView();

            return photo.gameObject;
        }


        public GameObject PrintPhysicalPhoto()
        {
            return PrintPhysicalPhoto(transform.position, Quaternion.identity);
        }

        #region Photo Capture methods

        private void FindObjectsInView()
        {

            RaycastHit hit;

            Vector3 p1 = transform.position + (transform.forward * castStartPoint);
        
            Collider[] hitColliders = Physics.OverlapSphere(p1, cameraRadius, _targetLayers);

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

        /// <summary>
        /// Turns a frame into a texture
        /// </summary>
        /// <returns></returns>
        private Texture2D CaptureFrame(int resolutionWidth, int resolutionHeight)
        {
            // Set up a RenderTexture with the desired resolution
            RenderTexture renderTexture = new RenderTexture(resolutionWidth, resolutionHeight, 24);
            _camera.targetTexture = renderTexture;

            // Render the camera's view
            _camera.Render();
        
            // Set the active RenderTexture so we can read pixels from it
            RenderTexture.active = renderTexture;
        
            // Create a new Texture2D to store the captured image
            Texture2D capturedTexture = new Texture2D(resolutionWidth, resolutionHeight, TextureFormat.RGB24, false);
        
            // Read the pixels from the active RenderTexture
            capturedTexture.ReadPixels(new Rect(0, 0, resolutionWidth, resolutionHeight), 0, 0);
            capturedTexture.Apply();

            // Clean up
            _camera.targetTexture = null;
            RenderTexture.active = null;
            Destroy(renderTexture);
        
            return capturedTexture;
        }
        #endregion
    }
}


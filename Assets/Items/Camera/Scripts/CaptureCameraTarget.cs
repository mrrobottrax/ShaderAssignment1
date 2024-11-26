using UnityEngine;

namespace CaptureCamera
{
    public class CaptureCameraTarget : MonoBehaviour
    {
    
        [SerializeField] Camera mainCamera;
        Renderer objRenderer;

        void Start()
        {
            //objRenderer = GetComponent<Renderer>(); // Assuming the object has a Renderer
        }
        public bool IsObjectInView()
        {
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(mainCamera);
            return GeometryUtility.TestPlanesAABB(planes, objRenderer.bounds);
        }
    }
}
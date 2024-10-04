using UnityEngine;

namespace CaptureCamera
{
	public class CameraItem : UseableItem
    {
		[Header("Components")]
		[SerializeField] private Camera _camera;
		[SerializeField] private LayerMask _targetLayers;
		[SerializeField] private Polaroid _photoPrefab;

		[Header("Capture Properites")]
		[SerializeField] private int cameraLensWidth = 300; // Width of the captured image
		[SerializeField] private int cameraLensHeight = 300; // Height of the captured image

		[SerializeField] private float fadeMultiplyer;
		[SerializeField] private float castStartPoint;
		[SerializeField] private float cameraRadius;

		#region Initialization Methods

		new void Awake()
		{
			base.Awake();

			_camera.enabled = false;
		}

		#endregion

        public override void TryModelFunction(PlayerHealth player, PlayerViewmodelManager viewModelManager, Vector3 attackPos, string actionTitle, AttackList.Attack attack = null)
        {
            if(actionTitle == "Capture")
            {
                GameObject photo = PrintPhysicalPhoto(transform.position, transform.rotation);
                ownerInventory.AddItem(photo.GetComponent<Item>(), false);
            }
        }

		#region Photo Capture methods

		/// <summary>
		/// Prints a photo gameobject
		/// </summary>
		public GameObject PrintPhysicalPhoto(Vector3 pos, Quaternion rot)
		{
			Polaroid photo = Instantiate(_photoPrefab, pos, Quaternion.identity, null);
			photo.transform.rotation = rot;

			// Apply frame as a texture to the created photo
			photo.polaroidImage.material.SetTexture("_Texture", CaptureFrame(cameraLensWidth, cameraLensHeight));
			photo.SetImageTexture(CaptureFrame(cameraLensWidth, cameraLensHeight));

			return photo.gameObject;
		}

		private void FindObjectsInView()
		{

			//RaycastHit hit;

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
		private Texture2D CaptureFrame(int resolutionWidth, int resolutionHeight)
		{
			// Set up a RenderTexture with the desired resolution
			RenderTexture renderTexture = new(resolutionWidth, resolutionHeight, 24);
			_camera.targetTexture = renderTexture;

			// Render the camera's view
			_camera.enabled = true;
			_camera.Render();
			_camera.enabled = false;

			// Set the active RenderTexture so we can read pixels from it
			RenderTexture.active = renderTexture;

			// Create a new Texture2D to store the captured image
			Texture2D capturedTexture = new(resolutionWidth, resolutionHeight, TextureFormat.RGB24, false);

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


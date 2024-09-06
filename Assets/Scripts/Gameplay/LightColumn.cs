using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LightColumn : MonoBehaviour
{
    [Header("ColumnProperties")]
    [SerializeField] private LayerMask _refelctiveLayer;
    [SerializeField] private LayerMask _stoppingLayers;
    [SerializeField] private float _columnLength = 20f;
    [SerializeField] private int _maxBounces = 4;

    [Header("Components")]
    private LineRenderer lineRenderer;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    /// <summary>
    /// This method will cast a beam of light from a starting point in space in a specified direction
    /// </summary>
    /// <param name="start">The starting point</param>
    /// <param name="dir">The starting direction</param>
    public void CastLight(Vector3 start, Vector3 dir)
    {
        // Create first ray
        Ray ray = new Ray(start, dir);
        RaycastHit hit;

        // Set first position
        lineRenderer.positionCount = 1;
        lineRenderer.SetPosition(0, start);

        // Check for potential reflections
        for (int i = 0; i < _maxBounces; i++)
        {
            // Check the current ray for reflection
            if (Physics.Raycast(ray.origin, ray.direction, out hit, _columnLength))
            {
                if ((_refelctiveLayer & (1 << hit.collider.gameObject.layer)) != 0)
                {
                    // Set this as a line point to potentially bounce from
                    lineRenderer.positionCount++;
                    lineRenderer.SetPosition(lineRenderer.positionCount - 1, hit.point);

                    // Set the new ray since there was a reflection
                    ray = new Ray(hit.point, Vector3.Reflect(ray.direction, hit.normal));
                }
                else if ((_stoppingLayers & (1 << hit.collider.gameObject.layer)) != 0) 
                {
                    // Set the next point to the hit point and break the loop
                    lineRenderer.positionCount++;
                    lineRenderer.SetPosition(lineRenderer.positionCount - 1, hit.point);
                    break;
                }
                
            }
            else
            {
                // Set the next point to the full length of a column since nothing was hit
                lineRenderer.positionCount++;
                lineRenderer.SetPosition(lineRenderer.positionCount - 1, ray.origin + (ray.direction * _columnLength));
                break;
            }
        }
    }
}

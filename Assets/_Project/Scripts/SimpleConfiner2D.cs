using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class SimpleConfiner2D : MonoBehaviour
{
    public Collider2D boundsCollider;
    public Transform target;

    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (boundsCollider == null || target == null)
            return;

        Vector3 desiredPos = target.position;
        desiredPos.z = transform.position.z;

        if (target.gameObject.activeInHierarchy == false)
        {
            desiredPos.x = -0.75f;
            transform.position = desiredPos;
        }
        else transform.position = Confine(desiredPos);
    }

    private Vector3 Confine(Vector3 position)
    {
        Bounds bounds = boundsCollider.bounds;

        float camHeight = cam.orthographicSize * 2f;
        float camWidth = camHeight * cam.aspect;

        float halfHeight = camHeight / 2f;
        float halfWidth = camWidth / 2f;

        float minX = bounds.min.x + halfWidth;
        float maxX = bounds.max.x - halfWidth;
        float minY = bounds.min.y + halfHeight;
        float maxY = bounds.max.y - halfHeight;

        position.x = Mathf.Clamp(position.x, minX, maxX);
        position.y = Mathf.Clamp(position.y, minY, maxY);

        return position;
    }
}
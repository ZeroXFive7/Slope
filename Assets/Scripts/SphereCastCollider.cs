using UnityEngine;
using System.Collections;

public class SphereCastCollider : MonoBehaviour
{
    private const int GizmoLineCount = 8;

    [SerializeField]
    private float radius = 0.5f;
    [SerializeField]
    private float maxDistance = 0.5f;

    [HideInInspector]
    public LayerMask LayerMask;

    private bool isColliding = false;

    public bool TryGetCollisions(out RaycastHit[] collisions)
    {
        collisions = Physics.SphereCastAll(transform.position, radius, transform.up, maxDistance, LayerMask.value);
        isColliding = collisions.Length > 0;

        if (collisions.Length > 0 && collisions[0].point == Vector3.zero && collisions[0].normal == -transform.up)
        {
            // Ignore raycast results from raycast originating in collider.
            collisions = null;
            isColliding = false;
        }
        return isColliding;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isColliding ? Color.red : Color.green;

        // Draw top.
        Gizmos.DrawWireSphere(transform.position, radius);

        // Draw lines connecting top and bottom.
        float deltaTheta = 360.0f / (float)GizmoLineCount;
        for (int i = 0; i < GizmoLineCount; ++i)
        {
            float theta = i * deltaTheta;
            Vector3 offset = radius * (Mathf.Cos(theta) * transform.right + Mathf.Sin(theta) * transform.forward);
            Gizmos.DrawLine(transform.position + offset, transform.position + transform.up * maxDistance + offset);
        }

        // Draw bottom.
        Gizmos.DrawWireSphere(transform.position + transform.up * maxDistance, radius);
    }
}

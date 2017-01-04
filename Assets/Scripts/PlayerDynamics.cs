using UnityEngine;

public class PlayerDynamics : MonoBehaviour
{
    // Component references.
    private new CharacterController collider = null;

    // Local state.
    private Vector3 previousPosition = Vector3.zero;

    [System.NonSerialized]
    public float Steering = 0.0f;

    public void Reset(Transform snapToTransform = null)
    {
        if (snapToTransform)
        {
            transform.position = snapToTransform.position;
            transform.rotation = snapToTransform.rotation;
        }

        previousPosition = transform.position;
    }

    private void Awake()
    {
        collider = GetComponent<CharacterController>();
    }

    private void FixedUpdate()
    {
        Vector3 acceleration = Vector3.zero;

        if (!collider.isGrounded || !CollidedWith(CollisionFlags.Below))
        {
            acceleration += Physics.gravity;
        }
        else
        {
            Vector3 surfaceNormal = GetSurfaceNormal();
            acceleration = Vector3.ProjectOnPlane(Physics.gravity, surfaceNormal);
        }

        Vector3 velocity = (transform.position - previousPosition) / Time.fixedDeltaTime;
        previousPosition = transform.position;

        velocity += acceleration * Time.fixedDeltaTime;
        collider.Move(velocity * Time.fixedDeltaTime);
    }

    #region Helpers

    private bool CollidedWith(CollisionFlags flag)
    {
        return (collider.collisionFlags & flag) != 0;
    }

    private Vector3 GetSurfaceNormal()
    {
        Vector3 normal = Vector3.up;

        RaycastHit hit;
        if (Physics.SphereCast(transform.position + transform.up * collider.height / 2.0f, collider.radius, -transform.up, out hit, collider.height))
        {
            normal = hit.normal;
        }

        return normal;
    }

    #endregion
}

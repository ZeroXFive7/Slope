using UnityEngine;

public class PlayerDynamics : MonoBehaviour
{
    [Header("Physical Properties")]
    [SerializeField]
    private float mass = 1.0f;
    [SerializeField]
    private float kineticFrictionCoefficient = 0.1f;
    [SerializeField]
    private float staticFrictionCoefficient = 0.3f;

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
        // Measure velocity.
        Vector3 velocity = (transform.position - previousPosition) / Time.fixedDeltaTime;
        previousPosition = transform.position;

        // Integrate velocity.
        Vector3 acceleration = GetForces(velocity) / mass;
        velocity += acceleration * Time.fixedDeltaTime;

        // Integrate position.
        collider.Move(velocity * Time.fixedDeltaTime);
    }

    #region Helpers

    private bool CollidedWith(CollisionFlags flag)
    {
        return (collider.collisionFlags & flag) != 0;
    }

    private Vector3 GetForces(Vector3 velocity)
    {
        Vector3 forceGravity = mass * Physics.gravity;
        Vector3 forceFriction = Vector3.zero;

        if (collider.isGrounded && CollidedWith(CollisionFlags.Below))
        {
            Vector3 surfaceNormal = GetSurfaceNormal();

            Vector3 forceTangent = Vector3.ProjectOnPlane(forceGravity, surfaceNormal);
            Vector3 forceNormal = Vector3.Project(forceGravity, surfaceNormal);

            if (velocity.magnitude > 0.001f)
            {
                // Kinetic friction.
                forceFriction = -velocity.normalized * forceNormal.magnitude * kineticFrictionCoefficient;
            }
            else
            {
                // Static friction.
                forceFriction = -forceTangent.normalized * forceNormal.magnitude * staticFrictionCoefficient;
            }

            forceGravity = forceTangent;
        }

        return forceGravity + forceFriction;
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

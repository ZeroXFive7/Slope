using UnityEngine;

public class PlayerDynamics : MonoBehaviour
{
    #region Tunables

    [Header("Physical Properties")]
    [SerializeField]
    private float mass = 1.0f;
    [SerializeField]
    private float kineticFrictionCoefficient = 0.1f;
    [SerializeField]
    private float staticFrictionCoefficient = 0.3f;

    [Header("Controller Behaviors")]
    [SerializeField]
    private float defaultSurfaceDistance = 0.01f;
    [SerializeField]
    private float surfaceClampingThreshold = 0.075f;

    [SerializeField]
    private float maxTurnSpeed = 90.0f;
    [SerializeField]
    private float maxTurnRadius = 30.0f;
    [SerializeField]
    private float minTurnRadius = 3.0f;

    [SerializeField]
    private Transform debugLine = null;

    #endregion

    #region Fields

    // Component references.
    private new CharacterController collider = null;

    // Local state.
    private Vector3 velocity = Vector3.zero;

    [System.NonSerialized]
    public Vector3 DesiredForward = Vector3.forward;

    #endregion

    #region Statics

    private const float SURFACE_RAYCAST_OFFSET_DISTANCE = 0.05f;

    private static readonly Vector3[] SURFACE_RAYCAST_OFFSET_LOCAL_DIRECTIONS =
    {
        new Vector3(0.0f, 0.0f, 0.0f),
        new Vector3(0.0f, 0.0f, 1.0f),
        new Vector3(0.0f, 0.0f, -1.0f),
        new Vector3(1.0f, 0.0f, 0.0f),
        new Vector3(-1.0f, 0.0f, 0.0f)
    };

    #endregion

    public void Reset(Transform snapToTransform = null)
    {
        if (snapToTransform)
        {
            transform.position = snapToTransform.position;
            transform.rotation = snapToTransform.rotation;
        }

        velocity = Vector3.zero;
    }

    private void Awake()
    {
        collider = GetComponent<CharacterController>();
    }

    private void FixedUpdate()
    {
        Vector3 surfaceNormal = Vector3.zero;
        Vector3 surfacePoint = Vector3.zero;
        bool isGrounded = TryGetSurface(defaultSurfaceDistance, out surfaceNormal, out surfacePoint);

        // Project velocity into surface plane to avoid popping.
        if (isGrounded)
        {
            velocity = Vector3.ProjectOnPlane(velocity, surfaceNormal);
        }

        // Integrate velocity.
        Vector3 acceleration = GetForces(velocity, isGrounded, surfaceNormal) / mass;
        velocity += acceleration * Time.fixedDeltaTime;

        float angularSpeed = GetAngularSpeed(velocity, isGrounded);
        Quaternion rotation = Quaternion.AngleAxis(angularSpeed * Time.fixedDeltaTime, Vector3.up);
        velocity = rotation * velocity;

        // Integrate position.
        bool wasGrounded = isGrounded;
        collider.Move(velocity * Time.fixedDeltaTime);

        // Try to snap to surface.
        if (wasGrounded)
        {
            isGrounded = TryGetSurface(surfaceClampingThreshold, out surfaceNormal, out surfacePoint);
            if (isGrounded)
            {
                collider.Move(surfacePoint - transform.position);
            }
            else
            {
                Debug.Log("NOT GROUNDED");
            }
        }

        // Rotate to face velocity.
        if (isGrounded)
        {
            debugLine.gameObject.SetActive(true);
            debugLine.transform.position = transform.position;
            debugLine.transform.rotation = Quaternion.LookRotation(surfaceNormal);
            debugLine.transform.localScale = new Vector3(1.0f, 1.0f, 5.0f);

            Vector3 velocityForward = Vector3.ProjectOnPlane(velocity, surfaceNormal);
            if (velocityForward.sqrMagnitude >= Mathf.Epsilon)
            {
                transform.rotation = Quaternion.LookRotation(velocityForward.normalized, surfaceNormal);
            }
        }
        else
        {
            debugLine.gameObject.SetActive(false);

            Vector3 velocityForward = Vector3.ProjectOnPlane(velocity, Vector3.up);
            if (velocityForward.sqrMagnitude >= Mathf.Epsilon)
            {
                transform.rotation = Quaternion.LookRotation(velocityForward.normalized, surfaceNormal);
            }
        }
    }

    #region Dynamics

    private Vector3 GetForces(Vector3 velocity, bool isGrounded, Vector3 surfaceNormal)
    {
        Vector3 forceGravity = mass * Physics.gravity;
        Vector3 forceFriction = Vector3.zero;

        if (isGrounded)
        {
            Vector3 forwardOnSurface = Vector3.ProjectOnPlane(transform.forward, surfaceNormal).normalized;

            // Calculate forces in surface plane.
            Vector3 forceTangent = Vector3.ProjectOnPlane(forceGravity, surfaceNormal);
            Vector3 forceParallel = Vector3.Project(forceTangent, forwardOnSurface);
            Vector3 forcePerpendicular = forceTangent - forceParallel;

            // Calculate force normal to surface plane.
            Vector3 forceNormal = Vector3.Project(forceGravity, surfaceNormal);

            float frictionMagnitude = forceNormal.magnitude + forcePerpendicular.magnitude;

            if (velocity.magnitude > 0.001f)
            {
                // Kinetic friction.
                forceFriction = -velocity.normalized * frictionMagnitude * kineticFrictionCoefficient;
            }
            else
            {
                // Static friction.
                forceFriction = -forceTangent.normalized * frictionMagnitude * staticFrictionCoefficient;
            }

            forceGravity = forceTangent;
        }

        return forceGravity + forceFriction;
    }

    private float GetAngularSpeed(Vector3 linearVelocity, bool isGrounded)
    {
        Vector3 desiredForwardPlanar = Vector3.ProjectOnPlane(DesiredForward, transform.up).normalized;
        float angleToDesiredForward = Vector3.Angle(transform.forward, desiredForwardPlanar);
        float desiredSpeed = angleToDesiredForward / Time.fixedDeltaTime;

        float direction = Mathf.Sign(Vector3.Dot(Vector3.Cross(transform.forward, DesiredForward), transform.up));

        float linearSpeed = linearVelocity.magnitude;
        if (!isGrounded || linearSpeed <= Mathf.Epsilon || angleToDesiredForward <= Mathf.Epsilon)
        {
            return 0.0f;
        }

        float speed = Mathf.Max(maxTurnSpeed, desiredSpeed);
        return direction * speed;
    }

    #endregion

    #region Helpers

    private bool CollidedWith(CollisionFlags flag)
    {
        return (collider.collisionFlags & flag) != 0;
    }

    private bool TryGetSurface(float maxDistance, out Vector3 surfaceNormal, out Vector3 surfacePoint)
    {
        float capHeight = collider.radius;
        float bodyHeight = collider.height - 2.0f * capHeight;
        float rayOriginHeight = capHeight + bodyHeight;
        Vector3 origin = transform.position + transform.up * rayOriginHeight;
        return TryGetSurfaceNormal(origin, -transform.up, rayOriginHeight + maxDistance, out surfaceNormal, out surfacePoint);
    }

    private bool TryGetSurfaceNormal(Vector3 raycastOrigin, Vector3 raycastDirection, float maxRaycastDistance, out Vector3 surfaceNormal, out Vector3 surfacePoint)
    {
        RaycastHit hit;
        if (Physics.SphereCast(raycastOrigin, collider.radius, raycastDirection, out hit, maxRaycastDistance))
        {
            surfaceNormal = hit.normal;
            surfacePoint = hit.point;
            return true;
        }

        surfacePoint = Vector3.zero;
        surfaceNormal = Vector3.zero;
        return false;
    }

    #endregion
}

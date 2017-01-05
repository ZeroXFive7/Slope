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

    [SerializeField]
    private float maxTurnRadius = 30.0f;
    [SerializeField]
    private float minTurnRadius = 3.0f;

    #endregion

    #region Fields

    // Component references.
    private new CharacterController collider = null;

    // Local state.
    private Vector3 previousPosition = Vector3.zero;

    [System.NonSerialized]
    public float TurnNormalized = 0.0f;

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

        // Project velocity into surface plane to avoid popping.
        Vector3 surfaceNormal = GetSurfaceNormal();
        bool isGrounded = collider.isGrounded && CollidedWith(CollisionFlags.Below);
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
        collider.Move(velocity * Time.fixedDeltaTime);

        // Rotate to face velocity.
        Vector3 velocityForward = Vector3.ProjectOnPlane(velocity, Vector3.up);
        if (collider.isGrounded && velocityForward.sqrMagnitude >= Mathf.Epsilon)
        {
            collider.transform.rotation = Quaternion.LookRotation(velocityForward.normalized, Vector3.up);
        }
    }

    #region Dynamics

    private Vector3 GetForces(Vector3 velocity, bool isGrounded, Vector3 surfaceNormal)
    {
        Vector3 forceGravity = mass * Physics.gravity;
        Vector3 forceFriction = Vector3.zero;

        if (isGrounded)
        {
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

    private float GetAngularSpeed(Vector3 linearVelocity, bool isGrounded)
    {
        float speed = linearVelocity.magnitude;
        float absTurnNormalized = Mathf.Abs(TurnNormalized);
        if (speed <= Mathf.Epsilon || absTurnNormalized <= Mathf.Epsilon)
        {
            return 0.0f;
        }

        float turnRadius = Mathf.Lerp(maxTurnRadius, minTurnRadius, absTurnNormalized);
        return Mathf.Sign(TurnNormalized) * speed / turnRadius * Mathf.Rad2Deg;
    }

    #endregion

    #region Helpers

    private bool CollidedWith(CollisionFlags flag)
    {
        return (collider.collisionFlags & flag) != 0;
    }

    private Vector3 GetSurfaceNormal()
    {
        Vector3 normal = Vector3.zero;

        Vector3 raycastOrigin = transform.position + transform.up * collider.height / 2.0f;
        Vector3 raycastDirection = -transform.up;
        float maxRaycastDistance = collider.height;

        Vector3 normalScratch;
        for (int i = 0; i < SURFACE_RAYCAST_OFFSET_LOCAL_DIRECTIONS.Length; ++i)
        {
            if (TryGetSurfaceNormal(
                raycastOrigin + transform.TransformVector(SURFACE_RAYCAST_OFFSET_LOCAL_DIRECTIONS[i] * SURFACE_RAYCAST_OFFSET_DISTANCE), 
                raycastDirection, 
                maxRaycastDistance, 
                out normalScratch))
            {
                normal += normalScratch;
            }
        }

        return normal.normalized;
    }

    private bool TryGetSurfaceNormal(Vector3 raycastOrigin, Vector3 raycastDirection, float maxRaycastDistance, out Vector3 surfaceNormal)
    {
        RaycastHit hit;
        if (Physics.Raycast(raycastOrigin, raycastDirection, out hit, maxRaycastDistance))
        {
            surfaceNormal = hit.normal;
            return true;
        }
        surfaceNormal = Vector3.zero;
        return false;
    }

    #endregion
}

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

    [Header("Steering")]
    [SerializeField]
    private float maxSpeed = 20.0f;
    [SerializeField]
    private float minSpeedTurnRadius = 3.0f;
    [SerializeField]
    private float maxSpeedTurnRadius = 15.0f;
    //[SerializeField]
    //private float maxTurnSpeed = 90.0f;
    [SerializeField]
    private float maxLookSpeed = 360.0f;

    #endregion

    #region Fields

    // Component references.
    private new Rigidbody rigidbody = null;
    private CapsuleCollider collider = null;

    // Local state.
    private Vector3 velocity = Vector3.zero;
    private Vector3 previousPosition = Vector3.zero;
    private SurfaceData surface;

    #endregion

    #region Properties

    public float Speed
    {
        get { return velocity.magnitude; }
    }

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
            rigidbody.position = snapToTransform.position;
            rigidbody.rotation = snapToTransform.rotation;
        }

        previousPosition = rigidbody.position;
        velocity = Vector3.zero;
        surface = SurfaceData.Default;
    }

    private void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
        collider = GetComponent<CapsuleCollider>();

        previousPosition = rigidbody.position;
        velocity = Vector3.zero;
        surface = SurfaceData.Default;
    }

    private void FixedUpdate()
    {
        // Query world state.
        velocity = UpdateVelocity(Time.fixedDeltaTime, ref previousPosition);
        surface = GetSurface(surface);

        // Integrate velocity.
        Vector3 acceleration = GetForces(velocity, surface) / mass;
        velocity += acceleration * Time.fixedDeltaTime;

        if (surface.IsOnSurface)
        {
            // Clamp to surface.
            velocity += ClampToSurface(surface);

            // Rotate velocity.
            float speed = velocity.magnitude;
            float turnRadius = Mathf.Lerp(minSpeedTurnRadius, maxSpeedTurnRadius, Mathf.InverseLerp(0.0f, maxSpeed, speed));
            float maxTurnSpeed = speed / turnRadius * Mathf.Rad2Deg;
            Quaternion velocityRotation = FaceTarget(velocity, DesiredForward, surface.Normal, maxTurnSpeed);
            velocity = velocityRotation * velocity;

            // Rotate body.
            Vector3 currentForward = Vector3.ProjectOnPlane(rigidbody.transform.forward, surface.Normal);
            Quaternion bodyRotation = FaceTarget(currentForward, DesiredForward, surface.Normal, maxLookSpeed);
            currentForward = bodyRotation * currentForward;
            rigidbody.rotation = Quaternion.LookRotation(currentForward, surface.Normal);
        }

        rigidbody.velocity = velocity;
    }

    #region Dynamics

    private Vector3 UpdateVelocity(float deltaTime, ref Vector3 previousPosition)
    {
        Vector3 velocity = (rigidbody.position - previousPosition) / deltaTime;
        previousPosition = rigidbody.position;
        return velocity;
    }

    private Vector3 ClampToSurface(SurfaceData surface)
    {
        return Vector3.Project(surface.ClosestPoint - rigidbody.position, Vector3.up);
    }

    private SurfaceData GetSurface(SurfaceData previousSurfaceData)
    {
        SurfaceData surface = SurfaceData.Default;

        // If a raycast starts inside a collider then the resulting RaycastHit will be invalid.
        // To avoid this condition start the raycast inside the collider by translating surface distance along local up.
        RaycastHit hit;
        if (Physics.Raycast(rigidbody.position + transform.up * defaultSurfaceDistance, -transform.up, out hit, 2.0f * defaultSurfaceDistance))
        {
            surface.IsOnSurface = true;
            surface.Normal = hit.normal;
            surface.ClosestPoint = hit.point;
        }

        return surface;
    }

    private Vector3 GetForces(Vector3 velocity, SurfaceData surface)
    {
        Vector3 forceGravity = mass * Physics.gravity;
        Vector3 forceFriction = Vector3.zero;

        if (surface.IsOnSurface)
        {
            Vector3 forwardOnSurface = Vector3.ProjectOnPlane(transform.forward, surface.Normal).normalized;

            // Calculate forces in surface plane.
            Vector3 forceTangent = Vector3.ProjectOnPlane(forceGravity, surface.Normal);
            Vector3 forceParallel = Vector3.Project(forceTangent, forwardOnSurface);
            Vector3 forcePerpendicular = forceTangent - forceParallel;

            // Calculate force normal to surface plane.
            Vector3 forceNormal = Vector3.Project(forceGravity, surface.Normal);

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

    private Quaternion FaceTarget(Vector3 current, Vector3 target, Vector3 normal, float maxSpeed)
    {
        Vector3 currentPlanar = Vector3.ProjectOnPlane(current, normal);
        Vector3 targetPlanar = Vector3.ProjectOnPlane(target, normal);
        float angleToTarget = Vector3.Angle(currentPlanar, targetPlanar);
        float direction = Mathf.Sign(Vector3.Dot(Vector3.Cross(currentPlanar, targetPlanar), normal));

        Quaternion rotation = Quaternion.identity;
        if (angleToTarget < (maxSpeed * Time.fixedDeltaTime))
        {
            rotation = Quaternion.AngleAxis(angleToTarget * direction, normal);
        }
        else
        {
            rotation = Quaternion.AngleAxis(maxSpeed * Time.fixedDeltaTime * direction, normal);
        }
        return rotation;
    }

    #endregion

    #region Structs

    private struct SurfaceData
    {
        public bool IsOnSurface;
        public Vector3 Normal;
        public Vector3 ClosestPoint;

        public static SurfaceData Default = new SurfaceData() { IsOnSurface = false, Normal = Vector3.up, ClosestPoint = Vector3.zero };
    }

    #endregion
}

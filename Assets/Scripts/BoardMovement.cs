using System.Collections;
using UnityEngine;

public class BoardMovement : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField]
    private new Rigidbody rigidbody = null;
    [SerializeField]
    private RigidbodyHovering hovering = null;
    [SerializeField]
    private BoardTrailRenderers trails = null;

    [Header("Debug Acceleration")]
    [SerializeField]
    private float debugMovementSpeed = 12.0f;
    [SerializeField]
    private float timeToDebugMovementSpeed = 1.0f;

    [Header("Steering")]
    [SerializeField]
    private float downhillAcceleration = 20.0f;
    [SerializeField]
    private float brakeAcceleration = -20.0f;
    [SerializeField]
    private float turnAcceleration = 180.0f;
    [SerializeField]
    private float maxSpeed = 10.0f;
    [SerializeField]
    private float maxTurnSpeed = 90.0f;

    [Header("Skidding")]
    [SerializeField]
    private float timeToSkiddedTurn = 1.0f;
    [SerializeField]
    private float skiddedTurnSpeed = 5.0f;

    [Header("Carving")]
    [SerializeField]
    private PropertyCurve timeToCarve;
    [SerializeField]
    private PropertyCurve carveRadius;

    [Header("Jumping")]
    [SerializeField]
    private PropertyCurve jumpHeight;
    [SerializeField]
    private float maxJumpDuration = 3.0f;
    [SerializeField]
    private PropertyCurve jumpForce;
    [SerializeField]
    private float jumpTakeoffDuration = 0.5f;

    [Header("Leaning")]
    [SerializeField]
    private float rollSpeed = -2.0f;
    [SerializeField]
    private float pitchSpeed = 2.0f;
    [SerializeField]
    private float timeToLean = 1.0f;

    [Header("Wipeout")]
    [SerializeField]
    private float wipeoutForce = 10.0f;
    [SerializeField]
    private float wipeoutTorque = 10.0f;
    [SerializeField]
    private float wipeoutDuration = 2.0f;

    [Header("Flip Over")]
    [SerializeField]
    private float flipForce = 3.0f;
    [SerializeField]
    private float flipTorque = 3.0f;

    [Header("Front and Back Control")]
    [SerializeField]
    private Transform front = null;
    [SerializeField]
    private Transform back = null;
    [SerializeField]
    private float frontBackSpeed = 12.0f;
    [SerializeField]
    private float timeToFrontBackSpeed = 1.0f;

    private float forwardSpeed = 0.0f;
    private float rotationSpeed = 0.0f;
    private float turnSpeed = 0.0f;

    private Vector3 previousPosition = Vector3.zero;
    private Quaternion previousRotation = Quaternion.identity;
    private float jumpTimer = 0.0f;

    private Vector3 previousFrontPosition = Vector3.zero;
    private Vector3 previousBackPosition = Vector3.zero;

    public bool IsJumping { get; private set; }

    public bool IsWipingOut { get; private set; }

    public Vector3 Velocity { get; private set; }
    public Vector3 AngularVelocity { get; private set; }

    public Transform Front { get { return front; } }

    public Transform Back { get { return back; } }

    public Vector3 BoardForward
    {
        get
        {
            return Mathf.Sign(Vector3.Dot(transform.forward, Velocity.normalized)) * transform.forward;
        }
    }

    public Vector3 BoardRight
    {
        get
        {
            return Mathf.Sign(Vector3.Dot(transform.forward, Velocity.normalized)) * transform.right;
        }
    }

    private void Awake()
    {
        rigidbody.isKinematic = true;
    }

    private void OnEnable()
    {

    }

    private float linearAcceleration;
    private float maxAcceleration = 100.0f;

    private void FixedUpdate()
    {
        Velocity = (transform.position - previousPosition) / Time.fixedDeltaTime;
        previousPosition = transform.position;

        AngularVelocity = AngularVelocityFromTo(previousRotation, transform.rotation) / Time.fixedDeltaTime;
        AngularVelocity = Vector3.Project(AngularVelocity, rigidbody.transform.up);
        previousRotation = transform.rotation;

        // Update angular velocity.
        float absSteering = Mathf.Abs(Steering);
        if (absSteering > 0.25f)
        {
            rotationSpeed += turnAcceleration * Mathf.Sign(Steering) * Time.fixedDeltaTime;
        }
        rotationSpeed = Mathf.Clamp(rotationSpeed, -maxTurnSpeed, maxTurnSpeed);

        // Update linear velocity.
        if (Throttle < 0.25f)
        {
            if (forwardSpeed <= brakeAcceleration * Time.fixedDeltaTime)
            {
                forwardSpeed = 0.0f;
            }
            else
            {
                forwardSpeed += brakeAcceleration * Time.fixedDeltaTime;
            }

            rotationSpeed = 0.0f;
        }
        else
        {
            forwardSpeed += downhillAcceleration * Time.fixedDeltaTime;
        }
        forwardSpeed = Mathf.Clamp(forwardSpeed, 0.0f, maxSpeed);

        transform.rotation *= Quaternion.AngleAxis(rotationSpeed * Time.fixedDeltaTime, transform.up);
        transform.position += forwardSpeed * transform.forward * Time.fixedDeltaTime;
    }

    public void Reset(Transform snapToTransform = null)
    {
        IsJumping = false;
        IsWipingOut = false;

        jumpTimer = 0.0f;

        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;

        if (snapToTransform)
        {
            transform.position = snapToTransform.position;
            transform.rotation = snapToTransform.rotation;
        }

        previousPosition = transform.position;
        previousFrontPosition = front.position;
        previousBackPosition = back.position;
        Velocity = Vector3.zero;

        trails.Reset();
    }

    public void JumpHold()
    {
        jumpTimer += Time.deltaTime;
        float t = Mathf.Clamp01(jumpTimer / maxJumpDuration);
        hovering.HoverHeightScalar = jumpHeight.Evaluate(t);
    }

    public void TryJumpRelease()
    {
        if (jumpTimer > 0.0f && !IsJumping)
        {
            StartCoroutine(JumpReleaseCoroutine());
        }
    }

    public void DebugMove()
    {
        if (hovering.IsGrounded)
        {
            SetDesiredLinearVelocity(BoardForward * debugMovementSpeed, timeToDebugMovementSpeed);
        }
    }

    public void Lean(float roll, float pitch)
    {
        if (Mathf.Abs(roll) > Mathf.Epsilon)
        {
            SetDesiredAngularVelocity(BoardForward, roll * rollSpeed, timeToLean);
        }

        if (Mathf.Abs(pitch) > Mathf.Epsilon)
        {
            SetDesiredAngularVelocity(BoardRight, pitch * pitchSpeed, timeToLean);
        }
    }

    public void SkiddedTurn(float turnAmount)
    {
        SetDesiredAngularVelocity(rigidbody.transform.up, turnAmount * skiddedTurnSpeed * Mathf.Deg2Rad, timeToSkiddedTurn);

        if (hovering.IsGrounded)
        {
            SetDesiredLinearVelocity(BoardForward * Velocity.magnitude, timeToSkiddedTurn);
        }
    }

    public void CarvedTurn(float turnAmount)
    {
        float turnMagnitude = Mathf.Abs(turnAmount);
        if (turnMagnitude > 0.0f && hovering.IsGrounded)
        {
            carveRadius.Evaluate(turnMagnitude);
            timeToCarve.Evaluate(turnMagnitude);

            float turnDirection = Mathf.Sign(turnAmount);
            float angularSpeed = (Velocity.magnitude * turnDirection) / carveRadius;
            SetDesiredAngularVelocity(rigidbody.transform.up, angularSpeed, timeToCarve);
            SetDesiredLinearVelocity(BoardForward * Velocity.magnitude, timeToCarve);
        }
    }

    [System.NonSerialized]
    public float Throttle = 0.0f;

    [System.NonSerialized]
    public float Steering = 0.0f;

    public void FlipOver()
    {
        if (!hovering.IsGrounded && !IsJumping && Vector3.Dot(Vector3.up, rigidbody.transform.up) < 0.0f)
        {
            rigidbody.AddForce(Vector3.up * flipForce, ForceMode.Impulse);
            rigidbody.AddRelativeTorque(Vector3.forward * flipTorque, ForceMode.Impulse);
        }
    }

    public void Wipeout()
    {
        StartCoroutine(WipeoutCoroutine());
    }

    public void SetDesiredFrontLinearVelocityNormalized(Vector3 desiredVelocityNormalized)
    {
        if (hovering.IsGrounded && !IsJumping)
        {
            Vector3 frontVelocity = (front.position - previousFrontPosition) / Time.unscaledDeltaTime;
            frontVelocity = Vector3.Project(frontVelocity, desiredVelocityNormalized.normalized);
            previousFrontPosition = front.position;

            Vector3 desiredVelocity = desiredVelocityNormalized * frontBackSpeed;
            Vector3 acceleration = (desiredVelocity - frontVelocity) / timeToFrontBackSpeed;

            rigidbody.AddForceAtPosition(acceleration, front.position);
        }
    }

    public void SetDesiredBackLinearVelocityNormalized(Vector3 desiredVelocityNormalized)
    {
        if (hovering.IsGrounded && !IsJumping)
        {
            Vector3 backVelocity = (back.position - previousBackPosition) / Time.unscaledDeltaTime;
            backVelocity = Vector3.Project(backVelocity, desiredVelocityNormalized.normalized);
            previousBackPosition = back.position;

            Vector3 desiredVelocity = desiredVelocityNormalized * frontBackSpeed;
            Vector3 acceleration = (desiredVelocity - backVelocity) / timeToFrontBackSpeed;

            rigidbody.AddForceAtPosition(acceleration, back.position);
        }
    }

    private void SetDesiredLinearVelocity(Vector3 desiredLinearVelocity, float timeToAccelerate)
    {
        Vector3 acceleration = (desiredLinearVelocity - Velocity) / timeToAccelerate;
        rigidbody.AddForce(acceleration);
    }

    private void SetDesiredAngularVelocity(Vector3 rotationAxis, float speed, float timeToAccelerate)
    {
        Vector3 currentAngularVelocity = Vector3.Project(rigidbody.angularVelocity, rotationAxis);
        Vector3 acceleration = (rotationAxis * speed - currentAngularVelocity) / timeToAccelerate;
        rigidbody.AddTorque(acceleration);
    }

    private void UpdateAngularVelocity(Vector3 rotationAxis, float acceleration, float maxSpeed, float timeToAccelerate = 1.0f)
    {
        Vector3 currentAngularVelocity = Vector3.Project(rigidbody.angularVelocity, rotationAxis);
        float currentSpeed = Mathf.Sign(Vector3.Dot(currentAngularVelocity, rotationAxis)) * currentAngularVelocity.magnitude;
        float desiredSpeed = Mathf.Clamp(currentSpeed + acceleration * Time.deltaTime / timeToAccelerate, -maxSpeed, maxSpeed);
        SetDesiredAngularVelocity(rotationAxis, desiredSpeed, Time.fixedDeltaTime);
    }

    private IEnumerator WipeoutCoroutine()
    {
        hovering.enabled = false;
        Vector3 torque = Vector3.Cross(BoardForward, rigidbody.transform.up) * wipeoutTorque;
        rigidbody.AddTorque(torque, ForceMode.Impulse);
        rigidbody.AddForce(rigidbody.transform.up * wipeoutForce, ForceMode.Impulse);
        IsWipingOut = true;

        yield return new WaitForSeconds(wipeoutDuration);
        hovering.enabled = true;
    }

    private IEnumerator JumpReleaseCoroutine()
    {
        IsJumping = true;
        hovering.HoverHeightScalar = 1.0f;
        hovering.enabled = false;

        float t = Mathf.Clamp01(jumpTimer / maxJumpDuration);
        jumpTimer = 0.0f;

        rigidbody.AddForce(hovering.SurfaceNormal * jumpForce.Evaluate(t), ForceMode.Impulse);

        yield return new WaitForSeconds(jumpTakeoffDuration);

        hovering.enabled = true;
        IsJumping = false;
    }

    private Quaternion QuaternionFromAngularVelocity(Vector3 angularVelocity)
    {
        return Quaternion.AngleAxis(angularVelocity.magnitude * Mathf.Rad2Deg, angularVelocity.normalized);
    }

    private Vector3 AngularVelocityFromTo(Quaternion from, Quaternion to)
    {
        float angle;
        Vector3 axis;
        FromToRotation(from, to).ToAngleAxis(out angle, out axis);

        if (angle > 180.0f)
        {
            angle -= 360.0f;
        }

        return angle * axis * Mathf.Deg2Rad;
    }

    private Quaternion FromToRotation(Quaternion from, Quaternion to)
    {
        return to * Quaternion.Inverse(from);
    }
}
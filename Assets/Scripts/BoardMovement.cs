using System.Collections;
using UnityEngine;

public class BoardMovement : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField]
    private new Rigidbody rigidbody = null;
    [SerializeField]
    private RigidbodyHovering hovering = null;

    [Header("Debug Acceleration")]
    [SerializeField]
    private float debugMovementSpeed = 12.0f;
    [SerializeField]
    private float timeToDebugMovementSpeed = 1.0f;

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

    private Vector3 previousPosition = Vector3.zero;
    private bool jumpAttemptedThisFrame = false;

    public bool IsJumping { get; private set; }

    public bool IsWipingOut { get; private set; }

    public Vector3 Velocity { get; private set; }

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

    private void OnEnable()
    {

    }

    private void FixedUpdate()
    {
        Velocity = (transform.position - previousPosition) / Time.fixedDeltaTime;
        previousPosition = transform.position;
    }

    public void Reset(Transform snapToTransform = null)
    {
        IsJumping = false;
        IsWipingOut = false;

        jumpAttemptedThisFrame = false;

        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;

        if (snapToTransform)
        {
            transform.position = snapToTransform.position;
            transform.rotation = snapToTransform.rotation;
        }

        previousPosition = transform.position;
        Velocity = Vector3.zero;
    }

    public void Jump()
    {
        jumpAttemptedThisFrame = true;

        if (!IsJumping)
        {
            StartCoroutine(JumpCoroutine());
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
        Vector3 leanAngularVelocity = roll * BoardForward * rollSpeed + pitch * BoardRight * pitchSpeed;

        float speed = leanAngularVelocity.magnitude;
        if (Mathf.Abs(speed) > float.Epsilon)
        {
            leanAngularVelocity /= speed;
        }
        SetDesiredAngularVelocity(leanAngularVelocity, speed, timeToLean);
    }

    public void SkiddedTurn(float turnAmount)
    {
        SetDesiredAngularVelocity(rigidbody.transform.up, turnAmount * skiddedTurnSpeed * Mathf.Deg2Rad, timeToSkiddedTurn);

        if (hovering.IsGrounded)
        {
            SetDesiredLinearVelocity(BoardForward * Velocity.magnitude, timeToSkiddedTurn);
        }
    }

    public void CarvedTurn(float lean)
    {
        float leanMagnitude = Mathf.Abs(lean);
        if (leanMagnitude > 0.0f && hovering.IsGrounded)
        {
            carveRadius.Evaluate(leanMagnitude);
            timeToCarve.Evaluate(leanMagnitude);

            float turnDirection = lean == 0.0f ? 0.0f : Mathf.Sign(lean);
            float angularSpeed = Velocity.magnitude / carveRadius;
            angularSpeed = angularSpeed * turnDirection;

            SetDesiredAngularVelocity(rigidbody.transform.up, angularSpeed, timeToCarve);
            SetDesiredLinearVelocity(BoardForward * Velocity.magnitude, timeToCarve);
        }
    }

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

    private IEnumerator JumpCoroutine()
    {
        IsJumping = true;

        float jumpTimer = 0.0f;
        float t = 0.0f;
        while (jumpAttemptedThisFrame)
        {
            jumpAttemptedThisFrame = false;

            yield return new WaitForSeconds(Time.deltaTime);
            jumpTimer += Time.deltaTime;
            t = Mathf.Clamp01(jumpTimer / maxJumpDuration);
            hovering.HoverHeightScalar = jumpHeight.Evaluate(t);
        }

        hovering.HoverHeightScalar = 1.0f;
        hovering.enabled = false;

        rigidbody.AddForce(hovering.SurfaceNormal * jumpForce.Evaluate(t), ForceMode.Impulse);

        yield return new WaitForSeconds(jumpTakeoffDuration);

        hovering.enabled = true;

        IsJumping = false;
    }
}
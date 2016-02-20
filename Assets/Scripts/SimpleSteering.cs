using UnityEngine;
using System.Collections;

public class SimpleSteering : MonoBehaviour
{
    public enum InputAxis
    {
        HorizontalLeft,
        HorizontalRight,
        VerticalLeft,
        VerticalRight,
        LeftTrigger,
        RightTrigger
    }

    [SerializeField]
    private new Rigidbody rigidbody = null;
    [SerializeField]
    private new Camera camera = null;
    [SerializeField]
    private RigidbodyHovering hovering = null;
    [SerializeField]
    private Transform body = null;

    [SerializeField]
    private PropertyCurve turnRadius;
    [SerializeField]
    private PropertyCurve leanAngle;

    [SerializeField]
    private float speed = 10.0f;
    [SerializeField]
    private float turnSpeed = 5.0f;
    [SerializeField]
    private float centerOfMassHeight = -0.1f;
    [SerializeField]
    private InputAxis horizontalTranslationAxis = InputAxis.HorizontalLeft;
    [SerializeField]
    private InputAxis verticalTranslationAxis = InputAxis.VerticalLeft;
    [SerializeField]
    private InputAxis turnAxis = InputAxis.HorizontalRight;

    [SerializeField]
    private InputAxis gasAxis = InputAxis.RightTrigger;

    [SerializeField]
    private float wipeoutAngle = 45.0f;
    [SerializeField]
    private float wipeoutForce = 10.0f;
    [SerializeField]
    private float wipeoutTorque = 10.0f;
    [SerializeField]
    private float wipeoutDuration = 2.0f;

    [SerializeField]
    private float timeToTurn = 1.0f;
    [SerializeField]
    private float timeToCarve = 0.1f;

    [Header("Jumping")]
    [SerializeField]
    private InputAxis jumpAxis = InputAxis.LeftTrigger;
    [SerializeField]
    private PropertyCurve jumpHeight;
    [SerializeField]
    private float maxJumpDuration = 3.0f;
    [SerializeField]
    private PropertyCurve jumpForce;
    [SerializeField]
    private float jumpTakeoffDuration = 0.5f;

    [Header("Spinning")]
    [SerializeField]
    private float rollSpeed = -2.0f;
    [SerializeField]
    private float pitchSpeed = 2.0f;
    [SerializeField]
    private float timeToSpin = 1.0f;

    private Vector3 previousPosition;

    private bool isJumping = false;

    public bool IsWipingOut { get; private set; }

    public Vector3 Velocity { get; private set; }

    private Vector3 BoardForward
    {
        get
        {
            return Mathf.Sign(Vector3.Dot(transform.forward, Velocity.normalized)) * transform.forward;
        }
    }

    private Vector3 BoardRight
    {
        get
        {
            return Mathf.Sign(Vector3.Dot(transform.forward, Velocity.normalized)) * transform.right;
        }
    }

    private void OnEnable()
    {
        isJumping = false;
        IsWipingOut = false;
        previousPosition = transform.position;
        Velocity = Vector3.zero;
    }

    private void FixedUpdate()
    {
        if (IsWipingOut)
        {
            if (hovering.IsGrounded)
            {
                IsWipingOut = false;
            }
            else
            {
                return;
            }
        }

        Vector3 inputGamepadSpace = Vector3.right * ReadInputAxis(horizontalTranslationAxis) + Vector3.forward * ReadInputAxis(verticalTranslationAxis);
        Vector3 leanForward = Vector3.ProjectOnPlane(camera.transform.forward, hovering.SurfaceNormal).normalized;
        Vector3 leanRight = Vector3.ProjectOnPlane(camera.transform.right, hovering.SurfaceNormal).normalized;
        Vector3 inputWorldSpace = leanRight * inputGamepadSpace.x + leanForward * inputGamepadSpace.z;
        Vector3 inputBoardSpace = transform.InverseTransformVector(inputWorldSpace);

        Vector3 spinAngularVelocity = inputGamepadSpace.x * BoardForward * rollSpeed + inputGamepadSpace.z * BoardRight * pitchSpeed;
        SetDesiredAngularVelocity(spinAngularVelocity, timeToSpin);

        Vector3 bodyRotationAxis = Vector3.Cross(rigidbody.transform.up, inputWorldSpace);
        float bodyRotationAngle = leanAngle.Evaluate(inputGamepadSpace.magnitude);
        //body.forward = Quaternion.AngleAxis(bodyRotationAngle, bodyRotationAxis) * rigidbody.transform.up;

        if (Vector3.Angle(Vector3.up, body.forward) > wipeoutAngle)
        {
            //Wipeout();
        }

        //Vector3 com = rigidbody.transform.InverseTransformVector(inputWorldSpace);
        //com.y = centerOfMassHeight;
        //rigidbody.centerOfMass = com;
        //Debug.DrawLine(transform.position, transform.TransformPoint(com), Color.white);

        Velocity = (transform.position - previousPosition) / Time.fixedDeltaTime;
        Debug.Log(Velocity.magnitude);
        previousPosition = transform.position;

        float turnInput = ReadInputAxis(turnAxis);
        Vector3 desiredAngularVelocity = rigidbody.transform.up * turnInput * turnSpeed;
        Vector3 currentAngularVelocity = Vector3.Project(rigidbody.angularVelocity, rigidbody.transform.up);
        rigidbody.AddTorque((desiredAngularVelocity - currentAngularVelocity));

        float gas = ReadInputAxis(gasAxis);
        if (gas > 0.0f && hovering.IsGrounded)
        {
            SetDesiredLinearVelocity(BoardForward * gas * speed, timeToTurn);
        }
        else
        {
            SetDesiredLinearVelocity(BoardForward * Velocity.magnitude, timeToTurn);
        }

        //Vector3 desiredVelocity = inputWorldSpace * speed;
        //Vector3 acceleration = desiredVelocity - velocity;
        //rigidbody.AddForce(acceleration);

        Debug.DrawLine(transform.position, transform.position + Velocity);

        if (!isJumping && ReadInputAxis(jumpAxis) > 0.0f)
        {
            StartCoroutine(JumpCoroutine());
        }
    }

    private void CarvedTurn(float lean)
    {
        float leanMagnitude = Mathf.Abs(lean);
        if (leanMagnitude > 0.0f)
        {
            turnRadius.Evaluate(leanMagnitude);

            float turnDirection = lean == 0.0f ? 0.0f : Mathf.Sign(lean);
            Vector3 turnCenter = transform.position + turnDirection * BoardRight * turnRadius;

            float angularSpeed = (180.0f * Velocity.magnitude) / (Mathf.PI * turnRadius) * turnDirection;

            //SetDesiredAngularVelocity(rigidbody.transform.up * angularSpeed, timeToCarve);
            SetDesiredLinearVelocity(Quaternion.AngleAxis(angularSpeed * Time.deltaTime, rigidbody.transform.up) * Velocity, timeToCarve);
        }
    }

    private float ReadInputAxis(InputAxis axis)
    {
        float value = 0.0f;
        switch (axis)
        {
            case InputAxis.HorizontalLeft:
                value = Input.GetAxis("HorizontalLeft");
                break;
            case InputAxis.HorizontalRight:
                value = Input.GetAxis("HorizontalRight");
                break;
            case InputAxis.VerticalLeft:
                value = Input.GetAxis("VerticalLeft");
                break;
            case InputAxis.VerticalRight:
                value = Input.GetAxis("VerticalRight");
                break;
            case InputAxis.LeftTrigger:
                value = Input.GetAxis("LeftTrigger");
                break;
            case InputAxis.RightTrigger:
                value = Input.GetAxis("RightTrigger");
                break;
        }
        return value;
    }

    private void SetDesiredLinearVelocity(Vector3 desiredLinearVelocity, float timeToAccelerate)
    {
        Vector3 acceleration = (desiredLinearVelocity - Velocity) / timeToAccelerate;
        rigidbody.AddForce(acceleration);
    }
    private void SetDesiredAngularVelocity(Vector3 desiredAngularVelocity, float timeToAccelerate)
    {
        Vector3 rotationAxis = desiredAngularVelocity.normalized;
        Vector3 currentAngularVelocity = Vector3.Project(rigidbody.angularVelocity, rotationAxis);
        Vector3 acceleration = (desiredAngularVelocity - currentAngularVelocity) / timeToAccelerate;
        rigidbody.AddTorque(acceleration);
    }

    private void Wipeout()
    {
        StartCoroutine(WipeoutCoroutine());
    }

    private IEnumerator WipeoutCoroutine()
    {
        hovering.enabled = false;
        Vector3 torque = Vector3.Cross(BoardForward, body.forward) * wipeoutTorque;
        rigidbody.AddTorque(torque, ForceMode.Impulse);
        rigidbody.AddForce(body.forward * wipeoutForce, ForceMode.Impulse);
        IsWipingOut = true;

        yield return new WaitForSeconds(wipeoutDuration);
        hovering.enabled = true;
    }

    private IEnumerator JumpCoroutine()
    {
        isJumping = true;

        float jumpTimer = 0.0f;
        float t = 0.0f;
        while (ReadInputAxis(jumpAxis) > 0.0f)
        {
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

        isJumping = false;
    }
}

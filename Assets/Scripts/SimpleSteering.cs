using UnityEngine;
using System.Collections;

public class SimpleSteering : MonoBehaviour
{
    [System.Serializable]
    public struct BoardTrailColors
    {
        public string Name;
        public Color FrontTrailColor;
        public Color BackTrailColor;
    };

    public int PlayerInputId
    {
        get
        {
            if (playerInput == null)
            {
                return -1;
            }
            return playerInput.id;
        }
        set
        {
            playerInput = Rewired.ReInput.players.GetPlayer(value);
        }
    }

    private Rewired.Player playerInput = null;

    [SerializeField]
    private new Rigidbody rigidbody = null;
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
    private PropertyCurve timeToCarve;

    [Header("Jumping")]
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

    [Header("Flip Over")]
    [SerializeField]
    private float flipForce = 3.0f;
    [SerializeField]
    private float flipTorque = 3.0f;

    [Header("Trails")]
    [SerializeField]
    private Renderer frontTrailRenderer = null;
    [SerializeField]
    private Renderer backTrailRenderer = null;

    private Vector3 previousPosition;

    private bool isJumping = false;

    [HideInInspector]
    public ChaseCamera Camera = null;

    public BoardTrailColors TrailColors
    {
        set
        {
            frontTrailRenderer.material.color = value.FrontTrailColor;
            frontTrailRenderer.material.SetColor("_EmissionColor", value.FrontTrailColor);

            backTrailRenderer.material.color = value.BackTrailColor;
            backTrailRenderer.material.SetColor("_EmissionColor", value.BackTrailColor);
        }
    }

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

    public void Reset(Transform snapToTransform = null)
    {
        isJumping = false;
        IsWipingOut = false;

        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;

        if (snapToTransform)
        {
            transform.position = snapToTransform.position;
            transform.rotation = snapToTransform.rotation;
        }

        previousPosition = transform.position;
        Velocity = Vector3.zero;

        if (Camera != null)
        {
            Camera.transform.position = transform.position;
        }
    }

    private void OnEnable()
    {
        Reset();
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

        if (playerInput.GetButton("Flip Over") && !hovering.IsGrounded && !isJumping && Vector3.Dot(Vector3.up, rigidbody.transform.up) < 0.0f)
        {
            FlipOver();
        }

        Vector3 inputGamepadSpace = Vector3.right * playerInput.GetAxis("Lean Horizontal") + Vector3.forward * playerInput.GetAxis("Lean Vertical");
        Vector3 leanForward = Vector3.ProjectOnPlane(Camera.transform.forward, hovering.SurfaceNormal).normalized;
        Vector3 leanRight = Vector3.ProjectOnPlane(Camera.transform.right, hovering.SurfaceNormal).normalized;
        Vector3 inputWorldSpace = leanRight * inputGamepadSpace.x + leanForward * inputGamepadSpace.z;

        Vector3 inputBoardSpace = new Vector3(Vector3.Dot(inputWorldSpace, BoardRight), 0.0f, Vector3.Dot(inputWorldSpace, BoardForward));
        Debug.DrawLine(transform.position, transform.position + transform.TransformVector(inputBoardSpace), Color.green);

        Vector3 spinAngularVelocity = inputBoardSpace.x * BoardForward * rollSpeed + inputBoardSpace.z * BoardRight * pitchSpeed;
        SetDesiredAngularVelocity(spinAngularVelocity.normalized, spinAngularVelocity.magnitude, timeToSpin);
        if (hovering.IsGrounded)
        {
            CarvedTurn(Vector3.Dot(inputWorldSpace, BoardRight));
        }

        // Input world space is vector screen relative.  Just find appropriate axes of rotation and set target to ws input value
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

        float turnInput = playerInput.GetAxis("Turn");
        //Vector3 desiredAngularVelocity = rigidbody.transform.up * turnInput * turnSpeed * Mathf.Deg2Rad;
        SetDesiredAngularVelocity(rigidbody.transform.up, turnInput * turnSpeed * Mathf.Deg2Rad, timeToTurn);
        //Vector3 currentAngularVelocity = Vector3.Project(rigidbody.angularVelocity, rigidbody.transform.up);
        //rigidbody.AddTorque((desiredAngularVelocity - currentAngularVelocity));

        if (playerInput.GetButton("Accelerate") && hovering.IsGrounded)
        {
            SetDesiredLinearVelocity(BoardForward * speed, timeToTurn);
        }
        else
        {
            SetDesiredLinearVelocity(BoardForward * Velocity.magnitude, timeToTurn);
        }

        //Vector3 desiredVelocity = inputWorldSpace * speed;
        //Vector3 acceleration = desiredVelocity - velocity;
        //rigidbody.AddForce(acceleration);

        Debug.DrawLine(transform.position, transform.position + Velocity);

        if (!isJumping && playerInput.GetButton("Jump"))
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
            timeToCarve.Evaluate(leanMagnitude);

            float turnDirection = lean == 0.0f ? 0.0f : Mathf.Sign(lean);
            float angularSpeed = Velocity.magnitude / turnRadius;
            angularSpeed = angularSpeed * turnDirection;

            SetDesiredAngularVelocity(rigidbody.transform.up, angularSpeed, timeToCarve);
            SetDesiredLinearVelocity(BoardForward * Velocity.magnitude, timeToCarve);
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

    private void FlipOver()
    {
        rigidbody.AddForce(Vector3.up * flipForce, ForceMode.Impulse);
        rigidbody.AddRelativeTorque(Vector3.forward * flipTorque, ForceMode.Impulse);
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
        while (playerInput.GetButton("Jump"))
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

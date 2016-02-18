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

    private Vector3 previousPosition;

    public Vector3 Velocity { get; private set; }

    private Vector3 BoardForward
    {
        get
        {
            return Mathf.Sign(Vector3.Dot(transform.forward, Velocity.normalized)) * transform.forward;
        }
    }

    private void OnEnable()
    {
        previousPosition = transform.position;
        Velocity = Vector3.zero;
    }

    private void FixedUpdate()
    {
        Vector3 inputGamepadSpace = Vector3.right * ReadInputAxis(horizontalTranslationAxis) + Vector3.forward * ReadInputAxis(verticalTranslationAxis);
        Vector3 leanForward = Vector3.ProjectOnPlane(camera.transform.forward, hovering.SurfaceNormal).normalized;
        Vector3 leanRight = Vector3.ProjectOnPlane(camera.transform.right, hovering.SurfaceNormal).normalized;
        Vector3 inputWorldSpace = leanRight * inputGamepadSpace.x + leanForward * inputGamepadSpace.z;

        Vector3 com = rigidbody.transform.InverseTransformVector(inputWorldSpace);
        com.y = centerOfMassHeight;
        rigidbody.centerOfMass = com;
        Debug.DrawLine(transform.position, transform.TransformPoint(com), Color.white);

        Velocity = (transform.position - previousPosition) / Time.fixedDeltaTime;
        Debug.Log(Velocity.magnitude);
        previousPosition = transform.position;

        float gas = ReadInputAxis(gasAxis);
        if (gas > 0.0f)
        {
            Vector3 desiredVelocty = BoardForward * gas * speed;
            rigidbody.AddForce((desiredVelocty - Velocity));
        }
        else
        {
            Vector3 desiredVelocity = BoardForward * Velocity.magnitude;
            rigidbody.AddForce((desiredVelocity - Velocity));
        }

        //Vector3 desiredVelocity = inputWorldSpace * speed;
        //Vector3 acceleration = desiredVelocity - velocity;
        //rigidbody.AddForce(acceleration);

        float turnInput = ReadInputAxis(turnAxis);
        Vector3 desiredAngularVelocity = rigidbody.transform.up * turnInput * turnSpeed;
        Vector3 currentAngularVelocity = Vector3.Project(rigidbody.angularVelocity, rigidbody.transform.up);
        Vector3 angularAcceleration = desiredAngularVelocity - currentAngularVelocity;
        rigidbody.AddTorque(angularAcceleration);
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
}

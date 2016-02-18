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
    private InputAxis horizontalTranslationAxis = InputAxis.HorizontalLeft;
    [SerializeField]
    private InputAxis verticalTranslationAxis = InputAxis.VerticalLeft;
    [SerializeField]
    private InputAxis turnAxis = InputAxis.HorizontalRight;

    private Vector3 previousPosition;

    private void OnEnable()
    {
        previousPosition = transform.position;
    }

    private void FixedUpdate()
    {
        Vector3 inputGamepadSpace = Vector3.right * ReadInputAxis(horizontalTranslationAxis) + Vector3.forward * ReadInputAxis(verticalTranslationAxis);
        Vector3 leanForward = Vector3.ProjectOnPlane(camera.transform.forward, hovering.SurfaceNormal).normalized;
        Vector3 leanRight = Vector3.ProjectOnPlane(camera.transform.right, hovering.SurfaceNormal).normalized;
        Vector3 inputWorldSpace = leanRight * inputGamepadSpace.x + leanForward * inputGamepadSpace.z;

        Debug.DrawLine(transform.position, transform.position + inputWorldSpace, Color.white);

        Vector3 velocity = (transform.position - previousPosition) / Time.fixedDeltaTime;
        Debug.Log(velocity.magnitude);
        previousPosition = transform.position;

        Vector3 desiredVelocity = inputWorldSpace * speed;
        Vector3 acceleration = desiredVelocity - velocity;
        rigidbody.AddForce(acceleration);

        float turnInput = ReadInputAxis(turnAxis);
        Vector3 desiredAngularVelocity = hovering.SurfaceNormal * turnInput * turnSpeed;
        Vector3 currentAngularVelocity = Vector3.Project(rigidbody.angularVelocity, hovering.SurfaceNormal);
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

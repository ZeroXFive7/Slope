﻿using UnityEngine;

public class BoardMovement : MonoBehaviour
{
    [System.Serializable]
    public struct Kinematic
    {
        public float MaxSpeed;
        public float Acceleration;

        public static Kinematic Lerp(Kinematic lhs, Kinematic rhs, float t)
        {
            return new Kinematic()
            {
                MaxSpeed = Mathf.Lerp(lhs.MaxSpeed, rhs.MaxSpeed, t),
                Acceleration = Mathf.Lerp(lhs.Acceleration, rhs.Acceleration, t)
            };
        }
    }

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

    [Header("Visual")]
    [SerializeField]
    private Transform mesh = null;
    [SerializeField]
    private Transform body = null;

    [Header("Turning")]
    [SerializeField]
    private InputAxis pivotAxis = InputAxis.HorizontalLeft;
    [SerializeField]
    private PropertyCurve pivotAngularSpeed;
    //[SerializeField]
    //private PropertyCurve turnDuration;

    [Header("Leaning")]
    [SerializeField]
    private InputAxis leanAxis = InputAxis.HorizontalRight;
    [SerializeField]
    private InputAxis horizontalLeanAxis = InputAxis.HorizontalRight;
    [SerializeField]
    private InputAxis verticalLeanAxis = InputAxis.VerticalRight;
    [SerializeField]
    private float minBoardLeanAmount = 0.85f;
    [SerializeField]
    private Camera camera = null;
    [SerializeField]
    private PropertyCurve turnRadius;
    [SerializeField]
    private PropertyCurve leanAngle;

    [SerializeField]
    private Vector3 downhillDirection = Vector3.forward;
    [SerializeField]
    private float downhillAcceleration = 5.0f;

    [SerializeField]
    private float maxSpeed = 5.0f;

    [SerializeField]
    private float brakingFriction = 10.0f;

    [SerializeField]
    private float turnDuration = 0.1f;

    [SerializeField]
    private LayerMask environmentLayerMask;

    [SerializeField]
    private bool singleThumbstickInput = true;
    [SerializeField]
    private float singleThumbstickDeadZone = 0.5f;

    private Vector3 boardVelocity;
    private Vector3 turnCenter;

    private Vector3 BoardForward
    {
        get
        {
            return Mathf.Sign(Vector3.Dot(transform.forward, boardVelocity.normalized)) * transform.forward;
        }
    }

    private Vector3 BoardRight
    {
        get
        {
            return Mathf.Sign(Vector3.Dot(transform.forward, boardVelocity.normalized)) * transform.right;
        }
    }

    private void FixedUpdate()
    {
        // 1. Read input.
        Vector3 inputGamepadSpace = Vector3.right * ReadInputAxis(horizontalLeanAxis) + Vector3.forward * ReadInputAxis(verticalLeanAxis);
        Vector3 leanForward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized;
        Vector3 leanRight = Vector3.ProjectOnPlane(camera.transform.right, Vector3.up).normalized;
        Vector3 inputWorldSpace = leanRight * inputGamepadSpace.x + leanForward * inputGamepadSpace.z;
        Vector3 inputBoardSpace = transform.InverseTransformVector(inputWorldSpace);

        Vector3 bodyRotationAxis = Vector3.Cross(Vector3.up, inputWorldSpace);
        float bodyRotationAngle = leanAngle.Evaluate(inputGamepadSpace.magnitude);
        body.forward = Quaternion.AngleAxis(bodyRotationAngle, bodyRotationAxis) * Vector3.up;

        if (singleThumbstickInput)
        {
            if (inputGamepadSpace.magnitude < singleThumbstickDeadZone)
            {
                inputGamepadSpace = Vector3.zero;
            }
        }

        Debug.DrawLine(transform.position, transform.position + inputWorldSpace);

        // 2. Accelerate due to gravity.
        ApplyGravity(boardVelocity);

        // 3. Apply board rotation and set desired acceleration and velocity direction due to pivot        
        float pivot = singleThumbstickInput ? inputGamepadSpace.x : ReadInputAxis(pivotAxis);
        ApplyPivot(pivot);

        // 4. Calculate desired acceleration and velocity direction due to lean
        ApplyLean(pivot, inputBoardSpace.x);

        // 5. Apply friction due to board orientation with surface.
        float forwardDotVelocity = Mathf.Abs(Vector3.Dot(BoardForward, boardVelocity.normalized));
        float friction = Mathf.Lerp(brakingFriction, 0.0f, forwardDotVelocity);
        boardVelocity = ApplyFriction(boardVelocity, friction);

        // 6. Resolve physics
        ApplyAcceleration(turnDuration);
    }

    private void ApplyGravity(Vector3 currentVelocity)
    {
        rigidbody.AddForce(downhillDirection * downhillAcceleration);
        //return currentVelocity + downhillDirection * downhillAcceleration * Time.deltaTime;
    }

    private void ApplyPivot(float pivotRate)
    {
        float pivotMagnitude = Mathf.Abs(pivotRate);
        float pivotDirection = pivotRate == 0.0f ? 0.0f : Mathf.Sign(pivotRate);
        transform.rotation *= Quaternion.Euler(0.0f, pivotAngularSpeed.Evaluate(pivotMagnitude) * pivotDirection * Time.deltaTime, 0.0f);
    }

    private void ApplyLean(float pivot, float lean)
    {
        float leanMagnitude = Mathf.Abs(lean);
        if (leanMagnitude < minBoardLeanAmount)
        {
            leanMagnitude = 0.0f;
        }
        turnRadius.Evaluate(leanMagnitude);
        leanAngle.Evaluate(leanMagnitude);

        float turnDirection = lean == 0.0f ? 0.0f : Mathf.Sign(lean);
        mesh.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, -turnDirection * leanAngle);
        turnCenter = transform.position + turnDirection * BoardRight * turnRadius;

        if (leanMagnitude > 0.0f)
        {
            // Calculate turn angle by finding arc around turn circle that would be traveled at current speed.
            // arcLength = 2*pi*r*theta/360
            // theta = (180*arcLength) / (pi*r)
            float arcLength = boardVelocity.magnitude * Time.deltaTime;
            float theta = (180.0f * arcLength) / (Mathf.PI * turnRadius);

            Quaternion rotation = Quaternion.Euler(0.0f, turnDirection * theta, 0.0f);
            transform.rotation *= rotation;
            boardVelocity = rotation * boardVelocity;
        }
    }

    private void ApplyAcceleration(float timeToTurn)
    {
        float speed = boardVelocity.magnitude;
        Vector3 targetVelocity = BoardForward * speed;
        Vector3 acceleration = (targetVelocity - rigidbody.velocity) / timeToTurn;

        boardVelocity += acceleration * Time.deltaTime;
        boardVelocity = Vector3.ClampMagnitude(boardVelocity, maxSpeed);

        rigidbody.AddForce((boardVelocity - rigidbody.velocity));
        //transform.position += boardVelocity * Time.deltaTime;
    }

    private Vector3 ApplyFriction(Vector3 currentVelocity, float friction)
    {
        if (friction > 0.0f)
        {
            float speed = currentVelocity.magnitude;
            if (speed < friction * Time.deltaTime)
            {
                currentVelocity = Vector3.zero;
            }
            else
            {
                currentVelocity += -currentVelocity / speed * friction * Time.deltaTime;
            }
        }
        return currentVelocity;
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

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, turnCenter);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + boardVelocity);
    }
}
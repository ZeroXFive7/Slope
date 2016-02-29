using UnityEngine;
using System.Collections;

public class SimpleSteering : MonoBehaviour
{
    public enum ControlMode
    {
        SkidAndCarve,
        TurnAndLean,
        Warthog,
        SlalomAndCarve
    }

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
    private BoardMovement movement = null;

    [SerializeField]
    private PropertyCurve leanAndSteerBlend;
    [SerializeField]
    private PropertyCurve warthogBlend;

    [HideInInspector]
    public ChaseCamera Camera = null;

    private ControlMode controlMode = ControlMode.TurnAndLean;

    public Vector3 TransformToCameraRelativeWorldSpace(Vector3 vectorCameraSpace)
    {
        Vector3 forward = Vector3.ProjectOnPlane(Camera.transform.forward, movement.transform.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(Camera.transform.right, movement.transform.up).normalized;

        return right * vectorCameraSpace.x + movement.transform.up * vectorCameraSpace.y + forward * vectorCameraSpace.z;
    }

    public void Reset(Transform snapToTransform = null)
    {
        movement.Reset(snapToTransform);

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
        // Default actions.
        if (playerInput.GetButton("Jump"))
        {
            movement.JumpHold();
        }
        if (playerInput.GetButtonUp("Jump"))
        {
            movement.JumpRelease();
        }

        if (playerInput.GetButton("Accelerate"))
        {
            movement.DebugMove();
        }

        if (playerInput.GetButtonDown("Flip Over"))
        {
            movement.FlipOver();
        }

        // Switch control scheme
        if (playerInput.GetButton("Set Controls 1"))
        {
            controlMode = ControlMode.SkidAndCarve;
        }
        else if (playerInput.GetButton("Set Controls 2"))
        {
            controlMode = ControlMode.SlalomAndCarve;
        }
        else if (playerInput.GetButton("Set Controls 3"))
        {
            controlMode = ControlMode.TurnAndLean;
        }
        else if (playerInput.GetButton("Set Controls 4"))
        {
            controlMode = ControlMode.Warthog;
        }

        Vector3 leftStickInputSpace = new Vector3(playerInput.GetAxis("Left Stick Horizontal"), 0.0f, playerInput.GetAxis("Left Stick Vertical"));
        Vector3 rightStickInputSpace = new Vector3(playerInput.GetAxis("Right Stick Horizontal"), 0.0f, playerInput.GetAxis("Right Stick Vertical"));

        switch (controlMode)
        {
            case ControlMode.SkidAndCarve:
                UpdateSkidAndCarve(leftStickInputSpace, rightStickInputSpace);
                break;
            case ControlMode.SlalomAndCarve:
                UpdateSlalomAndCarve(leftStickInputSpace, rightStickInputSpace);
                break;
            case ControlMode.TurnAndLean:
                UpdateTurnAndLean(leftStickInputSpace, rightStickInputSpace);
                break;
            case ControlMode.Warthog:
                UpdateWarthog(leftStickInputSpace, rightStickInputSpace);
                break;
        }
    }

    private void UpdateSkidAndCarve(Vector3 leftStickInputSpace, Vector3 rightStickInputSpace)
    {
        // Carve.
        Vector3 rightStickWorldSpace = TransformToCameraRelativeWorldSpace(rightStickInputSpace);
        Vector3 rightStickBoardSpace = new Vector3(Vector3.Dot(rightStickWorldSpace, movement.BoardRight), 0.0f, Vector3.Dot(rightStickWorldSpace, movement.BoardForward));

        movement.Lean(rightStickInputSpace.x, rightStickInputSpace.z);
        movement.CarvedTurn(rightStickInputSpace.x);

        // Skid.
        movement.SkiddedTurn(leftStickInputSpace.x);
    }

    private void UpdateTurnAndLean(Vector3 leftStickInputSpace, Vector3 rightStickInputSpace)
    {
        float t = 0.0f;
        float rightStickMagnitude = Mathf.Abs(rightStickInputSpace.x);
        if (rightStickMagnitude > Mathf.Epsilon && Mathf.Sign(rightStickInputSpace.x) == Mathf.Sign(leftStickInputSpace.x))
        {
            t = leanAndSteerBlend.Evaluate(rightStickMagnitude);
        }

        // if left stick is 0 then skidded turn is just rightstick
        movement.Lean(rightStickInputSpace.x, rightStickInputSpace.z);
        movement.CarvedTurn(leftStickInputSpace.x * t);
        movement.SkiddedTurn(leftStickInputSpace.x * (1.0f - t));
    }

    private void UpdateWarthog(Vector3 leftStickInputSpace, Vector3 rightStickInputSpace)
    {
        float t = warthogBlend.Evaluate(leftStickInputSpace.z);

        // if left stick is 0 then skidded turn is just rightstick
        movement.Lean(rightStickInputSpace.x, rightStickInputSpace.z);
        movement.CarvedTurn(rightStickInputSpace.x * t);
        movement.SkiddedTurn(rightStickInputSpace.x * (1.0f - t));

    }

    private void UpdateSlalomAndCarve(Vector3 leftStickInputSpace, Vector3 rightStickInputSpace)
    {

    }
}

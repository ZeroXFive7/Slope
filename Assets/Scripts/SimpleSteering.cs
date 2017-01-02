using UnityEngine;
using System.Collections;

public class SimpleSteering : MonoBehaviour
{
    public enum ControlMode
    {
        BasicSteer,
        SkidAndCarve,
        TurnAndLean,
        Warthog,
        SlalomAndCarve,
        FrontBack,
        FrontBackLocal
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

    private ControlMode controlMode = ControlMode.BasicSteer;

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
        else
        {
            movement.TryJumpRelease();
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
            controlMode = ControlMode.FrontBack;
        }
        else if (playerInput.GetButton("Set Controls 3"))
        {
            controlMode = ControlMode.TurnAndLean;
        }
        else if (playerInput.GetButton("Set Controls 4"))
        {
            controlMode = ControlMode.FrontBackLocal;
        }

        Vector3 leftStickInputSpace = new Vector3(playerInput.GetAxis("Left Stick Horizontal"), 0.0f, playerInput.GetAxis("Left Stick Vertical"));
        Vector3 rightStickInputSpace = new Vector3(playerInput.GetAxis("Right Stick Horizontal"), 0.0f, playerInput.GetAxis("Right Stick Vertical"));

        switch (controlMode)
        {
            case ControlMode.BasicSteer:
                UpdateBasicSteer(leftStickInputSpace, rightStickInputSpace);
                break;
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
            case ControlMode.FrontBack:
                UpdateFrontBackSteering(leftStickInputSpace, rightStickInputSpace);
                break;
            case ControlMode.FrontBackLocal:
                UpdateFrontBackSteering(leftStickInputSpace, rightStickInputSpace, false);
                break;
        }
    }

    private void UpdateBasicSteer(Vector3 leftStickInputSpace, Vector3 rightStickInputSpace)
    {
        float throttle = leftStickInputSpace.magnitude;
        float steering = leftStickInputSpace.x;
        movement.Throttle = throttle;
        movement.Steering = steering;
        //movement.Steer(throttle, steering);
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

    private void UpdateFrontBackSteering(Vector3 leftStickInputSpace, Vector3 rightStickInputSpace, bool inputInWorldSpace = true)
    {
        Vector3 leftStickLean = leftStickInputSpace;
        Vector3 leftStickSkid = leftStickLean;
        Vector3 rightStickLean = rightStickInputSpace;
        Vector3 rightStickSkid = rightStickLean;
        if (inputInWorldSpace)
        {
            Vector3 leftStickWorldSpace = TransformToCameraRelativeWorldSpace(leftStickInputSpace);
            leftStickLean = new Vector3(Vector3.Dot(leftStickWorldSpace, movement.BoardRight), 0.0f, Vector3.Dot(leftStickWorldSpace, movement.BoardForward));
            leftStickSkid = new Vector3(Vector3.Dot(leftStickWorldSpace, movement.transform.right), 0.0f, Vector3.Dot(leftStickWorldSpace, movement.transform.forward));

            Vector3 rightStickWorldSpace = TransformToCameraRelativeWorldSpace(rightStickInputSpace);
            rightStickLean = new Vector3(Vector3.Dot(rightStickWorldSpace, movement.BoardRight), 0.0f, Vector3.Dot(rightStickWorldSpace, movement.BoardForward));
            rightStickSkid = new Vector3(Vector3.Dot(rightStickWorldSpace, movement.transform.right), 0.0f, Vector3.Dot(rightStickWorldSpace, movement.transform.forward));
        }

        Debug.DrawLine(movement.Front.position, movement.Front.position + leftStickSkid);
        Debug.DrawLine(movement.Back.position, movement.Back.position + rightStickInputSpace);

        bool isLeaningHorizontal = (Mathf.Abs(leftStickLean.x) > Mathf.Epsilon && Mathf.Abs(rightStickLean.x) > Mathf.Epsilon &&
            Mathf.Sign(leftStickLean.x) == Mathf.Sign(rightStickLean.x));
        if (isLeaningHorizontal)
        {
            // When both stick are pointing in the same direction there is a lean.
            float carveAmount = (leftStickLean.x + rightStickLean.x) / 2.0f;
            movement.CarvedTurn(carveAmount);
        }
        else
        {
            // Otherwise rotate.
            float skidAmount = (leftStickSkid.x - rightStickSkid.x) / 2.0f;
            movement.SkiddedTurn(skidAmount);
        }

        bool isLeaningVertical = (Mathf.Abs(leftStickLean.z) > Mathf.Epsilon && Mathf.Abs(rightStickLean.z) > Mathf.Epsilon &&
            Mathf.Sign(leftStickLean.z) == Mathf.Sign(rightStickLean.z));
        float verticalLeanAmount = (isLeaningVertical ? (leftStickLean.z + rightStickLean.z) / 2.0f : 0.0f);
        float horizontalLeanAmount = (isLeaningHorizontal ? (leftStickLean.x + rightStickLean.x) / 2.0f : 0.0f);
        movement.Lean(horizontalLeanAmount, verticalLeanAmount);
    }

    private void UpdateSlalomAndCarve(Vector3 leftStickInputSpace, Vector3 rightStickInputSpace)
    {

    }
}

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

    [HideInInspector]
    public ChaseCamera Camera = null;

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
        if (movement.IsWipingOut)
        {
            return;
        }

        Vector3 inputGamepadSpace = Vector3.right * playerInput.GetAxis("Lean Horizontal") + Vector3.forward * playerInput.GetAxis("Lean Vertical");
        Vector3 leanForward = Vector3.ProjectOnPlane(Camera.transform.forward, movement.transform.up).normalized;
        Vector3 leanRight = Vector3.ProjectOnPlane(Camera.transform.right, movement.transform.up).normalized;
        Vector3 inputWorldSpace = leanRight * inputGamepadSpace.x + leanForward * inputGamepadSpace.z;
        Vector3 inputBoardSpace = new Vector3(Vector3.Dot(inputWorldSpace, movement.BoardRight), 0.0f, Vector3.Dot(inputWorldSpace, movement.BoardForward));

        movement.Lean(inputGamepadSpace.x, inputGamepadSpace.z);
        movement.CarvedTurn(inputBoardSpace.x);

        float turnInput = playerInput.GetAxis("Turn");
        movement.SkiddedTurn(turnInput);

        if (playerInput.GetButton("Accelerate"))
        {
            movement.DebugMove();
        }

        if (playerInput.GetButton("Jump"))
        {
            movement.JumpHold();
        }

        if (playerInput.GetButtonUp("Jump"))
        {
            movement.JumpRelease();
        }

        if (playerInput.GetButtonDown("Flip Over"))
        {
            movement.FlipOver();
        }
    }
}

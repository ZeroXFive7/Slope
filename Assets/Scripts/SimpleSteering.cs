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
    private BoardMovement movement = null;

    [Header("Trails")]
    [SerializeField]
    private Renderer frontTrailRenderer = null;
    [SerializeField]
    private Renderer backTrailRenderer = null;

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

        movement.Lean(inputBoardSpace.x, inputBoardSpace.z);
        movement.CarvedTurn(inputBoardSpace.x);

        float turnInput = playerInput.GetAxis("Turn");
        movement.SkiddedTurn(turnInput);

        if (playerInput.GetButton("Accelerate"))
        {
            movement.DebugMove();
        }

        if (playerInput.GetButton("Jump"))
        {
            movement.Jump();
        }

        if (playerInput.GetButtonDown("Flip Over"))
        {
            movement.FlipOver();
        }
    }
}

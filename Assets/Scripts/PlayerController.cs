using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [HideInInspector]
    public ChaseCamera Camera = null;

    [Header("Turning")]
    [SerializeField]
    private Transform turnDebugTarget = null;
    [SerializeField]
    private float turnSpeedDegrees = 30.0f;

    private Rewired.Player input = null;
    private Vector3 targetForward = Vector3.forward;

    public PlayerDynamics Dynamics { get; private set; }

    public int PlayerInputId
    {
        get
        {
            if (input == null)
            {
                return -1;
            }
            return input.id;
        }
        set
        {
            input = Rewired.ReInput.players.GetPlayer(value);
        }
    }

    private void Awake()
    {
        Dynamics = GetComponent<PlayerDynamics>();
        targetForward = transform.forward;
    }

    private void OnEnable()
    {
        Reset();
    }

    private void FixedUpdate()
    {
        Vector3 leftStickInputSpace = new Vector3(input.GetAxis("Left Stick Horizontal"), 0.0f, input.GetAxis("Left Stick Vertical"));
        Vector3 rightStickInputSpace = new Vector3(input.GetAxis("Right Stick Horizontal"), 0.0f, input.GetAxis("Right Stick Vertical"));

        targetForward = Vector3.ProjectOnPlane(targetForward, transform.up).normalized;
        targetForward = Quaternion.AngleAxis(leftStickInputSpace.x * turnSpeedDegrees * Time.fixedDeltaTime, transform.up) * targetForward;
        turnDebugTarget.transform.position = transform.position + targetForward * 5.0f;

        Camera.TargetForward = targetForward;
        Dynamics.DesiredForward = targetForward;
    }

    public void Reset(Transform snapToTransform = null)
    {
        Dynamics.Reset(snapToTransform);

        if (Camera != null)
        {
            Camera.transform.position = transform.position;
        }
    }
}

using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [HideInInspector]
    public ChaseCamera Camera = null;

    private PlayerDynamics dynamics = null;

    private Rewired.Player input = null;

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
        dynamics = GetComponent<PlayerDynamics>();
    }

    private void OnEnable()
    {
        Reset();
    }

    private void FixedUpdate()
    {
        Vector3 leftStickInputSpace = new Vector3(input.GetAxis("Left Stick Horizontal"), 0.0f, input.GetAxis("Left Stick Vertical"));
        dynamics.TurnNormalized = leftStickInputSpace.x;
    }

    public void Reset(Transform snapToTransform = null)
    {
        dynamics.Reset(snapToTransform);

        if (Camera != null)
        {
            Camera.transform.position = transform.position;
        }
    }
}

using UnityEngine;

public class ChaseCamera : MonoBehaviour
{
    [SerializeField]
    private Vector3 cameraOffset = new Vector3(0.0f, 1.0f, -2.0f);
    [SerializeField]
    private Vector3 targetOffset = new Vector3(0.0f, 0.0f, 0.0f);
    [SerializeField]
    private float smoothTime = 0.5f;
    [SerializeField]
    private bool rotate = false;

    private Vector3 velocity = Vector3.zero;

    public Camera Camera { get; private set; }

    [HideInInspector]
    public PlayerController Player = null;
    [HideInInspector]
    public Vector3 TargetForward = Vector3.forward;

    public Rect Viewport
    {
        get
        {
            return Camera.rect;
        }
        set
        {
            Camera.rect = value;
        }
    }

    private void Awake()
    {
        Camera = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (rotate)
        {
            transform.position = Vector3.SmoothDamp(transform.position, TransformOffset(cameraOffset), ref velocity, smoothTime, float.MaxValue, Time.fixedDeltaTime);
            transform.rotation = Quaternion.LookRotation(TransformOffset(targetOffset) - transform.position, Vector3.up);
        }
        else
        {
            transform.position = Player.transform.position + cameraOffset;
            transform.rotation = Quaternion.LookRotation(Player.transform.position + targetOffset - transform.position, Vector3.up);
        }
    }

    private Vector3 TransformOffset(Vector3 offset)
    {
        Vector3 steeringForward = Vector3.ProjectOnPlane(TargetForward, Vector3.up).normalized;
        Vector3 steeringRight = Vector3.Cross(Vector3.up, steeringForward).normalized;

        return Player.transform.position + steeringForward * offset.z + steeringRight * offset.x + Vector3.up * offset.y;
    }
}

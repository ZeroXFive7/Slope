using UnityEngine;

public class ChaseCamera : MonoBehaviour
{
    [SerializeField]
    private Vector3 cameraOffset = new Vector3(0.0f, 1.0f, -2.0f);
    [SerializeField]
    private Vector3 targetOffset = new Vector3(0.0f, 0.0f, 0.0f);
    [SerializeField]
    private float smoothTime = 0.5f;

    private Vector3 velocity = Vector3.zero;

    [Header("Component References")]
    [SerializeField]
    private new Camera camera = null;

    [HideInInspector]
    public SimpleSteering Steering = null;

    public Rect Viewport
    {
        get
        {
            return camera.rect;
        }
        set
        {
            camera.rect = value;
        }
    }

    private void LateUpdate()
    {
        transform.position = Vector3.SmoothDamp(transform.position, TransformOffset(cameraOffset), ref velocity, smoothTime);
        transform.rotation = Quaternion.LookRotation(TransformOffset(targetOffset) - transform.position, Vector3.up);
    }

    private Vector3 TransformOffset(Vector3 offset)
    {
        Vector3 steeringForward = (Steering.Velocity.magnitude > 0.001f && !Steering.IsWipingOut) ? Steering.Velocity : Steering.transform.forward;
        steeringForward = Vector3.ProjectOnPlane(steeringForward, Vector3.up).normalized;

        Vector3 steeringRight = Vector3.Cross(Vector3.up, steeringForward).normalized;
        return Steering.transform.position + steeringForward * offset.z + steeringRight * offset.x + Vector3.up * offset.y;
    }
}

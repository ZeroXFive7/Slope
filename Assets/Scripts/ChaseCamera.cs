using UnityEngine;

public class ChaseCamera : MonoBehaviour
{
    [SerializeField]
    private Transform targetTransform= null;

    [SerializeField]
    private Vector3 cameraOffset = new Vector3(0.0f, 1.0f, -2.0f);
    [SerializeField]
    private Vector3 targetOffset = new Vector3(0.0f, 0.0f, 0.0f);

    private void LateUpdate()
    {
        transform.position = TransformOffset(cameraOffset);
        transform.rotation = Quaternion.LookRotation(TransformOffset(targetOffset) - transform.position, Vector3.up);
    }

    private Vector3 TransformOffset(Vector3 offset)
    {
        Vector3 offsetLocalXZ = Vector3.ProjectOnPlane(offset, Vector3.up);
        return targetTransform.TransformPoint(offsetLocalXZ) + Vector3.up * offset.y;
    }
}

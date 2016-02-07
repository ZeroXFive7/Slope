using UnityEngine;

public class ChaseCamera : MonoBehaviour
{
    [SerializeField]
    private Transform targetTransform= null;

    [SerializeField]
    private Vector3 targetOffsetPosition = new Vector3(0.0f, 1.0f, -2.0f);

    private void LateUpdate()
    {
        transform.position = targetTransform.position + targetOffsetPosition;
        transform.rotation = Quaternion.LookRotation(targetTransform.position - transform.position, Vector3.up);
    }
}

using UnityEngine;

public class PlayerDynamics : MonoBehaviour
{
    private new CharacterController collider = null;

    private Vector3 previousPosition = Vector3.zero;

    [System.NonSerialized]
    public float Steering = 0.0f;

    private void Awake()
    {
        collider = GetComponent<CharacterController>();
    }

    private void FixedUpdate()
    {
        Vector3 acceleration = Vector3.zero;

        if (!collider.isGrounded)
        {
            acceleration += Physics.gravity;
        }

        Vector3 velocity = (transform.position - previousPosition) / Time.fixedDeltaTime;
        previousPosition = transform.position;

        velocity += acceleration * Time.fixedDeltaTime;
        collider.Move(velocity * Time.fixedDeltaTime);
    }

    public void Reset(Transform snapToTransform = null)
    {
        if (snapToTransform)
        {
            transform.position = snapToTransform.position;
            transform.rotation = snapToTransform.rotation;
        }

        previousPosition = transform.position;
    }
}

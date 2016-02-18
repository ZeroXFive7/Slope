using UnityEngine;
using System.Collections;

public class RigidbodyHovering : MonoBehaviour
{
    [System.Serializable]
    public class HoverThruster
    {
        [SerializeField]
        private string name;
        [SerializeField]
        private Transform transform;

        private Vector3 previousPosition = Vector3.zero;

        public Transform Transform { get { return transform; } }

        public Vector3 Velocity { get; private set; }

        public void Reset()
        {
            previousPosition = transform.position;
            Velocity = Vector3.zero;
        }

        public void Update()
        {
            Velocity = (transform.position - previousPosition) / Time.deltaTime;
            previousPosition = transform.position;
        }
    }

    [SerializeField]
    private HoverThruster[] thrusters = null;
    [SerializeField]
    private new Rigidbody rigidbody = null;

    [Header("Tunables")]
    [SerializeField]
    private float hoverHeight = 0.33f;
    [SerializeField]
    private float dampingDistance = 0.2f;
    [SerializeField]
    private float hoverSpeed = 10.0f;
    [SerializeField]
    private LayerMask environmentLayerMask;

    private void OnEnable()
    {
        for (int i = 0; i < thrusters.Length; ++i)
        {
            thrusters[i].Reset();
        }
    }

    private void FixedUpdate()
    {
        for (int i = 0; i < thrusters.Length; ++i)
        {
            thrusters[i].Update();

            RaycastHit hit;
            if (Physics.Raycast(thrusters[i].Transform.position, -Vector3.up, out hit, hoverHeight, environmentLayerMask))
            {
                Vector3 targetPosition = hit.point + Vector3.up * hoverHeight;
                Vector3 vectorToTarget = targetPosition - thrusters[i].Transform.position;

                float desiredSpeed = hoverSpeed * Mathf.Clamp01(vectorToTarget.magnitude / dampingDistance);
                Vector3 desiredVelocity = vectorToTarget.normalized * desiredSpeed;
                Vector3 acceleration = desiredVelocity - thrusters[i].Velocity;

                rigidbody.AddForceAtPosition(acceleration, thrusters[i].Transform.position);
            }
        }
    }
}

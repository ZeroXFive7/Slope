﻿using UnityEngine;
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
        [SerializeField]
        private FloatPIDController controller;

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
    private PropertyCurve hoverHeight;
    [SerializeField]
    private float dampingDistance = 0.2f;
    [SerializeField]
    private float hoverSpeed = 10.0f;
    [SerializeField]
    private LayerMask environmentLayerMask;

    public Vector3 SurfaceNormal { get; private set; }

    public bool IsGrounded { get; private set; }

    public float HoverHeightScalar
    {
        set
        {
            hoverHeight.Evaluate(Mathf.Clamp01(value));
        }
    }

    private void OnEnable()
    {
        hoverHeight.Evaluate(1.0f);
        IsGrounded = false;
        SurfaceNormal = Vector3.up;

        for (int i = 0; i < thrusters.Length; ++i)
        {
            thrusters[i].Reset();
        }
    }

    private void FixedUpdate()
    {
        Vector3 newSurfaceNormal = Vector3.zero;
        for (int i = 0; i < thrusters.Length; ++i)
        {
            thrusters[i].Update();

            RaycastHit hit;
            if (Physics.Raycast(thrusters[i].Transform.position, -thrusters[i].Transform.up, out hit, hoverHeight, environmentLayerMask))
            {
                Debug.DrawLine(thrusters[i].Transform.position, hit.point, Color.yellow);

                Vector3 targetPosition = hit.point + SurfaceNormal * hoverHeight;
                Vector3 vectorToTarget = targetPosition - thrusters[i].Transform.position;

                float desiredSpeed = hoverSpeed * Mathf.Clamp01(vectorToTarget.magnitude / dampingDistance);
                Vector3 desiredVelocity = vectorToTarget.normalized * desiredSpeed;
                Vector3 acceleration = desiredVelocity - thrusters[i].Velocity;
                acceleration = Vector3.Project(acceleration, hit.normal);

                rigidbody.AddForceAtPosition(acceleration, thrusters[i].Transform.position);

                newSurfaceNormal += hit.normal;
            }
        }

        float magnitude = newSurfaceNormal.magnitude;
        if (magnitude < 0.001f)
        {
            IsGrounded = false;
            newSurfaceNormal = Vector3.up;
        }
        else
        {
            IsGrounded = true;
            newSurfaceNormal /= magnitude;
        }

        SurfaceNormal = newSurfaceNormal;
    }
}

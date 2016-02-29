using UnityEngine;
using System.Collections;

public class BodyBalance : MonoBehaviour
{
    [SerializeField]
    private Rigidbody body = null;
    [SerializeField]
    private Rigidbody board = null;

    [SerializeField]
    private Vector3PIDController upVectorController;
    [SerializeField]
    private Vector3PIDController angularVelocityController;

    private void OnEnable()
    {
        upVectorController.Reset();
        angularVelocityController.Reset();
    }

    private void FixedUpdate()
    {
        Quaternion rotation = Quaternion.FromToRotation(body.transform.up, board.transform.up);
        Vector3 axis;
        float angle;
        rotation.ToAngleAxis(out angle, out axis);

        body.AddTorque(angularVelocityController.Update(-body.angularVelocity, Time.fixedDeltaTime));
        body.AddTorque(upVectorController.Update(axis * angle, Time.fixedDeltaTime));
    }
}

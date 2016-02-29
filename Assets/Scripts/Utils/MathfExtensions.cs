using UnityEngine;
using System.Collections;

public static class MathfExtensions
{
    public static Vector3 ClampMagnitudeMinMax(Vector3 vector, float minMagnitude, float maxMagnitude)
    {
        Vector3 minVector = new Vector3(minMagnitude, minMagnitude, minMagnitude);
        Vector3 maxVector = new Vector3(maxMagnitude, maxMagnitude, maxMagnitude);
        return Vector3.Min(Vector3.Max(vector, minVector), maxVector);
    }
}

using UnityEngine;

[System.Serializable]
public struct PropertyCurve
{
    [SerializeField]
    private AnimationCurve curve;
    [SerializeField]
    private float minValue;
    [SerializeField]
    private float maxValue;

    private float value;

    public float Evaluate(float t)
    {
        value = Mathf.Lerp(minValue, maxValue, curve.Evaluate(t));
        return value;
    }

    public static implicit operator float(PropertyCurve p)
    {
        return p.value;
    }
}

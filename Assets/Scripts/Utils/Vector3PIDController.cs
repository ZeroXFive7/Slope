using UnityEngine;
using System.Collections.Generic;
using System;

public abstract class PIDController<T>
{
    [Header("Coefficients")]
    [SerializeField]
    protected float proportionalCoefficient = 0.5f;
    [SerializeField]
    protected float integralCoefficient = 0.5f;
    [SerializeField]
    protected float derivativeCoefficient = 0.5f;

    [Header("Output Clamping")]
    [SerializeField]
    protected float minOutput;
    [SerializeField]
    protected float maxOutput;

    [Header("Integral Component Clamping")]
    [SerializeField]
    protected float minIntegralComponent;
    [SerializeField]
    protected float maxIntegralComponent;

    // Rolling sum of previous delta errors. Discrete integration of error.
    protected T integral;

    // Used to calculate instantaneous change in error for derivative.
    protected T previousError;

    public abstract void Reset();

    public abstract T Update(T currentError, float deltaTime);
}

[System.Serializable]
public class Vector3PIDController : PIDController<Vector3>
{
    public override void Reset()
    {
        integral = Vector3.zero;
        previousError = Vector3.zero;
    }

    public override Vector3 Update(Vector3 currentError, float deltaTime)
    {
        integral += currentError * deltaTime;
        //integral = MathfExtensions.ClampMagnitudeMinMax(integral, minIntegralComponent, maxIntegralComponent);

        Vector3 derivative = (currentError - previousError) / deltaTime;
        previousError = currentError;

        Vector3 output = proportionalCoefficient * currentError + integralCoefficient * integral + derivativeCoefficient * derivative;
        //output = MathfExtensions.ClampMagnitudeMinMax(output, minOutput, maxOutput);

        return output;
    }
}

[System.Serializable]
public class FloatPIDController : PIDController<float>
{
    public override void Reset()
    {
        integral = 0.0f;
        previousError = 0.0f;
    }

    public override float Update(float currentError, float deltaTime)
    {
        integral += currentError * deltaTime;
        integral = Mathf.Clamp(integral, minIntegralComponent, maxIntegralComponent);

        float derivative = (currentError - previousError) / deltaTime;
        previousError = currentError;

        float output = proportionalCoefficient * currentError + integralCoefficient * integral + derivativeCoefficient * derivative;
        output = Mathf.Clamp(output, minOutput, maxOutput);

        return output;
    }
}

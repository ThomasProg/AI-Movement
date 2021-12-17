using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Separation : SteeringBehaviour
{
    Group group;

    AnimationCurve curve = AnimationCurve.EaseInOut(0,1,1,0);

    float distMaxToApplyEffect = 5;

    public Separation(Group newGroup)
    {
        group = newGroup;
        Multiplicator = 4;
    }

    public float Multiplicator { get; set; }

    public Vector3 ApplySteering(Transform unit)
    {
        Vector3 dist = unit.position - group.ComputeCenter();

        if (dist.sqrMagnitude >= distMaxToApplyEffect * distMaxToApplyEffect)
            return Vector3.zero;

        return Multiplicator * dist * curve.Evaluate(distMaxToApplyEffect - dist.magnitude);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Group
{
    public List<Transform> boids = new List<Transform>();

    public Vector3 ComputeCenter()
    {
        Vector3 center = Vector3.zero;

        foreach (Transform t in boids)
        {
            center += t.position;
        }

        return center / boids.Count;
    }
};

public class Cohesion : SteeringBehaviour
{
    Group group;

    public Cohesion(Group newGroup)
    {
        group = newGroup;
    }

    public float Multiplicator { get; set; }

    public Vector3 ApplySteering(Transform unit)
    {
        return Multiplicator * (group.ComputeCenter() - unit.position).normalized;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface SteeringBehaviour 
{
    public float Multiplicator { get; set; }

     public abstract Vector3 ApplySteering(Transform unit);
}

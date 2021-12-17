using Navigation;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

public class Movement : MonoBehaviour
{
    [SerializeField]
    private float MaxSpeed = 10f;

    private Vector3 Velocity = Vector3.zero;
    private float RotationY = 0f;

    private float PosOffsetY = 0.1f;
    private Vector3 _TargetPos;

    [SerializeField]
    float distWeight = 1;

    public Vector3 TargetPos
    {
        get { return _TargetPos; }
        set 
        { 
            _TargetPos = value; 
            _TargetPos.y += PosOffsetY;

            Dictionary<Node, NodeData> pathsToStart = new Dictionary<Node, NodeData>();
            Node n = TileNavGraph.Instance.GetNode(TargetPos);
            if (n != null)
                path = Path.GetPathTo(ref pathsToStart, transform.position, n.Position, 1, distWeight);
            addDebugLines(path, pathsToStart);
            //path = Path.GetPathTo(transform.position, TileNavGraph.Instance.GetNode(_TargetPos).Position); 
        }
    }

	public void Stop()
	{
		Velocity = Vector3.zero;
	}

    protected float GetOrientationFromDirection(Vector3 direction)
    {
        if (direction.magnitude > 0)
        {
            return Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        }
        return RotationY;
    }

    #region MonoBehaviour methods

    private void Start()
    {
        _TargetPos = transform.position;
    }

    Path path = null;

    // Update is called once per frame
    private void Update ()
	{
        if (path == null)
            return;

        Vector3 nextNodePos = path.nodePos();

        Vector3 seekVelocity = nextNodePos - transform.position;
        //Velocity = Vector3.Lerp(Velocity, nextNodePos - transform.position, 0.3f);
        //RotationY = GetOrientationFromDirection(seekVelocity);

        //if (seekVelocity.magnitude < MaxSpeed)
        //{
        //    if (!path.next())
        //    {
        //        path = null;
        //    }
        //}

        float angularSpeed = 10f;
        Velocity += (seekVelocity - Velocity) * Time.deltaTime * angularSpeed;

        // truncate to max speed
        if (seekVelocity.magnitude > /*MaxSpeed * Time.deltaTime **/ 3)
		{
            Velocity.Normalize();
            Velocity *= MaxSpeed;
		}
        else
        //if (seekVelocity.magnitude < 1)
        {
            if (!path.next())
            {
                path = null;
            }
        }

        //if (Input.GetKeyDown(KeyCode.Space))
        //{
        //    if (!path.next())
        //    {
        //        path = null;
        //    }
        //}

        //Velocity = Vector3.Lerp(Velocity, seekVelocity, Time.deltaTime);

        // Update position and rotation
        transform.position += Velocity * Time.deltaTime;
        RotationY = GetOrientationFromDirection(Velocity);
        transform.eulerAngles = Vector3.up * RotationY;
    }

    #endregion

    #region Display Debug

    List<System.Tuple<Vector3, Vector3>> validPath = new List<System.Tuple<Vector3, Vector3>>();
    //List<List<System.Tuple<Vector3, Vector3>>> otherPaths = new List<List<System.Tuple<Vector3, Vector3>>>();
    List<System.Tuple<Vector3, Vector3>> otherPaths = new List<System.Tuple<Vector3, Vector3>>();

    void addDebugLines(Path p, Dictionary<Node, NodeData> pathsToStart)
    {
        //Gizmos.color = Color.red;
        while (p != null && p.GetFollowingPath() != null)
        {
            var p1 = p.nodePos();
            var p2 = p.GetFollowingPath().nodePos();
            validPath.Add(new System.Tuple<Vector3, Vector3>(p1, p2));

            p = p.GetFollowingPath();
        }

        foreach (NodeData nData in pathsToStart.Values)
        {
            if (nData.parentNode == null)
                continue;
            otherPaths.Add(new System.Tuple<Vector3, Vector3>(nData.currentNode.Position, nData.parentNode.currentNode.Position));
        }
    }

    private void OnDrawGizmos()
	{
		Gizmos.color = Color.blue;
		Gizmos.DrawRay(transform.position, Velocity);

        Handles.color = Color.blue;
        foreach (System.Tuple<Vector3, Vector3> l in otherPaths)
        {
            Handles.DrawLine(l.Item1, l.Item2, 3);
        }

        Handles.color = Color.red;
        foreach (System.Tuple<Vector3, Vector3> l in validPath)
        {
            Handles.DrawLine(l.Item1, l.Item2, 3);
        }
    }

    #endregion
}

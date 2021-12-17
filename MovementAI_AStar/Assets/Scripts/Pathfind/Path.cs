using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Navigation;

class NodeData
{
    public Node currentNode;
    public NodeData parentNode;
    //int totalCost;
    //float distToTarget;
    float totalCost; // without dist
    float heuristic;

    public NodeData(Node currentNode, float heuristic = float.MaxValue, float totalCost = float.MaxValue, NodeData parentNode = null)
    {
        this.parentNode = parentNode;
        this.currentNode = currentNode;
        this.heuristic = heuristic;
        this.totalCost = totalCost;
    }

    public float GetHeuristic()
    {
        return heuristic;
    }

    public float GetTotalCost()
    {
        return totalCost;
    }

    //public static float ComputeTotalCost(float oldNodeCost, float travelCost)
    //{
    //    return oldNodeCost + travelCost;
    //}

    //public static float ComputeHeuristic(float oldNodeCost, float travelCost, float distToTarget)
    //{
    //    return ComputeTotalCost(oldNodeCost, travelCost) + distToTarget;
    //}

    public static float ComputeHeuristic(float totalCost, float distToTarget, float distWeight)
    {
        return totalCost + distToTarget * distWeight;
    }

    public static Path MakePath(NodeData nodeData)
    {
        // Reverse the path
        Path followingPath = null;
        int j = 0;
        while (nodeData.parentNode != null)
        {
            j++;
            if (j > 10000)
                throw new System.Exception("infinite loop");

            followingPath = new Path(nodeData.currentNode, followingPath);
            nodeData = nodeData.parentNode;
        }
        return new Path(nodeData.currentNode, followingPath);
    }
}

class NodeHeuristicComparer : IComparer<float>
{
    public int Compare(float x, float y)
    {
        // Removes equality
        if (x > y)
            return 1;
        else
            return -1;
    }
}


class Path
{
    Node currentNode;
    Path followingPath;

    public Path(Node currentNode, Path followingPath)
    {
        this.currentNode = currentNode;
        this.followingPath = followingPath;
    }


    public bool next()
    {
        if (followingPath != null)
        {
            currentNode = followingPath.currentNode;
            followingPath = followingPath.followingPath;
            return true;
        }
        else
            return false;
    }

    public Vector3 nodePos()
    {
        return currentNode.Position;
    }

    public Path GetFollowingPath()
    {
        return followingPath;
    }

    public static Path GetPathTo(Vector3 startPos, Vector3 endPos, float offsetToStop = 1)
    {
        Dictionary<Node, NodeData> pathsToStart = new Dictionary<Node, NodeData>();
        return GetPathTo(ref pathsToStart, startPos, endPos, offsetToStop);
    }

    // A*
    public static Path GetPathTo(ref Dictionary<Node, NodeData> pathsToStart, Vector3 startPos, Vector3 endPos, float offsetToStop = 1, float distWeight = 100)
    {
        //SortedSet<NodeData> toProcess = new SortedSet<NodeData>(new NodeDataComparer());
        SortedList<float, NodeData> toProcess = new SortedList<float, NodeData>(new NodeHeuristicComparer());

        {
            Node startNode = TileNavGraph.Instance.GetNode(startPos);
            NodeData startNodeData = new NodeData(startNode, 0, 0);
            toProcess.Add(0, startNodeData);
            pathsToStart.Add(startNode, startNodeData);
        }

        int i = 0;
        while (toProcess.Count > 0)
        {
            i++;
            if (i > 10000)
                throw new System.Exception("infinite loop");

            float nodeHeuristic = toProcess.Keys[0];
            NodeData nodeData = toProcess.Values[0];
            toProcess.RemoveAt(0);

            // If at destination
            if ((nodeData.currentNode.Position - endPos).sqrMagnitude < offsetToStop * offsetToStop)
            {
                return NodeData.MakePath(nodeData);
            }

            List<Connection> connections;
            if (TileNavGraph.Instance.GetConnectionGraph().TryGetValue(nodeData.currentNode, out connections))
            {
                foreach (Connection connection in connections)
                {
                    float newTotalCost = nodeData.GetTotalCost() + connection.Cost;
                    float newHeuristic = NodeData.ComputeHeuristic(newTotalCost, (endPos - connection.ToNode.Position).magnitude, distWeight);


                    NodeData data;
                    if (pathsToStart.TryGetValue(connection.ToNode, out data))
                    {
                        if (data.GetTotalCost() > newTotalCost)
                        {
                            NodeData newNodeData = new NodeData(connection.ToNode, newHeuristic, newTotalCost, nodeData);
                            pathsToStart[connection.ToNode] = newNodeData;
                            toProcess.Add(newHeuristic, newNodeData);
                        }
                    }
                    else
                    {
                        NodeData newNodeData = new NodeData(connection.ToNode, newHeuristic, newTotalCost, nodeData);
                        pathsToStart[connection.ToNode] = newNodeData;
                        toProcess.Add(newHeuristic, newNodeData);
                    }


                }
            }

        }

        return null;
    }
}
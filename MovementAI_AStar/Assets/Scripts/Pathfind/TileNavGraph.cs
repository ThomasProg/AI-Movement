using UnityEngine;
using System.Collections.Generic;
using System.Threading;

namespace Navigation
{
    public class TileNavGraph : MonoBehaviour
    {
	    static TileNavGraph _Instance = null;
	    static public TileNavGraph Instance
	    {
		    get
		    {
			    if (_Instance == null)
				    _Instance = FindObjectOfType<TileNavGraph>();
			    return _Instance;
		    }
	    }
        
        [SerializeField]
        private int GrassCost = 1;
        [SerializeField]
        private int UnreachableCost = int.MaxValue;

        [SerializeField]
        private int GridSizeH = 100;
        [SerializeField]
        private int GridSizeV = 100;
        [SerializeField]
        private int SquareSize = 1;
        [SerializeField]
        private int MaxHeight = 10;
        [SerializeField]
        private int MaxWalkableHeight = 5;

        // enable / disable debug Gizmos
        [SerializeField]
        private bool DrawGrid = false;
        [SerializeField]
        private bool DisplayAllNodes = false;
        [SerializeField]
        private bool DisplayAllLinks = false;

        // threading
        private Thread GraphThread = null;

        // Grid parameters
        private Vector3 GridStartPos = Vector3.zero;
        private int NbTilesH = 0;
        private int NbTilesV = 0;

        // Nodes
        private List<Node> NodeList = new List<Node>();
        private Dictionary<Node, List<Connection>> ConnectionsGraph = new Dictionary<Node, List<Connection>>();

        public Dictionary<Node, List<Connection>> GetConnectionGraph()
        {
            return ConnectionsGraph;
        }

        private void Awake ()
        {
            CreateTiledGrid();
	    }

        private void Start()
        {
            // Generate navigation graph in a new thread
            ThreadStart threadStart = new ThreadStart(CreateGraph);
            GraphThread = new Thread(threadStart);
            GraphThread.Start();
        }

        // Create all nodes for the tiled grid
        private void CreateTiledGrid()
	    {
		    NodeList.Clear();

            GridStartPos = transform.position + new Vector3(-GridSizeH / 2f, 0f, -GridSizeV / 2f);

		    NbTilesH = GridSizeH / SquareSize;
		    NbTilesV = GridSizeV / SquareSize;

		    for(int i = 0; i < NbTilesV; i++)
		    {
			    for(int j = 0; j < NbTilesH; j++)
			    {
				    Node node = new Node();
                    Vector3 nodePos = GridStartPos + new Vector3((j + 0.5f) * SquareSize, 0f, (i + 0.5f) * SquareSize);

				    int Weight = 0;
				    RaycastHit hitInfo = new RaycastHit();

                    // Always compute node Y pos from floor collision
                    if (Physics.Raycast(nodePos + Vector3.up * MaxHeight, Vector3.down, out hitInfo, MaxHeight + 1, 1 << LayerMask.NameToLayer("Floor")))
                    {
                        if (Weight == 0)
                            Weight = hitInfo.point.y >= MaxWalkableHeight ? UnreachableCost : GrassCost;
                        nodePos.y = hitInfo.point.y;
                    }

                    node.Weight = Weight;
				    node.Position = nodePos;
				    NodeList.Add(node);
			    }
		    }
        }

        // Cast a ray for each possible corner of a tile node for better accuracy
        private bool RaycastNode(Vector3 nodePos, string layerName, out RaycastHit hitInfo)
        {
            int layer = 1 << LayerMask.NameToLayer(layerName);

            if (Physics.Raycast(nodePos - new Vector3(0f, 0f, SquareSize / 2f) + Vector3.up * 5, Vector3.down, out hitInfo, MaxHeight + 1, layer))
                return true;
            if (Physics.Raycast(nodePos + new Vector3(0f, 0f, SquareSize / 2f) + Vector3.up * 5, Vector3.down, out hitInfo, MaxHeight + 1, layer))
                return true;
            if (Physics.Raycast(nodePos - new Vector3(SquareSize / 2f, 0f, 0f) + Vector3.up * 5, Vector3.down, out hitInfo, MaxHeight + 1, layer))
                return true;
            if (Physics.Raycast(nodePos + new Vector3(SquareSize / 2f, 0f, 0f) + Vector3.up * 5, Vector3.down, out hitInfo, MaxHeight + 1, layer))
                return true;
            return false;
        }
        
        // Compute possible connections between each nodes
        private void CreateGraph()
        {
            foreach (Node node in NodeList)
            {
                if (IsNodeWalkable(node))
                {
                    ConnectionsGraph.Add(node, new List<Connection>());
                    foreach (Node neighbour in GetNeighbours(node))
                    {
                        Connection connection = new Connection();
                        connection.Cost = ComputeConnectionCost(node, neighbour);
                        connection.FromNode = node;
                        connection.ToNode = neighbour;
                        ConnectionsGraph[node].Add(connection);
                    }
                }
            }
        }

        private int ComputeConnectionCost(Node fromNode, Node toNode)
        {
            return (int)((fromNode.Weight + toNode.Weight) * (toNode.Position - fromNode.Position).magnitude);
        }

        public bool IsPosValid(Vector3 pos)
        {
            if (GraphThread.ThreadState == ThreadState.Running)
                return false;

            if (pos.x > (-GridSizeH / 2) && pos.x < (GridSizeH / 2) && pos.z > (-GridSizeV / 2) && pos.z < (GridSizeV / 2))
                return true;
            return false;
        }

        // Converts world 3d pos to tile 2d pos
        private Vector2Int GetTileCoordFromPos(Vector3 pos)
	    {
            Vector3 realPos = pos - GridStartPos;
            Vector2Int tileCoords = Vector2Int.Zero;
            tileCoords.x = Mathf.FloorToInt(realPos.x / SquareSize);
            tileCoords.y = Mathf.FloorToInt(realPos.z / SquareSize);
		    return tileCoords;
	    }

        public Node GetNode(Vector3 pos)
        {
            return GetNode(GetTileCoordFromPos(pos));
        }

        private Node GetNode(Vector2Int pos)
        {
            return GetNode(pos.x, pos.y);
        }

        private Node GetNode(int x, int y)
        {
            int index = y * NbTilesH + x;
            if (index >= NodeList.Count || index < 0)
                return null;

            return NodeList[index];
        }

        private bool IsNodeWalkable(Node node)
        {
            return node.Weight < UnreachableCost;
        }

        private void TryToAddNode(List<Node> list, Node node)
        {
            if (IsNodeWalkable(node))
            {
                list.Add(node);
            }
        }

        private List<Node> GetNeighbours(Node node)
	    {
            Vector2Int tileCoord = GetTileCoordFromPos(node.Position);
            int x = tileCoord.x;
            int y = tileCoord.y;

		    List<Node> nodes = new List<Node>();

		    if (x > 0)
		    {
			    if (y > 0)
                    TryToAddNode(nodes, GetNode(x - 1, y - 1));
                TryToAddNode(nodes, NodeList[(x - 1) + y * NbTilesH]);
			    if (y < NbTilesV - 1)
                    TryToAddNode(nodes, NodeList[(x - 1) + (y + 1) * NbTilesH]);
		    }

		    if (y > 0)
                TryToAddNode(nodes, NodeList[x + (y - 1) * NbTilesH]);
		    if (y < NbTilesV - 1)
                TryToAddNode(nodes, NodeList[x + (y + 1) * NbTilesH]);

		    if (x < NbTilesH - 1)
		    {
			    if (y > 0)
                    TryToAddNode(nodes, NodeList[(x + 1) + (y - 1) * NbTilesH]);
                TryToAddNode(nodes, NodeList[(x + 1) + y * NbTilesH]);
			    if (y < NbTilesV - 1)
                    TryToAddNode(nodes, NodeList[(x + 1) + (y + 1) * NbTilesH]);
		    }

		    return nodes;
	    }

#region Gizmos
        private void OnDrawGizmos()
	    {
            if (DrawGrid)
            {
                float gridHeight = 0.01f;
                Gizmos.color = Color.yellow;
                for (int i = 0; i < NbTilesV + 1; i++)
                {
                    Vector3 startPos = new Vector3(-GridSizeH / 2f, gridHeight, -GridSizeV / 2f + i * SquareSize);
                    Gizmos.DrawLine(startPos, startPos + Vector3.right * GridSizeV);

                    for (int j = 0; j < NbTilesH + 1; j++)
                    {
                        startPos = new Vector3(-GridSizeH / 2f + j * SquareSize, gridHeight, -GridSizeV / 2f);
                        Gizmos.DrawLine(startPos, startPos + Vector3.forward * GridSizeV);
                    }
                }
            }

            if (DisplayAllNodes)
            {
		        for(int i = 0; i < NodeList.Count; i++)
		        {
                    Node node = NodeList[i];
                    Gizmos.color = IsNodeWalkable(node) ? Color.green : Color.red;
                    Gizmos.DrawCube(node.Position, Vector3.one * 0.25f);
		        }
            }
            if (DisplayAllLinks)
            {
                foreach (Node crtNode in NodeList)
                {
                    if (ConnectionsGraph.ContainsKey(crtNode))
                    {
                        foreach (Connection c in ConnectionsGraph[crtNode])
                        {
                            Gizmos.color = Color.green;
                            Gizmos.DrawLine(c.FromNode.Position, c.ToNode.Position);
                        }
                    }
                }
            }
	    }
#endregion
    }
}



using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;



public class Node
{
    public Vector3 pos;
   
    // top edge connects node with the neighbor which has z + padding.z
    // bottom edge connects node with the neighbor which has z - padding.z
    // left edge connects node with the neighbor which has  x - padding.x
    // right edge connects node with the neighbor which has x + padding.x
    

    public Edge[] edges = new Edge[4];

    public Node() { }

}

public enum EdgeIndex
{
    Top = 0,
    Bottom = 1,
    Left = 2,
    Right = 3
};

public enum EdgeDirection
{
    Top = 0,
    Bottom = 1,
    Left = 2,
    Right = 3
}
// struct of Node that contains the data structure also seen by the gpu.
public struct NodeCB
{
    public Vector3 nodePos;
    public Vector3 topPos;
    public Vector3 botPos;
    public Vector3 leftPos;
    public Vector3 rightPos;
    public int isVerticaltop;
    public int isVerticalbot;
    public int isVerticalleft;
    public int isVerticalright;
    public int isActivetop;
    public int isActivebot;
    public int isActiveleft;
    public int isActiveright;
    public int isExploredtop;
    public int isExploredbot;
    public int isExploredleft;
    public int isExploredright;
}
 struct Example
{
    public Vector2 pos;
    public Vector2 rot;
}

public class Edge
{
    public Vector3 pos;
    public bool isVertical; // whether the edge is vertical or horizonal.
    public bool isActive;   // will be used to indicate whether this edge will be part of the maze (mst) and should be rendered.
    public bool isExplored; // will be used when calculating the MST (Minimum Spanning Tree).


    

    public Edge(Vector3 pos, bool isVertical, bool isActive = true, bool isExplored = false)
    {
        this.pos = pos;
        this.isActive = isActive;
        this.isVertical = isVertical;
        this.isExplored = isExplored;
    }

    
}
public class GridGraph : MonoBehaviour
{
    public int xCells = 20;
    public int zCells = 20;
    public int Width = 100;
    public int Height = 100;
    public GameObject Unit;


    public bool visualizeEdges, visualizeNodes;

    [HideInInspector]
    public Vector2 padding;

    Vector3 cubeScale;
    [HideInInspector]
    public Vector3 EdgeHorizontalScale;
    [HideInInspector]
    public Vector3 EdgeVerticalScale;
    
    //public List<Node> nodes;
    public Node[] nodes;
    List<GameObject> PlaneEdges;
    List<GameObject> CubeNodes;

    public ComputeShader generatorCS;
    private ComputeBuffer nodeBuffer;
    private GameObject floor;
    public Material floorMat;
    // Start is called before the first frame update
    void Start()
    {
       
    }
    public Node[] getGraph()
    {
        return nodes;
    }

    public ComputeBuffer getNodeBuffer()
    {
        return nodeBuffer;
    }

    public void GenerateFloor()
    {
        floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.GetComponent<MeshRenderer>().receiveShadows = true;
        floor.GetComponent<MeshRenderer>().material = floorMat;
        floor.GetComponent<MeshRenderer>().material.mainTextureScale = new Vector2(Width, Height) * 0.2f;
        floor.transform.position = new Vector3(Width * 0.5f, 0.0f, Height * 0.5f);
        floor.transform.localScale = new Vector3(0.1f * (Width + padding.x), 1.0f, 0.1f * (Height + 4.0f * padding.y));
    }

    public void ParallelGenerate ()
    {
        int size = 5 * 3 * sizeof(float) + 12 * sizeof(int);
        nodeBuffer = new ComputeBuffer((xCells + 1) * (zCells + 1), size, ComputeBufferType.Default);
        padding = new Vector2((float)Width / (float)xCells, (float)Height / (float)zCells);
        GenerateFloor();
        cubeScale = new Vector3(0.1f * padding.x, 0.1f, 0.1f * padding.y);
        EdgeHorizontalScale = new Vector3(0.1f * padding.x, 5.0f, 1.0f * padding.y);
        EdgeVerticalScale = new Vector3(1.0f * padding.x, 5.0f, 0.1f * padding.y);
        float[] _Padding = new float[] { padding.x, padding.y };
        generatorCS.SetBuffer(0, "_Nodes", nodeBuffer);
        generatorCS.SetVector("_PaddingResolution", new Vector4(padding.x, padding.y, ((float)xCells + 1.0f), ((float)zCells + 1.0f)));
        generatorCS.Dispatch(0, (xCells + 1) / 8 + 1, (zCells + 1) / 8 + 1, 1);




    }
    public void Generate()
    {
       
        nodes = new Node[(xCells + 1) * (zCells + 1)];

        padding = new Vector2((float)Width / (float)xCells, (float)Height / (float)zCells);
        GenerateFloor();
        cubeScale = new Vector3(0.1f * padding.x, 0.1f, 0.1f * padding.y);
        EdgeHorizontalScale = new Vector3(0.1f * padding.x,  5.0f, 1.0f * padding.y);
        EdgeVerticalScale = new Vector3(1.0f * padding.x, 5.0f, 0.1f * padding.y);
        for (int x = 0; x < xCells + 1; x++)
        {
            for (int z = 0; z < zCells + 1; z++)
            {
                Vector3 pos = new Vector3((float)x * padding.x, 0.0f, (float)z * padding.y);
                Node node = new Node();
                node.pos = pos;

                int edgeIndex = (int)EdgeIndex.Right;
                Vector3 rightPos = new Vector3(pos.x + padding.x * 0.5f, 0.0f, pos.z);
                node.edges[edgeIndex] = new Edge(rightPos, true, true);
                if (x == xCells)
                    node.edges[edgeIndex].isActive = false;


                edgeIndex = (int)EdgeIndex.Left;
                Vector3 leftpos = new Vector3(pos.x - padding.x * 0.5f, 0.0f, pos.z);
                node.edges[edgeIndex] = new Edge(leftpos, true, true);
                if (x == 0)
                    node.edges[edgeIndex].isActive = false;

                edgeIndex = (int)EdgeIndex.Top;
                Vector3 topPos = new Vector3(pos.x, 0.0f, pos.z + padding.y * 0.5f);
                node.edges[edgeIndex] = new Edge(topPos, false, true);
                if (z == zCells)
                    node.edges[edgeIndex].isActive = false;

                edgeIndex = (int)EdgeIndex.Bottom;
                Vector3 botPos = new Vector3(pos.x, 0.0f, pos.z - padding.y * 0.5f);
                node.edges[edgeIndex] = new Edge(botPos, false, true);
                if (z == 0)
                    node.edges[edgeIndex].isActive = false;

                nodes[x * (zCells + 1) + z] = node;
            }

        }
    }

    public void Destroy()
    {
        if(visualizeNodes)
        {
            if (CubeNodes.Count > 0)
            {
                foreach (GameObject cube in CubeNodes)
                {
                    Destroy(cube);
                }
                CubeNodes.Clear();
            }
        }
       

        if(visualizeEdges)
        {
            if (PlaneEdges.Count > 0)
            {
                foreach (GameObject plane in PlaneEdges)
                {
                    Destroy(plane);
                }
                PlaneEdges.Clear();
            }
        }
        if(nodeBuffer != null)
            nodeBuffer.Release();
        nodes = null;
        if (floor != null)
            Destroy(floor);
        
    }
    // Do not use this when GPU is on, Unity might crash
    public void Visualize()
    {
        if (visualizeNodes)
        {
            int cubeCounter = 0, planeCounter = 0;
            CubeNodes = new List<GameObject>();
            PlaneEdges = new List<GameObject>();
            foreach (Node node in nodes)
            {
                CubeNodes.Add(GameObject.CreatePrimitive(PrimitiveType.Cube));
                CubeNodes[cubeCounter].transform.position = node.pos;
                CubeNodes[cubeCounter].transform.localScale = cubeScale;
                cubeCounter++;
                if (visualizeEdges)
                {
                    Edge left = node.edges[(int)EdgeIndex.Left];
                    if (left.isActive )
                    {
                        PlaneEdges.Add(GameObject.CreatePrimitive(PrimitiveType.Plane));
                        PlaneEdges[planeCounter].transform.position = left.pos;
                        PlaneEdges[planeCounter].transform.localScale = new Vector3(EdgeVerticalScale.x * 0.1f, EdgeVerticalScale.y * 0.2f, EdgeVerticalScale.z * 0.1f);
                        PlaneEdges[planeCounter].GetComponent<Renderer>().material.SetColor("_Color", Color.red);
                        planeCounter++;
                    }

                    Edge right = node.edges[(int)EdgeIndex.Right];
                    if (right.isActive)
                    {
                        PlaneEdges.Add(GameObject.CreatePrimitive(PrimitiveType.Plane));
                        PlaneEdges[planeCounter].transform.position = right.pos;
                        PlaneEdges[planeCounter].transform.localScale = new Vector3(EdgeVerticalScale.x * 0.1f, EdgeVerticalScale.y * 0.2f, EdgeVerticalScale.z * 0.1f);
                        PlaneEdges[planeCounter].GetComponent<Renderer>().material.SetColor("_Color", Color.red);
                        planeCounter++;
                    }



                    Edge top = node.edges[(int)EdgeIndex.Top];
                    if (top.isActive)
                    {
                        PlaneEdges.Add(GameObject.CreatePrimitive(PrimitiveType.Plane));
                        PlaneEdges[planeCounter].transform.position = top.pos;
                        PlaneEdges[planeCounter].transform.localScale = new Vector3(EdgeHorizontalScale.x * 0.1f, EdgeHorizontalScale.y * 0.2f, EdgeHorizontalScale.z * 0.1f);
                        PlaneEdges[planeCounter].GetComponent<Renderer>().material.SetColor("_Color", Color.red);
                        planeCounter++;
                    }

                    Edge bottom = node.edges[(int)EdgeIndex.Bottom];
                    if (bottom.isActive)
                    {
                        PlaneEdges.Add(GameObject.CreatePrimitive(PrimitiveType.Plane));
                        PlaneEdges[planeCounter].transform.position = bottom.pos;
                        PlaneEdges[planeCounter].transform.localScale = new Vector3(EdgeHorizontalScale.x * 0.1f, EdgeHorizontalScale.y * 0.2f, EdgeHorizontalScale.z * 0.1f);
                        PlaneEdges[planeCounter].GetComponent<Renderer>().material.SetColor("_Color", Color.red);
                        planeCounter++;
                    }

                }
            }
        }
    }

    // We know the structure of the nodes list: nodes are inserted first by the z dimension.
    Vector2Int OneDimToTwoDims(int index)
    {
        int x = index / (zCells + 1);
        int z = index - (zCells + 1) * x;
        return new Vector2Int(x, z);
    }

    // Return the number of nodes of a tile and per dimension given its resolution and position of first node
    public Vector3Int GetNumberOfNodes(Vector4 posResolution, Vector2Int miniBlockSize, bool custom = false)
    {
        if (posResolution.x < 0 || posResolution.x > xCells)
            return Vector3Int.zero;
        if (posResolution.y < 0 || posResolution.y > zCells)
            return Vector3Int.zero;
       
        int xNodes = (int)((posResolution.x + posResolution.z - 1) < xCells + 1 ? posResolution.z : (xCells + 1 - posResolution.x));
        int zNodes = (int)((posResolution.y + posResolution.w - 1) < zCells + 1 ? posResolution.w : (zCells + 1 - posResolution.y));

       
        if(miniBlockSize.x != posResolution.z && custom)
        {
            xNodes = (xNodes % miniBlockSize.x) == 0 ? xNodes / miniBlockSize.x : xNodes / miniBlockSize.x + 1;
            zNodes = (zNodes % miniBlockSize.y) == 0 ? zNodes / miniBlockSize.y : zNodes / miniBlockSize.y + 1;

        }
        return new Vector3Int(xNodes, zNodes, zNodes * xNodes);

    }
    public int TwoDimsToOneDim(Vector2Int index)
    {
        if (index.x >= xCells + 1 || index.y >= zCells + 1)
            return -1;
        return index.x * (zCells + 1) + index.y;
    }
    
    public int GetNeighbor(int index, EdgeDirection dir, int cellResolution = 0, bool custom = false)
    {
        int cells = (custom ? cellResolution : zCells + 1);
        switch (dir)
        { 
            case EdgeDirection.Top:
                return index + 1;
            case EdgeDirection.Bottom:
                return index - 1;
            case EdgeDirection.Left:
                return index - (cells);
            case EdgeDirection.Right:
                return index + (cells);
        }

        return -1;
    }

    public int GetOppositeEdgeIndex(EdgeIndex index)
    {
        switch(index)
        {
            case EdgeIndex.Bottom:
                return (int)EdgeIndex.Top;
            case EdgeIndex.Top:
                return (int)EdgeIndex.Bottom;
            case EdgeIndex.Left:
                return (int)EdgeIndex.Right;
            case EdgeIndex.Right:
                return (int)EdgeIndex.Left;
        }
        return 0;
    }
   
    
}

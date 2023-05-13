using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Based on the Primm's algorithm
public class MSTSolver : MonoBehaviour
{
    public GridGraph graph;
    public GridGraph mstGraph;

    public int miniXCell = 1;
    public int miniZCell = 1;
    public ComputeShader mstSolverShader;


    public class LightEdge
    {
        public bool isExplored;
        public bool isActive;

       public LightEdge(bool isExplored = false, bool isActive = false)
        {
            this.isActive = isActive;
            this.isExplored = isExplored;
        }
    };

    

    public class LightNode
    {
       
        public LightEdge[] edges = new LightEdge[4];
        public LightNode()
        {
            for(int ei = 0; ei < 4; ei++)
            {
                edges[ei] = new LightEdge();
            }
        }
    };

    public class MSTEdge
    {
        public int parentNodeIndex; // parent node that originated from
        public EdgeDirection dir; // direction relative to the parent node;

        public MSTEdge(int parentNode, EdgeDirection dir)
        {
            this.parentNodeIndex = parentNode;
            this.dir = dir;
        }
    }
    // Single Threaded embarrassing parallel algorithm that runs on the cpu
    // for debug and testing purposes before transfering to the gpu.
    // The concept of the algorithm is to divide the graph into smaller graphs
    // and calculate the MST of each graph. In the next iteration the previous mini-graphs
    // are considered nodes. New mini-graphs are formed that have from the previous iteration the MST-solved
    // graphs as nodes. This process continues until the new graph contains all original nodes.
    public void ParallelGenerate()
    {
        

        graph.ParallelGenerate();
        Vector4 seed = new Vector4(Random.Range(0, 100000), Random.Range(0, 100000), Random.Range(0, 100000), 0);
        mstSolverShader.SetVector("_Seed", seed);
        mstSolverShader.SetVector("_MiniResolution", new Vector4((float)miniXCell + 1.0f, (float)miniZCell + 1.0f, (float)graph.xCells + 1.0f, (float)graph.zCells + 1.0f));
        mstSolverShader.SetBuffer(0, "_Nodes", graph.getNodeBuffer());

        bool optimalSizeFound = false;
        int miniNodesX = miniXCell + 1;
        int miniNodesZ = miniZCell + 1;
        int iterations = 1;
        Vector2Int optimalSize = new Vector2Int(miniNodesX, miniNodesZ);
        while (!optimalSizeFound)
        {
            if (optimalSize.x >= graph.xCells + 1 && optimalSize.y >= graph.zCells + 1)
            {
                optimalSizeFound = true;
            }
            else
            {
                optimalSize.x *= miniNodesX;
                optimalSize.y *= miniNodesZ;
                iterations++;   
            }
        }


        mstSolverShader.Dispatch(0, (graph.xCells + 1) / (8* miniNodesX) + 1 , (graph.zCells + 1) /(8* miniNodesZ)  + 1, iterations);
        NodeCB[] nodesCB = new NodeCB[(graph.xCells + 1) * (graph.zCells + 1)];
        




        // Old parallelized algorithm on the CPU.
        //for (int i = 0; i < graph.nodes.Count; i++)
        //{
        //    for (int ei = 0; ei < 4; ei++)
        //    {
        //        graph.nodes[i].edges[ei].isActive = false;
        //    }
        //}


        //for (int iter = 0; iter < iterations; iter++)
        //{

        //    int xScale = (int)Mathf.Pow(miniNodesX, iter + 1);
        //    int xScalePrev = (int)Mathf.Pow(miniNodesX, iter);
        //    int zScalePrev = (int)Mathf.Pow(miniNodesZ, iter);
        //    int zScale = (int)Mathf.Pow(miniNodesZ, iter + 1);

        //    for (int x = 0; x < (graph.xCells + 1); x += xScale)
        //    {

        //        for (int z = 0; z < (graph.zCells + 1); z += zScale)
        //        {
        //            Vector3Int nNodes;
        //            nNodes = graph.GetNumberOfNodes(new Vector4(x, z, xScale, zScale), new Vector2Int(xScalePrev, zScalePrev), true);
        //            if (nNodes.z == 0)
        //                continue;

        //            LightNode[] nodes = new LightNode[nNodes.z];
        //            bool[] isInMST = new bool[nNodes.z];
        //            for (int n = 0; n < nNodes.z; n++)
        //            {
        //                nodes[n] = new LightNode();
        //            }
        //            List<MSTEdge> mstEdges = new List<MSTEdge>();
        //            int xIndex = Random.Range(0, nNodes.x);
        //            int yIndex = Random.Range(0, nNodes.y);
        //            int index = xIndex * nNodes.y + yIndex;
        //            isInMST[index] = true;
        //            LightNode node = nodes[index];

        //            int nEdges = 0;
        //            while (nEdges < nNodes.z - 1)
        //            {
        //                int xOffset = index / nNodes.y;
        //                int zOffset = index - xOffset * nNodes.y;
        //                for (int ei = 0; ei < 4; ei++)
        //                {
        //                    if (!node.edges[ei].isActive && !node.edges[ei].isExplored)
        //                    {
        //                        int neighBorIndex = graph.GetNeighbor(index, (EdgeDirection)ei, nNodes.y, true);
        //                        check if the is no neighbor, e.g.node is boundary node
        //                        if (neighBorIndex < 0 || neighBorIndex > nNodes.z - 1 || (ei == 0 && zOffset == nNodes.y - 1) || (ei == 1 && zOffset == 0))
        //                            continue;
        //                        if (!isInMST[neighBorIndex])
        //                        {
        //                            mstEdges.Add(new MSTEdge(index, (EdgeDirection)ei));
        //                            nodes[index].edges[ei].isExplored = true;
        //                            nodes[neighBorIndex].edges[graph.GetOppositeEdgeIndex((EdgeIndex)ei)].isExplored = true;
        //                        }
        //                    }
        //                }

        //                int edgeIndex;
        //                MSTEdge edge;
        //                bool notFoundActive = true;
        //                edgeIndex = Random.Range(0, mstEdges.Count);
        //                edge = mstEdges[edgeIndex];
        //                while (notFoundActive)
        //                {

        //                    if (!isInMST[graph.GetNeighbor(edge.parentNodeIndex, edge.dir, nNodes.y, true)])
        //                        notFoundActive = false;
        //                    else
        //                    {
        //                        mstEdges.RemoveAt(edgeIndex);
        //                        edgeIndex = Random.Range(0, mstEdges.Count);
        //                        edge = mstEdges[edgeIndex];
        //                    }
        //                }

        //                nEdges++;
        //                nodes[edge.parentNodeIndex].edges[(int)edge.dir].isActive = true;

        //                //Switch to x,z in block space
        //                xOffset = edge.parentNodeIndex / nNodes.y;
        //                zOffset = edge.parentNodeIndex - xOffset * nNodes.y;

        //                int xOrigin = x + xOffset * (xScalePrev);
        //                int zOrigin = z + zOffset * zScalePrev;

        //                int realIndex = 0;
        //                Vector3Int nRealNodes = graph.GetNumberOfNodes(new Vector4(xOrigin, zOrigin, xScalePrev, zScalePrev), Vector2Int.zero);
        //                if ((int)edge.dir == 0)
        //                {
        //                    int randomNode = Random.Range(0, nRealNodes.x);
        //                    realIndex = graph.TwoDimsToOneDim(new Vector2Int(xOrigin + randomNode, zOrigin + nRealNodes.y - 1));
        //                }
        //                else if ((int)edge.dir == 1)
        //                {
        //                    int randomNode = Random.Range(0, nRealNodes.x);
        //                    realIndex = graph.TwoDimsToOneDim(new Vector2Int(xOrigin + randomNode, zOrigin));
        //                }
        //                else if ((int)edge.dir == 2)
        //                {
        //                    int randomNode = Random.Range(0, nRealNodes.y);
        //                    realIndex = graph.TwoDimsToOneDim(new Vector2Int(xOrigin, zOrigin + randomNode));
        //                }
        //                else if ((int)edge.dir == 3)
        //                {
        //                    int randomNode = Random.Range(0, nRealNodes.y);
        //                    realIndex = graph.TwoDimsToOneDim(new Vector2Int(xOrigin + nRealNodes.x - 1, zOrigin + randomNode));
        //                }


        //                graph.nodes[realIndex].edges[(int)edge.dir].isActive = true;
        //                int neighbor = graph.GetNeighbor(realIndex, edge.dir);
        //                graph.nodes[neighbor].edges[graph.GetOppositeEdgeIndex((EdgeIndex)edge.dir)].isActive = true;


        //                int neighborIndex = graph.GetNeighbor(edge.parentNodeIndex, edge.dir, nNodes.y, true);
        //                nodes[neighborIndex].edges[graph.GetOppositeEdgeIndex((EdgeIndex)edge.dir)].isActive = true;
        //                node = nodes[neighborIndex];
        //                index = neighborIndex;
        //                isInMST[neighborIndex] = true;
        //                mstEdges.RemoveAt(edgeIndex);

        //            }

        //        }
        //    }
        //}

       // graph.Visualize();
    }
    public void Generate()
    {

        // Initialize nodes' and edges' positions status based on maze properties (e.g. width, height, #cells)
        graph.Generate();

        // helper array to decide in O(1) whether a node is already in MST
        // to avoid using search algorithm to search through the MST nodes.
        bool[] isInMST = new bool[graph.nodes.Length];

       
        List<int> mstNodeIndices = new List<int>();  // Keep track of the nodes' indices that are in the MST
        List<MSTEdge> mstEdges = new List<MSTEdge>();  // Keep track of the edge that can lead to new explored nodes.

        //Pick random first vertex
        int nNodes = graph.nodes.Length;
        int indexFirstNode = Random.Range(0, nNodes);

        int nodeIndex = indexFirstNode;
        isInMST[nodeIndex] = true;
        mstNodeIndices.Add(nodeIndex);
        Node node = graph.nodes[nodeIndex];

        // We loop until "mstNodeIndices.Count < nNodes + 1" and not "mstNodeIndices.Count < nNodes",
        // because final node needs to extend its edges to remove any remaining edges that are not in MST.
        while (mstNodeIndices.Count < nNodes + 1)
        {
            // Extend node and check if edges connect to a node that is already in MST
            for (int ei = 0; ei < 4; ei++)
            {
                //if not active, either is already explored/deactivated or it should not exist(e.g. on boundaries of the grid)
                if (node.edges[ei].isActive && !node.edges[ei].isExplored)
                {
                    int neighborIndex = graph.GetNeighbor(nodeIndex, (EdgeDirection)ei);
                    if (isInMST[neighborIndex])
                    {
                        // deactivate the edge from both the node's and its neighbor's side.
                        node.edges[ei].isActive = false;
                        graph.nodes[neighborIndex].edges[graph.GetOppositeEdgeIndex((EdgeIndex)ei)].isActive = false;
                        
                    }
                    else
                    {
                        mstEdges.Add(new MSTEdge(nodeIndex, (EdgeDirection)ei));
                    }

                }

            }


            // We are at the final node, terminate.
            if (mstNodeIndices.Count == nNodes)
                break;

            // Pop random edge from mstEdges
            int edgeIndex;
            MSTEdge edge;
            // loop until we find an edge that is active. MSTEdges can contain edges that are deactivated, because of reaching a neighbor in previous iterations.
            // We want to remove those from the sampling pool, they should be removed from MSTEdges. Since it is a dynamic list there is no way of knowing the index of every edge
            // when a neighboring node deactivates it. It would be too much work and not efficient calculating and storing into a temp buffer the index of every
            // edge in the dynamic list; 'MSTEdges'.
            bool notFoundActive = true;
            edgeIndex = Random.Range(0, mstEdges.Count);
            edge = mstEdges[edgeIndex];
            while (notFoundActive)
            {
                if (graph.nodes[edge.parentNodeIndex].edges[(int)edge.dir].isActive)
                    notFoundActive = false;
                else
                {
                    mstEdges.RemoveAt(edgeIndex);
                    // Try again
                    edgeIndex = Random.Range(0, mstEdges.Count);
                    edge = mstEdges[edgeIndex];
                }
                   
            }
            

            // Set the explored flag to true
            graph.nodes[edge.parentNodeIndex].edges[(int)edge.dir].isExplored = true;
            nodeIndex = graph.GetNeighbor(edge.parentNodeIndex, edge.dir);
            node = graph.nodes[nodeIndex];
            // Need to do the same for the corresponding edge from the neighbor's side.
            // If we did not, when expanding the neighbor it would falsely deactivate it.
            node.edges[graph.GetOppositeEdgeIndex((EdgeIndex)edge.dir)].isExplored = true;

            mstNodeIndices.Add(nodeIndex);
            isInMST[nodeIndex] = true;
            mstEdges.RemoveAt(edgeIndex);
        }

       
        graph.Visualize();
    }

   
}

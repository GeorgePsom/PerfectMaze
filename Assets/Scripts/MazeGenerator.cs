using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System;

public class MazeGenerator : MonoBehaviour
{

    public MSTSolver solver;
    public Mesh wallMesh;
    public Material wallMaterial, wallGPUMaterial;
    public ComputeShader wallGenerationCS;
    public bool useGPU;
    bool shouldDestroy = false;


    private ComputeBuffer wallCB, argsBuffer;
    private int entrance, exit;
    private Vector4[] wallsArray;
    private GameObject[] wallColliders;

    public struct Wall
    {
        public Vector3 pos;
        public bool isVertical;

        public Wall(Vector3 pos, bool isVertical)
        {
            this.pos = pos;
            this.isVertical = isVertical;
        }
    }

    public List<Wall> walls;
    List<Matrix4x4> matrices;
   
    public float getMazeEntrance()  { return entrance * solver.graph.padding.x; }    

    // Get exit in world space
    public  Vector3 getMazeExit()
    {
        return new Vector3(exit * solver.graph.padding.x, 1.0f, solver.graph.Height + 0.5f * solver.graph.padding.y);
    }
    public void ParallelGenerate()
    {
        shouldDestroy = true;
        matrices = new List<Matrix4x4>();
        solver.ParallelGenerate();
        // Computebuffer is append type; we do not know the number of walls in advance.
        wallCB = new ComputeBuffer(4 * (solver.graph.xCells + 2) * (solver.graph.zCells + 2), 4 * sizeof(float), ComputeBufferType.Append);
        wallCB.SetCounterValue(0);
        entrance = (int)UnityEngine.Random.Range(0, solver.graph.xCells);
        exit = (int)UnityEngine.Random.Range(0, solver.graph.xCells);
        wallGenerationCS.SetInt("_Entrance", entrance);
        wallGenerationCS.SetInt("_Exit", exit);
        wallGenerationCS.SetBuffer(0, "_Nodes", solver.graph.getNodeBuffer());
        wallGenerationCS.SetBuffer(0, "_Walls", wallCB);
        wallGenerationCS.SetVector("_ResolutionPadding", new Vector4(solver.graph.xCells, solver.graph.zCells, solver.graph.padding.x, solver.graph.padding.y));
        wallGenerationCS.Dispatch(0, (solver.graph.xCells + 2) / 8 + 1, (solver.graph.zCells + 2) / 8 + 1, 1);
        ComputeBuffer countBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.IndirectArguments);
        ComputeBuffer.CopyCount(wallCB, countBuffer,0);

        int[] counter = new int[1] { 0 };
        countBuffer.GetData(counter);
        int count = counter[0];
        // the cpu array of walls will be used for generating the colliders.
        wallsArray = new Vector4[count];
        wallCB.GetData(wallsArray);

        uint[] args = new uint[5] { 0, 0, 0, 0, 0 }; 
        args[0] = (uint)wallMesh.GetIndexCount(0);
        args[1] = (uint)count;
        args[2] = (uint)wallMesh.GetIndexStart(0);
        args[3] = (uint)wallMesh.GetBaseVertex(0);
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);

        wallGPUMaterial.SetBuffer("_Offsets", wallCB);
        wallGPUMaterial.SetVector("_HorizontalScale", solver.graph.EdgeHorizontalScale);
        wallGPUMaterial.SetVector("_VerticalScale", solver.graph.EdgeVerticalScale);
        
        countBuffer.Release();
        
    }

    // When on play mode, we generate the walls also as gameobjects but disable the mesh renderer. 
    // I wanted to have Unity's built-in collision detection so that I do not have to implement myself.
    public void GenerateMeshColliders(bool useGPU)
    {
        if(useGPU)
        {
            wallColliders = new GameObject[wallsArray.Length];
            for(int i =0; i < wallsArray.Length; i++)
            {
                GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Vector4 walli = wallsArray[i];
                wall.transform.position = new Vector3(walli.x, walli.y, walli.z);
                wall.transform.localScale = walli.w > 0 ? solver.graph.EdgeHorizontalScale : solver.graph.EdgeVerticalScale;
                wall.GetComponent<MeshRenderer>().enabled = false;
                wall.layer = LayerMask.NameToLayer("Obstacle");
                wallColliders[i] = wall;
            }
        }

        else
        {
            wallColliders = new GameObject[walls.Count];
            for(int i =0; i  < walls.Count; i++)
            {
                GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.transform.position = walls[i].pos;
                wall.transform.localScale = walls[i].isVertical ? solver.graph.EdgeHorizontalScale : solver.graph.EdgeVerticalScale;
                wall.GetComponent<MeshRenderer>().enabled = false;
                wall.layer = LayerMask.NameToLayer("Obstacle");
                wallColliders[i] = wall;
            }
        }
    }
    public void Generate()
    {
        shouldDestroy = true;
        solver.Generate();
        matrices = new List<Matrix4x4>();

        int width = solver.graph.xCells;
        int height = solver.graph.zCells;
        Vector2 padding = solver.graph.padding;
        walls = new List<Wall>();
        entrance = (int)UnityEngine.Random.Range(0, width);
        exit = (int)UnityEngine.Random.Range(0, width);
        // the maze has 2 nodes more in both dimensions than the MST graph.
        for(int x = 0; x < width + 2; x++)
        {
            for (int z = 0; z < height + 2; z++)
            {
                if (x < width + 1)
                {
                    Vector2Int xzDim = new Vector2Int(x, z == height + 1 ? z - 1 : z);
                    Node horizonalNode = solver.graph.nodes[solver.graph.TwoDimsToOneDim(xzDim)];
                    // we always check the right edge unless we are at the last node then we take the left.
                    int edgeIndex = (xzDim.x < width) ? 3 : 2; 
                    Edge edge = horizonalNode.edges[edgeIndex];

                if (!(horizonalNode.edges[1].isActive && (z > 0 && z < height + 1)) && (!(x == entrance && z == 0)) && (!(x == exit && z == height + 1)))

                {
                    Vector3 pos = new Vector3(edge.pos.x + (x == width ? padding.x * 0.5f : -padding.x * 0.5f),
                        1.0f,
                        edge.pos.z + (z == height + 1 ? padding.y * 0.5f : -padding.y * 0.5f));
                        walls.Add(new Wall(pos, false));
                    }
                    
                }

                if (z < height + 1)
                {
                    Vector2Int xzDim = new Vector2Int(x == width + 1 ? x - 1 : x, z);
                    Node verticalNode = solver.graph.nodes[solver.graph.TwoDimsToOneDim(xzDim)];
                    int edgeIndex = (xzDim.y < height) ? 0 : 1;
                    Edge edge = verticalNode.edges[edgeIndex];

                    if (!(verticalNode.edges[2].isActive && (x > 0 && x < width + 1)))
                    {
                        Vector3 pos = new Vector3(edge.pos.x + (x == width + 1 ? padding.x * 0.5f : -padding.x * 0.5f),
                                1.0f,
                                edge.pos.z + (z == height ? padding.y * 0.5f : -padding.y * 0.5f));
                        walls.Add(new Wall(pos, true));
                    }

                }
            }
        }

        foreach(Wall w in walls)
        {
            matrices.Add(Matrix4x4.TRS(w.pos, Quaternion.identity, (w.isVertical ? solver.graph.EdgeHorizontalScale : solver.graph.EdgeVerticalScale))); 
            
        }
        float maxpadding = Mathf.Max(solver.graph.padding.x, solver.graph.padding.y) * 0.2f;
        wallMaterial.mainTextureScale = new Vector2(maxpadding * 2.0f, maxpadding);
    }

    public void Destroy()
    {
        if(shouldDestroy)
        {
            if(matrices != null)
                matrices.Clear();

            if(walls != null)
                walls.Clear();


            if (wallsArray != null)
                Array.Clear(wallsArray, 0, wallsArray.Length);

            if(wallCB != null)
                wallCB.Release();

            if(wallColliders != null)
            {
                foreach(GameObject wallCollider in wallColliders)
                {
                    Destroy(wallCollider);
                }
                Array.Clear(wallColliders, 0, wallColliders.Length);
            }

            if (argsBuffer != null)
                argsBuffer.Release();

            solver.graph.Destroy();
        }
            
    }
    
    void Update()
    {
        // to ensure that when the app is running it renders only if at least once the generate button has been pressed
        if (shouldDestroy)
        {
            if(useGPU)
            {
                Bounds bounds = new Bounds(new Vector3(solver.graph.Width / 2, -2, solver.graph.Height / 2), new Vector3(solver.graph.Width, 10, solver.graph.Height));
                Graphics.DrawMeshInstancedIndirect(wallMesh, 0, wallGPUMaterial, bounds, argsBuffer);
            }else
            {
                // maximum number of instances is 1023. We need to make separate drawcalls if instances exceed 1023.
                int iter = matrices.Count / 1023;
                for (int i = 0; i < iter + 1; i++)
                {
                    int count = 1023;
                    int end = (i + 1) * 1023;
                    if (i == iter)
                    {
                        count = matrices.Count - (iter) * 1023;
                        end = matrices.Count;
                    }
                    List<Matrix4x4> matrixList = matrices.GetRange(i * 1023, count);
                    Graphics.DrawMeshInstanced(wallMesh, 0, wallMaterial, matrixList.ToArray(), count, null, ShadowCastingMode.On, true);

                }
            }

        }

    }
}

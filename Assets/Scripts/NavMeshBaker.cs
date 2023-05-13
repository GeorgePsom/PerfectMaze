using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;
public class NavMeshBaker : MonoBehaviour
{

    private NavMeshSurface surface;
    // Start is called before the first frame update
    void Start()
    {
        surface = GetComponent<NavMeshSurface>();
    }

    // Update is called once per frame
    public void Bake()
    {
        surface.BuildNavMesh();
    }
}

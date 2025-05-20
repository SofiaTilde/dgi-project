using UnityEngine;
using Classes;
using System.Collections.Generic;
using Unity.VisualScripting;

public class LeafTester : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SortedDictionary<string, Internode> internodes = new();
        SortedDictionary<string, Petiole> petioles = new();
        SortedDictionary<string, Leaf> leaves = new();

        Leaf leaf = new (0, 0, "L1", 1, 0, 0, ref internodes, ref petioles, ref leaves);

        leaf.Angle = 0;
        leaf.Rotation = 0;
        leaf.Size = (1, 1);
        leaf.Holes = (0, 0);

        leaves.Add(leaf.ID, leaf);

        List<Vector3> vertices = new();
        List<int> triangles = new();

        GetComponent<MeshFabricator>().GenerateLeafMesh(leaf, new Vector3(0, 0, 0), vertices, triangles);

        Mesh mesh = GetComponent<MeshFilter>().mesh;
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

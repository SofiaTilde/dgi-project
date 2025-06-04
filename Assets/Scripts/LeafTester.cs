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
        leaf.Width = 1f;
        leaf.Height = 1f;
        leaf.ThicknessFenestrations = 0.7f;
        leaf.LengthFenestrations = 0.6f;
        float[] holes = {0f, 0f, 0.8f, 0.8f, 0.8f, 0.8f, 0f, 0.8f, 0f, 0f };
        leaf.Holes = holes;

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

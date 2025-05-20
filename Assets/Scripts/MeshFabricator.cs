using UnityEngine;
using Classes;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Reflection;
using System.CodeDom.Compiler;
using System.Xml.Serialization;
using UnityEditor;

public class MeshFabricator : MonoBehaviour
{
    Vector3[] myVertices = new Vector3[5];
    int[] myTriangles = new int[18];

    struct Matrix
    {
        public Vector3 x;
        public Vector3 y;
        public Vector3 z;
    }

    void Start()
    {
        /*
        SortedDictionary<string, Internode> internodes = new SortedDictionary<string, Internode>();
        SortedDictionary<string, Petiole> petioles = new SortedDictionary<string, Petiole>();
        SortedDictionary<string, Leaf> leaves = new SortedDictionary<string, Leaf>();
        Internode i1 = new(1, 1, "I1", 1, ref internodes, ref petioles, ref leaves);
        Internode i2 = new(1, 1, "I2", 1, ref internodes, ref petioles, ref leaves);
        Petiole   p1 = new(1, 1, "P1", 1, ref internodes, ref petioles, ref leaves);
        Leaf      l1 = new(1, 1, "L1", 1, ref internodes, ref petioles, ref leaves);

        i1.Thickness = 0.2;
        i1.Length = 0.5;
        i1.Angle = 45;
        i1.Rotation = 45;

        i2.Thickness = 0.2;
        i2.Length = 0.5;
        i2.Angle = 12;
        i2.Rotation = -30;

        p1.ThicknessStart = 0.20;
        p1.ThicknessEnd = 0.10;
        p1.Length = 2;
        p1.Angle = i1.Angle + 10;
        p1.Rotation = i2.Rotation;

        List<Vector3> vertices = new();
        List<int> triangles = new();

        Vector3 next;
        next = GenerateInternodeMesh(i1, new Vector3(0, 0, 0), vertices, triangles);
        GenerateInternodeMesh(i2, next, vertices, triangles);

        Vector3 pos_leaf = GeneratePetioleMesh(p1, next, vertices, triangles);
        GenerateLeafMesh(l1, p1.Angle, p1.Rotation, pos_leaf, vertices, triangles);

        Mesh mesh = GetComponent<MeshFilter>().mesh;
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        */
    }

    void Update()
    {
        
    }

    //Generates the mesh of an entire Monstera Deliciosa specimen.
    public void GenerateSpecimenMesh(SortedDictionary<string, Internode> internodes, SortedDictionary<string, Petiole> petioles, SortedDictionary<string, Leaf> leaves)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        List<Vector3> internode_ends = new List<Vector3> { new Vector3(0, 0, 0) };
        List<Vector3> petiole_ends = new ();

        int petiole_count = 1;
        int leaf_count = 0;

        foreach (KeyValuePair<string, Internode> internode in internodes)
        {
            internode_ends.Add(GenerateInternodeMesh(internode.Value, internode_ends.Last(), vertices, triangles));
        }

        foreach (KeyValuePair<string, Petiole> petiole in petioles)
        {
            petiole_ends.Add(GeneratePetioleMesh(petiole.Value, internode_ends[petiole_count], vertices, triangles));
            petiole_count++;
        }

        foreach (KeyValuePair<string, Leaf> leaf in leaves)
        {
            GenerateLeafMesh(leaf.Value, petiole_ends[leaf_count], vertices, triangles);
            leaf_count++;
        }

        Mesh mesh = GetComponent<MeshFilter>().mesh;
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
    }

    //Generates a mesh for a given internode at a given position and adds it to a given mesh.
    Vector3 GenerateInternodeMesh(Internode internode, Vector3 positions_start, List<Vector3> vertices, List<int> triangles)
    {
        float angle = internode.Angle * Mathf.Deg2Rad;
        float rotation = internode.Rotation * Mathf.Deg2Rad;

        Matrix rotated_matrix = Rotate(angle, rotation);

        List<Vector3> positions = new();

        positions.Add(positions_start + (rotated_matrix.y * -internode.Thickness / 4));
        positions.Add(positions_start + (rotated_matrix.y * (internode.Length + internode.Thickness / 4)));
        positions.AddRange(OctagonPositions(positions_start, rotated_matrix, internode.Thickness / 2));
        positions.AddRange(OctagonPositions(positions_start + (rotated_matrix.y * internode.Length), rotated_matrix, internode.Thickness / 2));

        AddInternodeMeshComponents(positions.ToArray(), vertices, triangles);

        return positions_start + (rotated_matrix.y * internode.Length);
    }

    void AddInternodeMeshComponents(Vector3[] positions, List<Vector3> vertices, List<int> triangles)
    {
        vertices.Add(positions[0]);
        vertices.Add(positions[1]);

        int vertice_index = vertices.Count;

        for (int t = 0; t < 8; t++)
        {
            vertices.Add(positions[t + 2]);
            vertices.Add(positions[t + 10]);

            //Bottom cap
            triangles.Add(vertice_index + (t * 2 + 0) % 16);
            triangles.Add(vertice_index + (t * 2 + 2) % 16);
            triangles.Add(vertice_index - 2);

            //Top cap
            triangles.Add(vertice_index + (t * 2 + 3) % 16);
            triangles.Add(vertice_index + (t * 2 + 1) % 16);
            triangles.Add(vertice_index - 1);

            //Bottom side
            triangles.Add(vertice_index + (t * 2 + 0) % 16);
            triangles.Add(vertice_index + (t * 2 + 1) % 16);
            triangles.Add(vertice_index + (t * 2 + 2) % 16);

            //Top side
            triangles.Add(vertice_index + (t * 2 + 2) % 16);
            triangles.Add(vertice_index + (t * 2 + 1) % 16);
            triangles.Add(vertice_index + (t * 2 + 3) % 16);
        }
    }

    Vector3 GeneratePetioleMesh(Petiole petiole, Vector3 positions_start, List<Vector3> vertices, List<int> triangles)
    {
        float angle = petiole.Angle * Mathf.Deg2Rad;
        float rotation = petiole.Rotation * Mathf.Deg2Rad;

        Matrix rotated_matrix = Rotate(angle, rotation);

        List<Vector3> positions = new();

        positions.AddRange(BasePositions(positions_start, rotated_matrix, petiole.ThicknessStart / 2));
        positions.AddRange(MiddlePositions(positions_start + rotated_matrix.x * petiole.Length / 3, rotated_matrix, (petiole.ThicknessStart + petiole.ThicknessEnd) / 4));
        positions.AddRange(EndPositions(positions_start + rotated_matrix.x * petiole.Length, rotated_matrix, petiole.ThicknessEnd / 2));

        AddPetioleMeshComponents(positions.ToArray(), vertices, triangles);

        return positions_start + (rotated_matrix.x * petiole.Length);
    }

    void AddPetioleMeshComponents(Vector3[] positions, List<Vector3> vertices, List<int> triangles)
    {
        int vertice_index = vertices.Count;

        for (int i = 0; i < 23; i++)
        {
            vertices.Add(positions[i]);
        }

        for (int t = 0; t < 4; t++)
        {
            //Bowl
            triangles.Add(vertice_index + t * 2 + 4);
            triangles.Add(vertice_index + t * 2 + 2);
            triangles.Add(vertice_index + 0);
            
            //Bottom cap
            triangles.Add(vertice_index + t * 2 + 3);
            triangles.Add(vertice_index + t * 2 + 5);
            triangles.Add(vertice_index + 1);
            
            //Top side
            triangles.Add(vertice_index + t * 2 + 3);
            triangles.Add(vertice_index + t * 2 + 2);
            triangles.Add(vertice_index + t * 2 + 4);

            //Bottom side
            triangles.Add(vertice_index + t * 2 + 3);
            triangles.Add(vertice_index + t * 2 + 4);
            triangles.Add(vertice_index + t * 2 + 5);
        }

        int[] verts_middle = {
            0, 2, 12, 13,
            2, 3, 13, 14,
            3, 1, 14, 15,
            1, 11, 16, 17,
            11, 10, 17, 13,
            10, 0, 13, 12,
        };

        for (int i = 0; i < 6; i++)
        {
            triangles.Add(vertice_index + verts_middle[i * 4 + 0]);
            triangles.Add(vertice_index + verts_middle[i * 4 + 1]);
            triangles.Add(vertice_index + verts_middle[i * 4 + 2]);

            triangles.Add(vertice_index + verts_middle[i * 4 + 1]);
            triangles.Add(vertice_index + verts_middle[i * 4 + 3]);
            triangles.Add(vertice_index + verts_middle[i * 4 + 2]);
        }

        triangles.Add(vertice_index + 1);
        triangles.Add(vertice_index + 16);
        triangles.Add(vertice_index + 15);

        for (int i = 0; i < 5; i++)
        {
            triangles.Add(vertice_index + 13 + ((i + 0) % 5));
            triangles.Add(vertice_index + 13 + ((i + 1) % 5));
            triangles.Add(vertice_index + 18 + ((i + 0) % 5));

            triangles.Add(vertice_index + 13 + ((i + 1) % 5));
            triangles.Add(vertice_index + 18 + ((i + 1) % 5));
            triangles.Add(vertice_index + 18 + ((i + 0) % 5));
        }

        triangles.Add(vertice_index + 18);
        triangles.Add(vertice_index + 19);
        triangles.Add(vertice_index + 20);

        triangles.Add(vertice_index + 18);
        triangles.Add(vertice_index + 20);
        triangles.Add(vertice_index + 21);

        triangles.Add(vertice_index + 18);
        triangles.Add(vertice_index + 21);
        triangles.Add(vertice_index + 22);
    }

    public void GenerateLeafMesh(Leaf leaf, Vector3 positions_start, List<Vector3> vertices, List<int> triangles)
    {
        float angle = leaf.Angle * Mathf.Deg2Rad;
        float rotation = leaf.Rotation * Mathf.Deg2Rad;

        float width = 0.2f;
        float height = 0.2f;

        Matrix rotated_matrix = Rotate(angle, rotation);

        List<Vector3> positions = new();

        positions.AddRange(LeafPositions(positions_start + rotated_matrix.x * 0.01f, rotated_matrix, width / 2, height));
        positions.AddRange(LeafPositions(positions_start + rotated_matrix.x * 0.005f, rotated_matrix, width / 2, height));

        AddLeafMeshComponents(positions.ToArray(), vertices, triangles);
    }

    void AddLeafBasicMeshComponents(Vector3[] positions, List<Vector3> vertices, List<int> triangles)
    {
        int vertice_index = vertices.Count;

        for (int i = 0; i < 13; i++)
        {
            vertices.Add(positions[i]);
        }

        for (int i = 0; i < 10; i++)
        {
            triangles.Add(vertice_index + 0);
            triangles.Add(vertice_index + i + 1);
            triangles.Add(vertice_index + i + 2);

            triangles.Add(vertice_index + 12);
            triangles.Add(vertice_index + i + 2);
            triangles.Add(vertice_index + i + 1);
        }
    }

    void AddLeafMeshComponents(Vector3[] positions, List<Vector3> vertices, List<int> triangles)
    {
        int vertice_index = vertices.Count;

        for (int i = 0; i < 23; i++)
        {
            vertices.Add(positions[i]);
        }

        triangles.Add(vertice_index + 0);
        triangles.Add(vertice_index + 11);
        triangles.Add(vertice_index + 22);

        for (int i = 0; i < 10; i++)
        {
            triangles.Add(vertice_index + 0);
            triangles.Add(vertice_index + i + 1);
            triangles.Add(vertice_index + i + 2);

            triangles.Add(vertice_index + 22);
            triangles.Add(vertice_index + i + 11);
            triangles.Add(vertice_index + i + 12);
        }

        vertice_index = vertices.Count;

        for (int i = 0; i < 23; i++)
        {
            vertices.Add(positions[i + 23]);
        }

        triangles.Add(vertice_index + 0);
        triangles.Add(vertice_index + 22);
        triangles.Add(vertice_index + 11);

        for (int i = 0; i < 10; i++)
        {
            triangles.Add(vertice_index + 0);
            triangles.Add(vertice_index + i + 2);
            triangles.Add(vertice_index + i + 1);

            triangles.Add(vertice_index + 22);
            triangles.Add(vertice_index + i + 12);
            triangles.Add(vertice_index + i + 11);
        }
    }

    Matrix Rotate(float angle, float rotation)
    {
        Matrix rotated;

        Vector3 x_angled = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
        rotated.x = new Vector3(Mathf.Cos(rotation) * x_angled.x, x_angled.y, Mathf.Sin(rotation) * x_angled.x);

        Vector3 y_angled = new Vector3(-Mathf.Sin(angle), Mathf.Cos(angle), 0);
        rotated.y = new Vector3(Mathf.Cos(rotation) * y_angled.x, y_angled.y, Mathf.Sin(rotation) * y_angled.x);

        rotated.z = new Vector3(-Mathf.Sin(rotation), 0, Mathf.Cos(rotation));

        return rotated;
    }

    Matrix RotateHeadache(float angle, float rotation, Matrix identity)
    {
        Matrix rotated;

        Vector3 angled_x = new Vector3(
            identity.x.x * Mathf.Cos(angle) + identity.y.x * Mathf.Sin(angle),
            identity.x.y * Mathf.Cos(angle) + identity.y.y * Mathf.Sin(angle),
            identity.x.z * Mathf.Cos(angle) + identity.y.z * Mathf.Sin(angle)
        );

        Vector3 angled_y = new Vector3(
            identity.y.x * Mathf.Cos(angle) - identity.x.x * Mathf.Sin(angle),
            identity.y.y * Mathf.Cos(angle) - identity.x.y * Mathf.Sin(angle),
            identity.y.z * Mathf.Cos(angle) - identity.x.z * Mathf.Sin(angle)
        );

        rotated.x = new Vector3(
            identity.y.x * Mathf.Sin(angle) +
            (angled_x.x - identity.y.x * Mathf.Sin(angle)) * Mathf.Cos(rotation) +
            identity.z.x * Mathf.Cos(angle) * Mathf.Sin(rotation),

            identity.y.y * Mathf.Sin(angle) +
            identity.x.y * Mathf.Cos(angle) * Mathf.Cos(rotation) +
            identity.z.y * Mathf.Cos(angle) * Mathf.Sin(rotation),

            identity.y.z * Mathf.Sin(angle) +
            (angled_x.z - identity.y.z * Mathf.Sin(angle)) * Mathf.Cos(rotation) +
            identity.z.z * Mathf.Cos(angle) * Mathf.Sin(rotation)
        );

        rotated.y = new Vector3(
            identity.y.x * Mathf.Cos(angle) +
            (angled_y.x - identity.y.x * Mathf.Cos(angle)) * Mathf.Cos(rotation) -
            identity.z.x * Mathf.Sin(angle) * Mathf.Sin(rotation),

            identity.y.y * Mathf.Cos(angle) -
            identity.x.y * Mathf.Sin(angle) * Mathf.Cos(rotation) +
            identity.z.y * Mathf.Sin(angle) * Mathf.Sin(rotation),

            identity.y.z * Mathf.Cos(angle) +
            (angled_y.z - identity.y.z * Mathf.Cos(angle)) * Mathf.Cos(rotation) -
            identity.z.z * Mathf.Sin(angle) * Mathf.Sin(rotation)
        );

        rotated.z = new Vector3(
            identity.z.x * Mathf.Cos(rotation) - identity.x.x * Mathf.Sin(rotation),
            identity.z.y * Mathf.Cos(rotation) - identity.x.y * Mathf.Sin(rotation),
            identity.z.z * Mathf.Cos(rotation) - identity.x.z * Mathf.Sin(rotation)
        );

        return rotated;
    }

    //Generates positions corresponting to the corners of an octagon with respect to.
    List<Vector3> OctagonPositions(Vector3 pos_center, Matrix rotation, float radius)
    {
        float side_half = radius / (1 + 2 / Mathf.Sqrt(2));

        return new List<Vector3>
        {
            pos_center + rotation.x * +radius + rotation.z * -side_half,
            pos_center + rotation.x * +radius + rotation.z * +side_half,
            pos_center + rotation.x * +side_half + rotation.z * +radius,
            pos_center + rotation.x * -side_half + rotation.z * +radius,
            pos_center + rotation.x * -radius + rotation.z * +side_half,
            pos_center + rotation.x * -radius + rotation.z * -side_half,
            pos_center + rotation.x * -side_half + rotation.z * -radius,
            pos_center + rotation.x * +side_half + rotation.z * -radius,
        };
    }

    List<Vector3> BasePositions(Vector3 pos_center, Matrix rotation, float radius)
    {
        //TODO
        float corner_component = 1.6f * radius / (1 + 2 / Mathf.Sqrt(2));

        return new List<Vector3>
        {
            pos_center + rotation.y * radius * -0.5f,
            pos_center + rotation.y * radius * -1,

            pos_center + rotation.z * +radius + rotation.y * radius * 0.5f,
            pos_center + rotation.z * +radius * 1.2f + rotation.y * -radius * 0.8f,

            pos_center + rotation.x * -corner_component + rotation.z * +corner_component + rotation.y * radius * 0.5f,
            pos_center + rotation.x * -corner_component * 1.2f + rotation.z * +corner_component * 1.2f + rotation.y * -radius * 0.8f,

            pos_center + rotation.x * -radius + rotation.y * radius * 0.5f,
            pos_center + rotation.x * -radius * 1.2f + rotation.y * -radius * 0.8f,

            pos_center + rotation.x * -corner_component + rotation.z * -corner_component + rotation.y * radius * 0.5f,
            pos_center + rotation.x * -corner_component * 1.2f + rotation.z * -corner_component * 1.2f + rotation.y * -radius * 0.8f,

            pos_center + rotation.z * -radius + rotation.y * radius * 0.5f,
            pos_center + rotation.z * -radius * 1.2f + rotation.y * -radius * 0.8f,
        };
    }

    List<Vector3> MiddlePositions(Vector3 pos_center, Matrix rotation, float thickness)
    {
        //float side_half = radius / (1 + 2 / Mathf.Sqrt(2));
        float radians = 72 * Mathf.Deg2Rad;

        thickness *= 0.8f;

        return new List<Vector3>
        {
            pos_center + rotation.y * thickness * -0.5f,
            pos_center + rotation.y * thickness,
            pos_center + rotation.y * thickness * Mathf.Cos(radians * 1) + rotation.z * thickness * Mathf.Sin(radians * 1),
            pos_center + rotation.y * thickness * Mathf.Cos(radians * 2) + rotation.z * thickness * Mathf.Sin(radians * 2),
            pos_center + rotation.y * thickness * Mathf.Cos(radians * 3) + rotation.z * thickness * Mathf.Sin(radians * 3),
            pos_center + rotation.y * thickness * Mathf.Cos(radians * 4) + rotation.z * thickness * Mathf.Sin(radians * 4),
        };
    }

    List<Vector3> EndPositions(Vector3 pos_center, Matrix rotation, float thickness)
    {
        return new List<Vector3>
        {
            pos_center + rotation.y * thickness * 0.4f,
            pos_center + rotation.y * thickness * 0.6f + rotation.z * thickness,
            pos_center - rotation.y * thickness * 0.1f + rotation.z * thickness * 0.5f,
            pos_center - rotation.y * thickness * 0.1f - rotation.z * thickness * 0.5f,
            pos_center + rotation.y * thickness * 0.6f - rotation.z * thickness,
        };
    }

    List<Vector3> LeafBasicPositions(Vector3 pos_center, Matrix rotation, float width)
    {
        return new List<Vector3>
        {
            pos_center,
            pos_center + rotation.z * width * 0.16f + rotation.y * width * 0.20f,
            pos_center + rotation.z * width * 0.33f + rotation.y * width * 0.20f,
            pos_center + rotation.z * width * 0.50f,
            pos_center + rotation.z * width * 0.50f - rotation.y * width * 0.5f,
            pos_center + rotation.z * width * 0.33f - rotation.y * width * 0.75f,
            pos_center - rotation.y * width * 1.00f,
            pos_center - rotation.z * width * 0.33f - rotation.y * width * 0.75f,
            pos_center - rotation.z * width * 0.50f - rotation.y * width * 0.50f,
            pos_center - rotation.z * width * 0.50f,
            pos_center - rotation.z * width * 0.33f + rotation.y * width * 0.20f,
            pos_center - rotation.z * width * 0.16f + rotation.y * width * 0.20f,
            pos_center - rotation.x * 0.01f,
        };
    }

    List<Vector3> LeafPositions(Vector3 pos_center, Matrix rotation, float width, float height)
    {
        List<Vector3> positions = new();

        positions.Add(pos_center + LeafPosition(rotation, width, height, width * +0.03f, height * +0.00f));
        positions.Add(pos_center + LeafPosition(rotation, width, height, width * +0.10f, height * +0.12f));
        positions.Add(pos_center + LeafPosition(rotation, width, height, width * +0.40f, height * +0.20f));
        positions.Add(pos_center + LeafPosition(rotation, width, height, width * +0.70f, height * +0.18f));
        positions.Add(pos_center + LeafPosition(rotation, width, height, width * +0.87f, height * +0.15f));

        positions.Add(pos_center + LeafPosition(rotation, width, height, width * +1.00f, height * +0.08f));
        positions.Add(pos_center + LeafPosition(rotation, width, height, width * +1.00f, height * -0.20f));
        positions.Add(pos_center + LeafPosition(rotation, width, height, width * +0.90f, height * -0.40f));
        positions.Add(pos_center + LeafPosition(rotation, width, height, width * +0.75f, height * -0.62f));
        positions.Add(pos_center + LeafPosition(rotation, width, height, width * +0.40f, height * -0.88f));
                                                                                                  
        positions.Add(pos_center + LeafPosition(rotation, width, height, width * +0.07f, height * -0.97f));
        positions.Add(pos_center + LeafPosition(rotation, width, height, width * +0.00f, height * -1.00f));
        positions.Add(pos_center + LeafPosition(rotation, width, height, width * -0.07f, height * -0.97f));

        positions.Add(pos_center + LeafPosition(rotation, width, height, width * -0.35f, height * -0.92f));
        positions.Add(pos_center + LeafPosition(rotation, width, height, width * -0.70f, height * -0.68f));
        positions.Add(pos_center + LeafPosition(rotation, width, height, width * -0.87f, height * -0.45f));
        positions.Add(pos_center + LeafPosition(rotation, width, height, width * -0.95f, height * -0.25f));
        positions.Add(pos_center + LeafPosition(rotation, width, height, width * -1.00f, height * -0.00f));

        positions.Add(pos_center + LeafPosition(rotation, width, height, width * -0.90f, height * +0.10f));
        positions.Add(pos_center + LeafPosition(rotation, width, height, width * -0.70f, height * +0.18f));
        positions.Add(pos_center + LeafPosition(rotation, width, height, width * -0.40f, height * +0.20f));
        positions.Add(pos_center + LeafPosition(rotation, width, height, width * -0.10f, height * +0.12f));
        positions.Add(pos_center + LeafPosition(rotation, width, height, width * -0.03f, height * +0.00f));

        return positions;
    }

    Vector3 LeafPosition(Matrix rotation, float width, float height, float pos_z, float pos_y)
    {
        float distance_from_center;

        if (pos_y > 0)
        {
            distance_from_center = Mathf.Pow(pos_z / width, 2) + Mathf.Pow(pos_y / height, 2) / 0.04f;
        }
        else
        {
            distance_from_center = Mathf.Pow(pos_z / width, 2) + Mathf.Pow(pos_y / height, 2);
        }

        float depth = -0.2f * Mathf.Pow(distance_from_center, 2) + 0.16f * distance_from_center;

        depth *= (height + width) / 2;

        return rotation.z * pos_z + rotation.y * pos_y + rotation.x * depth;
    }

    void GenerateCube(Matrix rotation, List<Vector3> vertices, List<int> triangles)
    {
        int vertice_index = vertices.Count;

        Vector3 rot_x = rotation.x / 2;
        Vector3 rot_y = rotation.y / 2;
        Vector3 rot_z = rotation.z / 2;

        Vector3 pos_center = new Vector3(0, 0, 0);
        vertices.AddRange(new List<Vector3>
        {
            pos_center - rot_x - rot_y - rot_z,
            pos_center + rot_x - rot_y - rot_z,
            pos_center - rot_x - rot_y + rot_z,
            pos_center + rot_x - rot_y + rot_z,
            pos_center - rot_x + rot_y - rot_z,
            pos_center + rot_x + rot_y - rot_z,
            pos_center - rot_x + rot_y + rot_z,
            pos_center + rot_x + rot_y + rot_z,
        });

        triangles.Add(vertice_index + 0);
        triangles.Add(vertice_index + 1);
        triangles.Add(vertice_index + 2);

        triangles.Add(vertice_index + 1);
        triangles.Add(vertice_index + 3);
        triangles.Add(vertice_index + 2);


        triangles.Add(vertice_index + 0);
        triangles.Add(vertice_index + 4);
        triangles.Add(vertice_index + 1);

        triangles.Add(vertice_index + 1);
        triangles.Add(vertice_index + 4);
        triangles.Add(vertice_index + 5);


        triangles.Add(vertice_index + 2);
        triangles.Add(vertice_index + 6);
        triangles.Add(vertice_index + 0);

        triangles.Add(vertice_index + 0);
        triangles.Add(vertice_index + 6);
        triangles.Add(vertice_index + 4);


        triangles.Add(vertice_index + 3);
        triangles.Add(vertice_index + 7);
        triangles.Add(vertice_index + 2);

        triangles.Add(vertice_index + 2);
        triangles.Add(vertice_index + 7);
        triangles.Add(vertice_index + 6);


        triangles.Add(vertice_index + 1);
        triangles.Add(vertice_index + 5);
        triangles.Add(vertice_index + 3);

        triangles.Add(vertice_index + 3);
        triangles.Add(vertice_index + 5);
        triangles.Add(vertice_index + 7);


        triangles.Add(vertice_index + 4);
        triangles.Add(vertice_index + 6);
        triangles.Add(vertice_index + 5);

        triangles.Add(vertice_index + 5);
        triangles.Add(vertice_index + 6);
        triangles.Add(vertice_index + 7);
    }
}

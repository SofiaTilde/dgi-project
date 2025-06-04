using UnityEngine;
using Classes;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Reflection;
using System.CodeDom.Compiler;
using System.Xml.Serialization;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UI;

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
            Debug.Log(leaf.Value.LengthFenestrations);
            Debug.Log(leaf.Value.ThicknessFenestrations);
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

        float width = leaf.Width / 2;
        float height = leaf.Height;
        float slit_length = leaf.LengthFenestrations;
        float slit_thickness = leaf.ThicknessFenestrations * 0.04f * height;
        float[] holes = leaf.Holes;

        Matrix rotated_matrix = Rotate(angle, rotation);

        List<List<Vector3>> positions = new();

        positions.AddRange(LeafPositions(positions_start + rotated_matrix.x * 0.01f, rotated_matrix, width, height, slit_length, slit_thickness, holes));

        //AddLeafMeshComponents(positions, vertices, triangles, false);
        AddLeafMeshComponents(positions, vertices, triangles, true);
    }

    void AddLeafMeshComponents(List<List<Vector3>> positions, List<Vector3> vertices, List<int> triangles, bool invert)
    {
        int vertice_index = vertices.Count;
        int triangle_index = triangles.Count;

        vertices.AddRange(positions[0]);

        List<int> connectors;

        for (int i = 0; i < 10; i++)
        {
            vertices.AddRange(positions[i + 1]);
        }

        for (int i = 0; i < 9; ++i)
        {
            connectors = new List<int>() { 4, 17, 10, 4, 5, 17, 13, 14, 18 };
            for (int j = 0; j < 9; j++)
            {
                triangles.Add(vertice_index + 13 + i * 19 + connectors[j]);
            }

            if (i != 4)
            {
                connectors = new List<int>()
                {
                    9 , 8 , 7 , 6 , 5 , 17, 16, 15, 14, 18,
                    19, 20, 21, 22, 23, 29, 30, 31, 32, 37,
                };

                for (int j = 0; j < 9; j++)
                {
                    triangles.Add(vertice_index + 13 + i * 19 + connectors[j]);
                    triangles.Add(vertice_index + 13 + i * 19 + connectors[j + 11]);
                    triangles.Add(vertice_index + 13 + i * 19 + connectors[j + 1]);

                    triangles.Add(vertice_index + 13 + i * 19 + connectors[j]);
                    triangles.Add(vertice_index + 13 + i * 19 + connectors[j + 10]);
                    triangles.Add(vertice_index + 13 + i * 19 + connectors[j + 11]);
                }
            }
        }

        for (int j = 0; j < 6; j++)
        {
            connectors = new List<int>()
            {
                9, 8, 7, 6, 5, 17, 16,
                0, 1, 2, 3, 4, 10, 11,
            };

            triangles.Add(vertice_index + 13 + 4 * 19 + connectors[j]);
            triangles.Add(vertice_index + 5);
            triangles.Add(vertice_index + 13 + 4 * 19 + connectors[j + 1]);

            triangles.Add(vertice_index + 13 + 5 * 19 + connectors[j + 7]);
            triangles.Add(vertice_index + 13 + 5 * 19 + connectors[j + 8]);
            triangles.Add(vertice_index + 7);
        }

        connectors = new List<int>()
        {
            4, 13, 14,
            4, 14, 15,
            4, 15, 16,
            3, 4, 16,
            2, 3, 16,
            2, 16, 17,
            2, 17, 23,
            2, 23, 24,
            2, 24, 25,
            1, 2, 25,
            1, 25, 26,
            0, 1, 26,
            0, 26, 31,
        };

        for (int i = 0; i < 39; i++)
        {
            triangles.Add(vertice_index + connectors[i]);
        }

        connectors = new List<int>()
        {
            8, 192, 193,
            8, 191, 192,
            8, 190, 191,
            8, 9, 190,
            9, 10, 190,
            10, 189, 190,
            10, 201, 189,
            10, 200, 201,
            10, 199, 200,
            10, 11, 199,
            11, 198, 199,
            11, 12, 198,
            12, 31, 198,
        };

        for (int i = 0; i < 39; i++)
        {
            triangles.Add(vertice_index + connectors[i]);
        }

        connectors = new List<int>()
        {
            0, 31, 12,
            31, 202, 198,
            189, 201, 188,
            188, 201, 194,
            198, 202, 197,
        };

        for (int i = 0; i < 15; i++)
        {
            triangles.Add(vertice_index + connectors[i]);
        }

        connectors = new List<int>()
        {
            105, 5, 6,
            119, 6, 7,
            119, 105, 6,
            105, 119, 104,
            104, 119, 120,
            104, 120, 103,
            103, 120, 121,
            103, 121, 126,
            103, 126, 107,
        };

        for (int i = 0; i < 27; i++)
        {
            triangles.Add(vertice_index + connectors[i]);
        }

        if (invert)
        {
            int new_triangles = triangles.Count - triangle_index;
            for (int t = 0; t < new_triangles; t += 3)
            {
                int temp = triangles[t];
                triangles[t] = triangles[t + 1];
                triangles[t + 1] = temp;
            }
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

    // Creates the positions for the vertices of the outline of the leaf. Also calls the function to create 10 leaf slits at predetermined locations.
    List<List<Vector3>> LeafPositions(Vector3 pos_center, Matrix rotation, float width, float height, float slit_length, float slit_thickness, float[] holes)
    {
        List<List<Vector3>> positions = new();
        List<Vector3> positions_basic = new();

        positions_basic.Add(new Vector3(0, height * +0.00f, width * +0.03f));
        positions_basic.Add(new Vector3(0, height * +0.18f, width * +0.10f));
        positions_basic.Add(new Vector3(0, height * +0.20f, width * +0.40f));
        positions_basic.Add(new Vector3(0, height * +0.18f, width * +0.70f));
        positions_basic.Add(new Vector3(0, height * +0.15f, width * +0.87f));

        positions_basic.Add(new Vector3(0, height * -0.97f, width * +0.07f));
        positions_basic.Add(new Vector3(0, height * -1.00f, width * +0.00f));
        positions_basic.Add(new Vector3(0, height * -0.97f, width * -0.07f));

        positions_basic.Add(new Vector3(0, height * +0.10f, width * -0.90f));
        positions_basic.Add(new Vector3(0, height * +0.18f, width * -0.70f));
        positions_basic.Add(new Vector3(0, height * +0.20f, width * -0.40f));
        positions_basic.Add(new Vector3(0, height * +0.18f, width * -0.10f));
        positions_basic.Add(new Vector3(0, height * +0.00f, width * -0.03f));

        positions_basic = BendPositions(positions_basic, pos_center, rotation, width, height);

        positions.Add(positions_basic);

        positions.Add(BendPositions(LeafSlit(new Vector3(0, height * +0.08f, width * +1.00f), slit_length, slit_thickness, 25, 0, holes[1]), pos_center, rotation, width, height));
        positions.Add(BendPositions(LeafSlit(new Vector3(0, height * -0.20f, width * +1.00f), slit_length, slit_thickness, -5, 0, holes[3]), pos_center, rotation, width, height));
        positions.Add(BendPositions(LeafSlit(new Vector3(0, height * -0.40f, width * +0.90f), slit_length, slit_thickness, -20, 0, holes[5]), pos_center, rotation, width, height));
        positions.Add(BendPositions(LeafSlit(new Vector3(0, height * -0.62f, width * +0.75f), slit_length, slit_thickness, -35, 0, holes[7]), pos_center, rotation, width, height));
        positions.Add(BendPositions(LeafSlit(new Vector3(0, height * -0.88f, width * +0.40f), slit_length, slit_thickness, -65, 0, holes[9]), pos_center, rotation, width, height));
        
        positions.Add(BendPositions(LeafSlit(new Vector3(0, height * -0.92f, width * -0.35f), slit_length, slit_thickness, 245, 0, holes[8]), pos_center, rotation, width, height));
        positions.Add(BendPositions(LeafSlit(new Vector3(0, height * -0.68f, width * -0.70f), slit_length, slit_thickness, 215, 0, holes[6]), pos_center, rotation, width, height));
        positions.Add(BendPositions(LeafSlit(new Vector3(0, height * -0.45f, width * -0.87f), slit_length, slit_thickness, 200, 0, holes[4]), pos_center, rotation, width, height));
        positions.Add(BendPositions(LeafSlit(new Vector3(0, height * -0.25f, width * -0.95f), slit_length, slit_thickness, 185, 0, holes[2]), pos_center, rotation, width, height));
        positions.Add(BendPositions(LeafSlit(new Vector3(0, height * -0.00f, width * -1.00f), slit_length, slit_thickness, 160, 0, holes[0]), pos_center, rotation, width, height));

        return positions;
    }

    // Creates the positions for the vertices of a single leaf slit. The shear property is not utilized but could in theory adjust the vertices at the edge of the slit to make them align with the outline of the leaf.
    List<Vector3> LeafSlit(Vector3 pos_edge, float slit_length, float thickness, float angle, float shear, float hole_size)
    {
        List<Vector3> positions = new List<Vector3>();

        float sin = Mathf.Sin(angle * Mathf.Deg2Rad);
        float cos = Mathf.Cos(angle * Mathf.Deg2Rad);
        float tan = Mathf.Tan(angle * Mathf.Deg2Rad);

        float length = pos_edge.z / cos * slit_length;

        Vector3 pos_hole = new(0, (pos_edge.y - tan * pos_edge.z + (pos_edge.y - sin * length)) / 2, (pos_edge.z - cos * length) / 2);

        positions.Add(pos_edge + new Vector3(0, +thickness * cos - shear * sin, -thickness * sin - shear * cos));
        positions.Add(pos_edge + new Vector3(0, +thickness * cos - (length * 1) / 3 * sin, -thickness * sin - (length * 1) / 3 * cos));
        positions.Add(pos_edge + new Vector3(0, +thickness * cos - (length * 2) / 3 * sin, -thickness * sin - (length * 2) / 3 * cos));
        positions.Add(pos_edge + new Vector3(0, +thickness * cos - (length * 3) / 3 * sin, -thickness * sin - (length * 3) / 3 * cos));
        positions.Add(pos_edge + new Vector3(0, +thickness * 0.33f * cos - (length + thickness) * sin, -thickness * 0.33f * sin - (length + thickness) * cos));

        positions.Add(pos_edge + new Vector3(0, -thickness * 0.33f * cos - (length + thickness) * sin, +thickness * 0.33f * sin - (length + thickness) * cos));
        positions.Add(pos_edge + new Vector3(0, -thickness * cos - (length * 3) / 3 * sin, +thickness * sin - (length * 3) / 3 * cos));
        positions.Add(pos_edge + new Vector3(0, -thickness * cos - (length * 2) / 3 * sin, +thickness * sin - (length * 2) / 3 * cos));
        positions.Add(pos_edge + new Vector3(0, -thickness * cos - (length * 1) / 3 * sin, +thickness * sin - (length * 1) / 3 * cos));
        positions.Add(pos_edge + new Vector3(0, -thickness * cos + shear * sin, +thickness * sin + shear * cos));


        positions.Add(pos_hole + hole_size * new Vector3(0, +thickness * cos * 0.33f + thickness * sin * 1.60f, -thickness * sin * 0.33f + thickness * cos * 1.60f));
        positions.Add(pos_hole + hole_size * new Vector3(0, +thickness * cos * 1.00f + thickness * sin * 1.00f, -thickness * sin * 1.00f + thickness * cos * 1.00f));
        positions.Add(pos_hole + hole_size * new Vector3(0, +thickness * cos * 1.00f - thickness * sin * 1.00f, -thickness * sin * 1.00f - thickness * cos * 1.00f));
        positions.Add(pos_hole + hole_size * new Vector3(0, +thickness * cos * 0.33f - thickness * sin * 1.60f, -thickness * sin * 0.33f - thickness * cos * 1.60f));

        positions.Add(pos_hole + hole_size * new Vector3(0, -thickness * cos * 0.33f - thickness * sin * 1.60f, +thickness * sin * 0.33f - thickness * cos * 1.60f));
        positions.Add(pos_hole + hole_size * new Vector3(0, -thickness * cos * 1.00f - thickness * sin * 1.00f, +thickness * sin * 1.00f - thickness * cos * 1.00f));
        positions.Add(pos_hole + hole_size * new Vector3(0, -thickness * cos * 1.00f + thickness * sin * 1.00f, +thickness * sin * 1.00f + thickness * cos * 1.00f));
        positions.Add(pos_hole + hole_size * new Vector3(0, -thickness * cos * 0.33f + thickness * sin * 1.60f, +thickness * sin * 0.33f + thickness * cos * 1.60f));


        positions.Add(new Vector3(0, pos_edge.y - tan * pos_edge.z, 0));

        return positions;
    }

    // Moves leaf vertices in the x axis to create the curvature of a leaf using a 2nd degree polynomial function. Also applies angle and roation.
    List<Vector3> BendPositions(List<Vector3> positions, Vector3 pos_center, Matrix rotation, float width, float height)
    {
        List<Vector3> positions_bent = new();

        foreach (Vector3 position in positions)
        {
            float distance_from_center;

            if (position.y > 0)
            {
                distance_from_center = Mathf.Pow(position.z / width, 2) + Mathf.Pow(position.y / height, 2) / 0.04f;
            }
            else
            {
                distance_from_center = Mathf.Pow(position.z / width, 2) + Mathf.Pow(position.y / height, 2);
            }

            float magnitude = 0.4f;
            float depth = -magnitude * Mathf.Pow(distance_from_center, 2) + 0.8f * magnitude * distance_from_center;
            depth *= (height + width) / 2;
            positions_bent.Add(pos_center + rotation.z * position.z + rotation.y * position.y + rotation.x * depth);
        }

        return positions_bent;
    }

    // Generates a cube mesh for debugging. Unused in the final version.
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

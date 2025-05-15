using UnityEngine;
using Classes;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Reflection;
using System.CodeDom.Compiler;
using System.Xml.Serialization;

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
        myVertices[0] = new Vector3(1, 0, 1);
        myVertices[1] = new Vector3(1, 0, -1);
        myVertices[2] = new Vector3(-1, 0, -1);
        myVertices[3] = new Vector3(-1, 0, 1);
        myVertices[4] = new Vector3(0, 1, 0);

        myTriangles = new int[]
        {
            2, 1, 0,
            3, 2, 0,
            0, 1, 4,
            1, 2, 4,
            2, 3, 4,
            3, 0, 4,
        };

        SortedDictionary<string, Internode> internodes = new SortedDictionary<string, Internode>();
        SortedDictionary<string, Petiole> petioles = new SortedDictionary<string, Petiole>();
        SortedDictionary<string, Leaf> leaves = new SortedDictionary<string, Leaf>();
        Internode i1 = new(1, 1, "I1", 1, ref internodes, ref petioles, ref leaves);
        Internode i2 = new(1, 1, "I2", 1, ref internodes, ref petioles, ref leaves);

        i1.Thickness = 0.2;
        i1.Length = 0.5;
        i1.Angle = 45;
        i1.Rotation = 45;

        i2.Thickness = 0.2;
        i2.Length = 0.5;
        i2.Angle = 12;
        i2.Rotation = -30;

        List<Vector3> vertices = new();
        List<int> triangles = new();

        Vector3 next;
        next = GenerateInternodeMesh(i1, new Vector3(0, 0, 0), vertices, triangles);
        next = GenerateInternodeMesh(i2, next, vertices, triangles);

        Mesh mesh = GetComponent<MeshFilter>().mesh;
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();

        /*double[] lengths = { 0.2, 0.19, 0.215, 0.205, 0.220 };
        int[] angles = { 10, 12, 8, 22, 18 };
        int[] rotations = { 76, -45, 126, 13, -158 };
        LineRenderer line = GetComponent<LineRenderer>();
        line.SetPositions(new Vector3[0]);
        line.positionCount = lengths.Length + 1;
        Vector3 last_end = new Vector3();

        line.SetPosition(0, new Vector3());

        for (int i = 0; i < lengths.Length; i++)
        {
            Vector3 end_position = new Vector3();
            end_position += new Vector3(Mathf.Sin(angles[i] * Mathf.Deg2Rad) * (float)lengths[i], Mathf.Cos(angles[i] * Mathf.Deg2Rad) * (float)lengths[i], 0);

            //Debug.Log(angles[i] * Mathf.Deg2Rad);
            Debug.Log(Mathf.Cos(angles[i] * Mathf.Deg2Rad));
            Debug.Log(Mathf.Cos(angles[i] * Mathf.Deg2Rad) * (float)lengths[i]);

            end_position += new Vector3(Mathf.Cos(rotations[i] * Mathf.Deg2Rad) * end_position.x, 0, Mathf.Sin(rotations[i] * Mathf.Deg2Rad) * end_position.x);

            end_position += last_end;

            line.SetPosition(i + 1, end_position);

            last_end = end_position;
        }*/
    }

    void Update()
    {
        
    }

    //Generates the mesh of an entire Monstera Deliciosa specimen.
    void GenereateSpecimenMesh(Internode[] internodes, Petiole[] petiols, Leaf[] leaves)
    {
        LineRenderer line = GetComponent<LineRenderer>();
        line.SetPositions(new Vector3[0]);
        line.positionCount = internodes.Length;
        Vector3 last_end = new Vector3();

        for (int i = 0; i < internodes.Length; i++)
        {
            Vector3 end_position = new Vector3();
            end_position += new Vector3(Mathf.Sin(internodes[i].Angle * Mathf.Deg2Rad) * (float)internodes[i].Length, Mathf.Cos(internodes[i].Angle * Mathf.Deg2Rad) * (float)internodes[i].Length, 0);
            end_position += new Vector3(Mathf.Cos(internodes[i].Rotation * Mathf.Deg2Rad) * end_position.x, 0, Mathf.Sin(internodes[i].Rotation * Mathf.Deg2Rad) * end_position.x);

            end_position += last_end;
            line.SetPosition(i + 1, end_position);
            last_end = end_position;
        }
    }

    //Generates a mesh for a given internode at a given position and adds it to a given mesh.
    Vector3 GenerateInternodeMesh(Internode internode, Vector3 positions_start, List<Vector3> vertices, List<int> triangles)
    {
        float angle = internode.Angle * Mathf.Deg2Rad;
        float rotation = internode.Rotation * Mathf.Deg2Rad;

        Matrix rotated_matrix = Rotate(angle, rotation);

        Vector3[] positions_octagon_bottom = OctagonPositions(positions_start, rotated_matrix, (float)internode.Thickness / 2);
        Vector3[] positions_octagon_top = OctagonPositions(positions_start + (rotated_matrix.y * (float)internode.Length), rotated_matrix, (float)internode.Thickness / 2);
        Vector3 position_bottom = positions_start + (rotated_matrix.y * -(float)internode.Thickness / 4);
        Vector3 position_top = positions_start + (rotated_matrix.y * ((float)internode.Length + (float)internode.Thickness / 4));

        vertices.Add(position_bottom);
        vertices.Add(position_top);

        int vertice_index = vertices.Count;

        for (int t = 0; t < 8; t++)
        {
            vertices.Add(positions_octagon_bottom[t]);
            vertices.Add(positions_octagon_top[t]);

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

        return positions_start + (rotated_matrix.y * (float)internode.Length);
    }

    void GenereatePetioleMesh(Petiole petiole, Vector3 positions_start, List<Vector3> vertices, List<int> triangles)
    {
        float angle = petiole.Angle * Mathf.Deg2Rad;
        float rotation = petiole.Rotation * Mathf.Deg2Rad;
        //int detail = 1;

        Matrix rotated_matrix = Rotate(angle, rotation);

        Vector3[] positions_base = BasePositions(positions_start, rotated_matrix, (float)petiole.ThicknessStart / 2);
        //List<Vector3[]> positions_segments = SegmentPositions(positions_start + (rotated_matrix.y * (float)internode.Length), rotated_matrix.x, rotated_matrix.z, (float)internode.Thickness / 2);

        int vertice_index = vertices.Count;

        /*for (int t = 0; t < 8; t++)
        {
            vertices.Add(positions_octagon_bottom[t]);
            vertices.Add(positions_octagon_top[t]);

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
        }*/
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

    //Generates positions corresponting to the corners of an octagon with respect to.
    Vector3[] OctagonPositions(Vector3 pos_center, Matrix rotation, float radius)
    {
        float side_half = radius / (1 + 2 / Mathf.Sqrt(2));

        return new Vector3[]
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
    Vector3[] BasePositions(Vector3 pos_center, Matrix rotation, float radius)
    {
        float corner_component = radius / (1 + 2 / Mathf.Sqrt(2));

        return new Vector3[]
        {
            pos_center,

            pos_center + rotation.z * +radius + rotation.y * radius * 0.5f,
            pos_center + rotation.x * -corner_component + rotation.z * +corner_component, rotation.y * radius * 0.5f,
            pos_center + rotation.x * -radius + rotation.y * radius * 0.5f,
            pos_center + rotation.x * -corner_component + rotation.z * -corner_component, rotation.y * radius * 0.5f,
            pos_center + rotation.z * -radius + rotation.y * radius * 0.5f,

            pos_center + rotation.z * +radius * 1.2f + rotation.y * -radius * 0.8f,
            pos_center + rotation.x * -corner_component * 1.2f + rotation.z * +corner_component * 1.2f, rotation.y * -radius * 0.8f,
            pos_center + rotation.x * -radius * 1.2f + rotation.y * -radius * 0.8f,
            pos_center + rotation.x * -corner_component * 1.2f + rotation.z * -corner_component * 1.2f, rotation.y * -radius * 0.8f,
            pos_center + rotation.z * -radius * 1.2f + rotation.y * -radius * 0.8f,

            pos_center + rotation.y * -1,
        };
    }
    Vector3[] SegmentPositions(Vector3 pos_center, Vector3 x_rotated, Vector3 z_rotated, float radius)
    {
        float side_half = radius / (1 + 2 / Mathf.Sqrt(2));

        return new Vector3[]
        {
            
        };
    }
}

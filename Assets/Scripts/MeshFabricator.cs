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
    // A 3x3 matrix used for rotations. 
    struct Matrix
    {
        public Vector3 x;
        public Vector3 y;
        public Vector3 z;
    }

    //Generates the mesh of an entire Monstera Deliciosa specimen.
    public void GenerateSpecimenMesh(SortedDictionary<string, Internode> internodes, SortedDictionary<string, Petiole> petioles, SortedDictionary<string, Leaf> leaves)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        // The ends of some generated components are saved and used as starting points for other components. 
        List<Vector3> internode_ends = new List<Vector3> { new Vector3(0, 0, 0) };
        List<Vector3> petiole_ends = new ();

        int petiole_count = 1;
        int leaf_count = 0;

        // Generates every internode.
        foreach (KeyValuePair<string, Internode> internode in internodes)
        {
            internode_ends.Add(GenerateInternodeMesh(internode.Value, internode_ends.Last(), vertices, triangles));
        }

        // Generates every petiole.
        foreach (KeyValuePair<string, Petiole> petiole in petioles)
        {
            petiole_ends.Add(GeneratePetioleMesh(petiole.Value, internode_ends[petiole_count], vertices, triangles));
            petiole_count++;
        }

        // Generates every leaf.
        foreach (KeyValuePair<string, Leaf> leaf in leaves)
        {
            GenerateLeafMesh(leaf.Value, petiole_ends[leaf_count], vertices, triangles);
            leaf_count++;
        }
        
        // The mesh of the current specimen is replaced with a new one.
        Mesh mesh = new();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        GetComponent<MeshFilter>().mesh = mesh;
    }

    // Generates vertices and triangles for an internode at a given position.
    Vector3 GenerateInternodeMesh(Internode internode, Vector3 positions_start, List<Vector3> vertices, List<int> triangles)
    {
        // The angle and rotation are converted to radians.
        float angle = internode.Angle * Mathf.Deg2Rad;
        float rotation = internode.Rotation * Mathf.Deg2Rad;

        // Calculates a rotation matrix with the given angle and rotation
        Matrix rotated_matrix = Rotate(angle, rotation);

        List<Vector3> positions = new();

        // The positions of the top and bottom vertices along with the bottom and top rings are stored. 
        positions.Add(positions_start + (rotated_matrix.y * -internode.Thickness / 4));
        positions.Add(positions_start + (rotated_matrix.y * (internode.Length + internode.Thickness / 4)));
        positions.AddRange(OctagonPositions(positions_start, rotated_matrix, internode.Thickness / 2));
        positions.AddRange(OctagonPositions(positions_start + (rotated_matrix.y * internode.Length), rotated_matrix, internode.Thickness / 2));

        // The positions are used to create vertices and triangles.
        AddInternodeMeshComponents(positions.ToArray(), vertices, triangles);

        // The position of the end of the internode is returned to be used as the starting point of other components.
        return positions_start + (rotated_matrix.y * internode.Length);
    }

    // Fills the vertices and triangles lists with the generated internode positions and how they're stitced together into a mesh.
    void AddInternodeMeshComponents(Vector3[] positions, List<Vector3> vertices, List<int> triangles)
    {
        // Bottom and top points
        vertices.Add(positions[0]);
        vertices.Add(positions[1]);

        int vertice_index = vertices.Count;

        // Adds one octagonal segment consisting of pieces of the top and bottom caps and one square side.
        for (int t = 0; t < 8; t++)
        {
            // Bottom and top ring segments
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

    // Generates vertices and triangles for a petiole at a given position.
    Vector3 GeneratePetioleMesh(Petiole petiole, Vector3 positions_start, List<Vector3> vertices, List<int> triangles)
    {
        // The angle and rotation are converted to radians.
        float angle = petiole.Angle * Mathf.Deg2Rad;
        float rotation = petiole.Rotation * Mathf.Deg2Rad;

        // Calculates a rotation matrix with the given angle and rotation
        Matrix rotated_matrix = Rotate(angle, rotation);

        List<Vector3> positions = new();

        // The positions of the base section, middle section, and end section are stored. The middle section is located one third of the total length from the base.
        positions.AddRange(BasePositions(positions_start, rotated_matrix, petiole.ThicknessStart / 2));
        positions.AddRange(MiddlePositions(positions_start + rotated_matrix.x * petiole.Length / 3, rotated_matrix, (petiole.ThicknessStart + petiole.ThicknessEnd) / 4));
        positions.AddRange(EndPositions(positions_start + rotated_matrix.x * petiole.Length, rotated_matrix, petiole.ThicknessEnd / 2));

        // The positions are used to create vertices and triangles.
        AddPetioleMeshComponents(positions.ToArray(), vertices, triangles);

        // The position of the end of the petiole is returned to be used as the starting point of its corresponding leaf.
        return positions_start + (rotated_matrix.x * petiole.Length);
    }

    // Fills the vertices and triangles lists with the generated petiole positions and how they're stitced together into a mesh.
    void AddPetioleMeshComponents(Vector3[] positions, List<Vector3> vertices, List<int> triangles)
    {
        int vertice_index = vertices.Count;

        // All vertices are added at once.
        for (int i = 0; i < 23; i++)
        {
            vertices.Add(positions[i]);
        }

        // The triangles for the base.
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

        // Vertex indexes where each row represenst the corners of a square. These squares connect the base to the middle.
        int[] verts_middle = {
            0, 2, 12, 13,
            2, 3, 13, 14,
            3, 1, 14, 15,
            1, 11, 16, 17,
            11, 10, 17, 13,
            10, 0, 13, 12,
        };

        // Triangles for the squares are added.
        for (int i = 0; i < 6; i++)
        {
            triangles.Add(vertice_index + verts_middle[i * 4 + 0]);
            triangles.Add(vertice_index + verts_middle[i * 4 + 1]);
            triangles.Add(vertice_index + verts_middle[i * 4 + 2]);

            triangles.Add(vertice_index + verts_middle[i * 4 + 1]);
            triangles.Add(vertice_index + verts_middle[i * 4 + 3]);
            triangles.Add(vertice_index + verts_middle[i * 4 + 2]);
        }

        // The only single triangle of this segment, located at the bottom.
        triangles.Add(vertice_index + 1);
        triangles.Add(vertice_index + 16);
        triangles.Add(vertice_index + 15);

        // Squares that connect the middle to the end are added.
        for (int i = 0; i < 5; i++)
        {
            triangles.Add(vertice_index + 13 + ((i + 0) % 5));
            triangles.Add(vertice_index + 13 + ((i + 1) % 5));
            triangles.Add(vertice_index + 18 + ((i + 0) % 5));

            triangles.Add(vertice_index + 13 + ((i + 1) % 5));
            triangles.Add(vertice_index + 18 + ((i + 1) % 5));
            triangles.Add(vertice_index + 18 + ((i + 0) % 5));
        }

        // The final three triangles seal the end of the petiole.
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

    // Generates vertices and triangles for a leaf at a given position.
    public void GenerateLeafMesh(Leaf leaf, Vector3 positions_start, List<Vector3> vertices, List<int> triangles)
    {
        // The angle and rotation are converted to radians.
        float angle = leaf.Angle * Mathf.Deg2Rad;
        float rotation = leaf.Rotation * Mathf.Deg2Rad;

        // Leaf atributes are tweaked before 
        float width = leaf.Width / 2;
        float height = leaf.Height;
        float fenestration_length = leaf.LengthFenestrations;
        float fenestration_thickness = leaf.ThicknessFenestrations * 0.04f * height;
        float[] holes = leaf.Holes;

        // Calculates a rotation matrix with the given angle and rotation
        Matrix rotated_matrix = Rotate(angle, rotation);

        List<List<Vector3>> positions = new();

        // All positions are added at once.
        positions.AddRange(LeafPositions(positions_start, rotated_matrix, width, height, fenestration_length, fenestration_thickness, holes));

        // The positions are used to create vertices and triangles, once for the front side and once for the backside.
        AddLeafMeshComponents(positions, vertices, triangles, false);
        AddLeafMeshComponents(positions, vertices, triangles, true);
    }

    // Fills the vertices and triangles lists with the generated leaf positions and how they're stitced together into a mesh. If invert is true, all triangles are flipped to face the opposite direction.
    void AddLeafMeshComponents(List<List<Vector3>> positions, List<Vector3> vertices, List<int> triangles, bool invert)
    {
        int vertice_index = vertices.Count;
        int triangle_index = triangles.Count;

        // The perimeter positions are added.
        vertices.AddRange(positions[0]);

        // The fnestration positions are added.
        for (int i = 0; i < 10; i++)
        {
            vertices.AddRange(positions[i + 1]);
        }

        // A list of vertices are used to reduce the amount of repeated code when creating triangles.
        List<int> vertex_indexs;

        // Every region between fenestrations and most of their individual triangles are generated here.
        for (int i = 0; i < 9; ++i)
        {
            // The triangles that belong to only one fenestration are generated.
            vertex_indexs = new List<int>() { 4, 17, 10, 4, 5, 17, 13, 14, 18 };
            for (int j = 0; j < 9; j++)
            {
                triangles.Add(vertice_index + 13 + i * 19 + vertex_indexs[j]);
            }

            // The region between the bottom two fenestrations is different than most in between regions. That one is skipped and the rest are generated here.
            if (i != 4)
            {
                vertex_indexs = new List<int>()
                {
                    9 , 8 , 7 , 6 , 5 , 17, 16, 15, 14, 18,
                    19, 20, 21, 22, 23, 29, 30, 31, 32, 37,
                };

                for (int j = 0; j < 9; j++)
                {
                    triangles.Add(vertice_index + 13 + i * 19 + vertex_indexs[j]);
                    triangles.Add(vertice_index + 13 + i * 19 + vertex_indexs[j + 11]);
                    triangles.Add(vertice_index + 13 + i * 19 + vertex_indexs[j + 1]);

                    triangles.Add(vertice_index + 13 + i * 19 + vertex_indexs[j]);
                    triangles.Add(vertice_index + 13 + i * 19 + vertex_indexs[j + 10]);
                    triangles.Add(vertice_index + 13 + i * 19 + vertex_indexs[j + 11]);
                }
            }
        }

        // Most of the bottom two fenestration are connected to the outline.
        for (int j = 0; j < 6; j++)
        {
            vertex_indexs = new List<int>()
            {
                9, 8, 7, 6, 5, 17, 16,
                0, 1, 2, 3, 4, 10, 11,
            };

            triangles.Add(vertice_index + 13 + 4 * 19 + vertex_indexs[j]);
            triangles.Add(vertice_index + 5);
            triangles.Add(vertice_index + 13 + 4 * 19 + vertex_indexs[j + 1]);

            triangles.Add(vertice_index + 13 + 5 * 19 + vertex_indexs[j + 7]);
            triangles.Add(vertice_index + 13 + 5 * 19 + vertex_indexs[j + 8]);
            triangles.Add(vertice_index + 7);
        }

        // The top left fenestration is connected to the outline.
        vertex_indexs = new List<int>()
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
            triangles.Add(vertice_index + vertex_indexs[i]);
        }

        // The top right fenestration is connected to the outline.
        vertex_indexs = new List<int>()
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
            triangles.Add(vertice_index + vertex_indexs[i]);
        }

        // Triangles that fill the gap at the top of the leaf are generated.
        vertex_indexs = new List<int>()
        {
            0, 31, 12,
            31, 202, 198,
            189, 201, 188,
            188, 201, 194,
            198, 202, 197,
        };

        for (int i = 0; i < 15; i++)
        {
            triangles.Add(vertice_index + vertex_indexs[i]);
        }

        // Triangles that fill the gap at the bottom of the leaf are generated.
        vertex_indexs = new List<int>()
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
            triangles.Add(vertice_index + vertex_indexs[i]);
        }

        // If the generated mesh is meant to be the bask side all triangles have to be flipped. This is easily done by swapping places of any two vertices in a triangle.
        if (invert)
        {
            int new_triangles = triangles.Count;
            for (int t = triangle_index; t < new_triangles; t += 3)
            {
                int temp = triangles[t];
                triangles[t] = triangles[t + 1];
                triangles[t + 1] = temp;
            }
        }
    }

    // Applies an angle and then a rotation on top of that to an identity matrix and returns the resulting rotation matrix.
    Matrix Rotate(float angle, float rotation)
    {
        Matrix rotated;

        // The x vector is angled and then rotated.
        Vector3 x_angled = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
        rotated.x = new Vector3(Mathf.Cos(rotation) * x_angled.x, x_angled.y, Mathf.Sin(rotation) * x_angled.x);

        // The y vector is angled and then rotated.
        Vector3 y_angled = new Vector3(-Mathf.Sin(angle), Mathf.Cos(angle), 0);
        rotated.y = new Vector3(Mathf.Cos(rotation) * y_angled.x, y_angled.y, Mathf.Sin(rotation) * y_angled.x);

        // The z vector stays the same when angled and therefore only needs to be rotated.
        rotated.z = new Vector3(-Mathf.Sin(rotation), 0, Mathf.Cos(rotation));

        return rotated;
    }

    // This function does the same as the above but for any matrix, not just the identity. It turns out to be a much more complicated operation and I spent hours on it before learning that we never need to perform this operation twice.
    // It sort of works but causes minor errors that become apparent after multiple operations. The matix stop being an orthonormal set. This could be due to floating point errors, though that seems unlikely.
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

    // Creates positions corresponting to the corners of an octagon.
    List<Vector3> OctagonPositions(Vector3 pos_center, Matrix rotation, float radius)
    {
        // The length of half the side of an octagon given its radius i calculated in advance. 
        float side_half = radius / (1 + 2 / Mathf.Sqrt(2));

        // All points of an octagon, with respect to a starting position and the rotated matrix, are calculated and returned in a list.
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

    // Creates potisions for the base of a petiole.
    List<Vector3> BasePositions(Vector3 pos_center, Matrix rotation, float radius)
    {
        // The length to the corner of an octagon given its radius i calculated in advance. (This calculation is incorrect. It was stolen from the function above and tweaked slightly. I could find the correct value but who really cares?)
        float corner = 1.6f * radius / (1 + 2 / Mathf.Sqrt(2));

        // The positions of the base are calculated and returned in a list. The base has a similar pattern when viewved from above and below. Each pair in this list represents the upper and lower position that match in said pattern.
        return new List<Vector3>
        {
            pos_center + rotation.y * radius * -0.5f,
            pos_center + rotation.y * radius * -1,

            pos_center + rotation.z * +radius + rotation.y * radius * 0.5f,
            pos_center + rotation.z * +radius * 1.2f + rotation.y * -radius * 0.8f,

            pos_center + rotation.x * -corner + rotation.z * +corner + rotation.y * radius * 0.5f,
            pos_center + rotation.x * -corner * 1.2f + rotation.z * +corner * 1.2f + rotation.y * -radius * 0.8f,

            pos_center + rotation.x * -radius + rotation.y * radius * 0.5f,
            pos_center + rotation.x * -radius * 1.2f + rotation.y * -radius * 0.8f,

            pos_center + rotation.x * -corner + rotation.z * -corner + rotation.y * radius * 0.5f,
            pos_center + rotation.x * -corner * 1.2f + rotation.z * -corner * 1.2f + rotation.y * -radius * 0.8f,

            pos_center + rotation.z * -radius + rotation.y * radius * 0.5f,
            pos_center + rotation.z * -radius * 1.2f + rotation.y * -radius * 0.8f,
        };
    }

    // Creates potisions for the middle of a petiole.
    List<Vector3> MiddlePositions(Vector3 pos_center, Matrix rotation, float thickness)
    {
        // A fifth of a single revolution in radians is calculated in advance.
        float radians = 72 * Mathf.Deg2Rad;

        // The middle of a petiole is a bit thinner compared to the other parts and so, the provided thickness is lowered slightly.
        thickness *= 0.8f;

        // The positions of the middle are calculated and returned in a list. In essence it's a pentagon with a point inside of it.
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

    // Creates potisions for the end of a petiole.
    List<Vector3> EndPositions(Vector3 pos_center, Matrix rotation, float thickness)
    {
        // The positions for the end are manually determined and returned in a list.
        return new List<Vector3>
        {
            pos_center + rotation.y * thickness * 0.4f,
            pos_center + rotation.y * thickness * 0.6f + rotation.z * thickness,
            pos_center - rotation.y * thickness * 0.1f + rotation.z * thickness * 0.5f,
            pos_center - rotation.y * thickness * 0.1f - rotation.z * thickness * 0.5f,
            pos_center + rotation.y * thickness * 0.6f - rotation.z * thickness,
        };
    }

    // Creates the positions for the vertices of the outline of the leaf. Also calls the function to create 10 leaf fenestrations at predetermined locations.
    List<List<Vector3>> LeafPositions(Vector3 pos_center, Matrix rotation, float width, float height, float fenestration_length, float fenestration_thickness, float[] holes)
    {
        List<List<Vector3>> positions = new();
        List<Vector3> positions_perimeter = new();

        // The postions for the perimeter are manually determined.
        positions_perimeter.Add(new Vector3(0, height * +0.00f, width * +0.03f));
        positions_perimeter.Add(new Vector3(0, height * +0.18f, width * +0.10f));
        positions_perimeter.Add(new Vector3(0, height * +0.20f, width * +0.40f));
        positions_perimeter.Add(new Vector3(0, height * +0.18f, width * +0.70f));
        positions_perimeter.Add(new Vector3(0, height * +0.15f, width * +0.87f));

        positions_perimeter.Add(new Vector3(0, height * -0.97f, width * +0.07f));
        positions_perimeter.Add(new Vector3(0, height * -1.00f, width * +0.00f));
        positions_perimeter.Add(new Vector3(0, height * -0.97f, width * -0.07f));

        positions_perimeter.Add(new Vector3(0, height * +0.10f, width * -0.90f));
        positions_perimeter.Add(new Vector3(0, height * +0.18f, width * -0.70f));
        positions_perimeter.Add(new Vector3(0, height * +0.20f, width * -0.40f));
        positions_perimeter.Add(new Vector3(0, height * +0.18f, width * -0.10f));
        positions_perimeter.Add(new Vector3(0, height * +0.00f, width * -0.03f));

        // The perimeter positions are bent, rotated, and added to the final list.
        positions_perimeter = BendPositions(positions_perimeter, pos_center, rotation, width, height);
        positions.Add(positions_perimeter);

        // The postions for the fenestrations are manually determined. Each has a fixed angle. The shear feature is unused as it wasn't necessary.
        positions.Add(BendPositions(LeafFenestration(new Vector3(0, height * +0.08f, width * +1.00f), fenestration_length, fenestration_thickness, 25, 0, holes[1]), pos_center, rotation, width, height));
        positions.Add(BendPositions(LeafFenestration(new Vector3(0, height * -0.20f, width * +1.00f), fenestration_length, fenestration_thickness, -5, 0, holes[3]), pos_center, rotation, width, height));
        positions.Add(BendPositions(LeafFenestration(new Vector3(0, height * -0.40f, width * +0.90f), fenestration_length, fenestration_thickness, -20, 0, holes[5]), pos_center, rotation, width, height));
        positions.Add(BendPositions(LeafFenestration(new Vector3(0, height * -0.62f, width * +0.75f), fenestration_length, fenestration_thickness, -35, 0, holes[7]), pos_center, rotation, width, height));
        positions.Add(BendPositions(LeafFenestration(new Vector3(0, height * -0.88f, width * +0.40f), fenestration_length, fenestration_thickness, -65, 0, holes[9]), pos_center, rotation, width, height));
        
        positions.Add(BendPositions(LeafFenestration(new Vector3(0, height * -0.92f, width * -0.35f), fenestration_length, fenestration_thickness, 245, 0, holes[8]), pos_center, rotation, width, height));
        positions.Add(BendPositions(LeafFenestration(new Vector3(0, height * -0.68f, width * -0.70f), fenestration_length, fenestration_thickness, 215, 0, holes[6]), pos_center, rotation, width, height));
        positions.Add(BendPositions(LeafFenestration(new Vector3(0, height * -0.45f, width * -0.87f), fenestration_length, fenestration_thickness, 200, 0, holes[4]), pos_center, rotation, width, height));
        positions.Add(BendPositions(LeafFenestration(new Vector3(0, height * -0.25f, width * -0.95f), fenestration_length, fenestration_thickness, 185, 0, holes[2]), pos_center, rotation, width, height));
        positions.Add(BendPositions(LeafFenestration(new Vector3(0, height * -0.00f, width * -1.00f), fenestration_length, fenestration_thickness, 160, 0, holes[0]), pos_center, rotation, width, height));

        return positions;
    }

    // Creates the positions for the vertices of a single leaf fenestration. The shear property is not utilized but could in theory adjust the vertices at the edge of the fenestration to make them align with the outline of the leaf.
    List<Vector3> LeafFenestration(Vector3 pos_edge, float fenestration_length, float thickness, float angle, float shear, float hole_size)
    {
        List<Vector3> positions = new List<Vector3>();

        // For simplicity, the sine, cosine, and tangent of the andle are calculated in advance.
        float sin = Mathf.Sin(angle * Mathf.Deg2Rad);
        float cos = Mathf.Cos(angle * Mathf.Deg2Rad);
        float tan = Mathf.Tan(angle * Mathf.Deg2Rad);

        // The actial length of the fenestration is calculated in advance.
        float length = pos_edge.z / cos * fenestration_length;

        // The position of the hole between the central spine of the leaf and the fenestration is calculated in advance.
        Vector3 pos_hole = new(0, (pos_edge.y - tan * pos_edge.z + (pos_edge.y - sin * length)) / 2, (pos_edge.z - cos * length) / 2);

        // The postions for the edge of the fenestration are calcualted.
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

        // The postions for the hole are calcualted.
        positions.Add(pos_hole + hole_size * new Vector3(0, +thickness * cos * 0.33f + thickness * sin * 1.60f, -thickness * sin * 0.33f + thickness * cos * 1.60f));
        positions.Add(pos_hole + hole_size * new Vector3(0, +thickness * cos * 1.00f + thickness * sin * 1.00f, -thickness * sin * 1.00f + thickness * cos * 1.00f));
        positions.Add(pos_hole + hole_size * new Vector3(0, +thickness * cos * 1.00f - thickness * sin * 1.00f, -thickness * sin * 1.00f - thickness * cos * 1.00f));
        positions.Add(pos_hole + hole_size * new Vector3(0, +thickness * cos * 0.33f - thickness * sin * 1.60f, -thickness * sin * 0.33f - thickness * cos * 1.60f));

        positions.Add(pos_hole + hole_size * new Vector3(0, -thickness * cos * 0.33f - thickness * sin * 1.60f, +thickness * sin * 0.33f - thickness * cos * 1.60f));
        positions.Add(pos_hole + hole_size * new Vector3(0, -thickness * cos * 1.00f - thickness * sin * 1.00f, +thickness * sin * 1.00f - thickness * cos * 1.00f));
        positions.Add(pos_hole + hole_size * new Vector3(0, -thickness * cos * 1.00f + thickness * sin * 1.00f, +thickness * sin * 1.00f + thickness * cos * 1.00f));
        positions.Add(pos_hole + hole_size * new Vector3(0, -thickness * cos * 0.33f + thickness * sin * 1.60f, +thickness * sin * 0.33f + thickness * cos * 1.60f));

        // Finally, a single position intersecting the central spine of the leaf and the direction of the fenestration is calcualted.
        positions.Add(new Vector3(0, pos_edge.y - tan * pos_edge.z, 0));

        return positions;
    }

    // Moves leaf vertices in the x axis to create the curvature of a leaf using a 2nd degree polynomial function. Also applies angle and roation.
    List<Vector3> BendPositions(List<Vector3> positions, Vector3 pos_center, Matrix rotation, float width, float height)
    {
        List<Vector3> positions_bent = new();

        foreach (Vector3 position in positions)
        {
            // The distance from the center of the leaf is determined by abstracting the leaf to an oval shape with the same width and height. Different ovals are used depending in if the y position is positive or not.
            float distance_from_center;

            if (position.y > 0)
            {
                distance_from_center = Mathf.Pow(position.z / width, 2) + Mathf.Pow(position.y / height, 2) / 0.04f;
            }
            else
            {
                distance_from_center = Mathf.Pow(position.z / width, 2) + Mathf.Pow(position.y / height, 2);
            }

            // How much the x position is moved depends on how far away from the center the position is according to this function: [-m * d^2 + 0.8 * m * d] where m is the magnitude and d is the distance from center.
            // The magnitude is used to adjust how pronounced the curvature should be.
            float magnitude = 0.4f;
            float depth = -magnitude * Mathf.Pow(distance_from_center, 2) + 0.8f * magnitude * distance_from_center;

            // The resulting depth is scaled to the size of the leaf.
            depth *= (height + width) / 2;

            // The postions depth is adjusted and rotation is applied to it. 
            positions_bent.Add(pos_center + rotation.z * position.z + rotation.y * position.y + rotation.x * depth);
        }

        return positions_bent;
    }

    // Generates a cube mesh for debugging. It's unused in the final version. Ulike other functions in this script, the positions and mesh data are both created in this one function.
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

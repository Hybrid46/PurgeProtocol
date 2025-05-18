using UnityEngine;
using System.Collections.Generic;

public class WallMeshGenerator : MonoBehaviour
{
    private float wallHeight = 2f;

    public void GenerateWallMesh(bool[,] map)
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = CreateWallMesh(map);
    }

    Mesh CreateWallMesh(bool[,] map)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        Dictionary<Vector3, int> vertexMap = new Dictionary<Vector3, int>();

        for (int x = 0; x < map.GetLength(0); x++)
        {
            for (int z = 0; z < map.GetLength(1); z++)
            {
                if (!map[x, z]) // This is a wall cell
                {
                    // Check adjacent cells for floor
                    CheckNorthFace(x, z, map, vertices, triangles, vertexMap);
                    CheckSouthFace(x, z, map, vertices, triangles, vertexMap);
                    CheckEastFace(x, z, map, vertices, triangles, vertexMap);
                    CheckWestFace(x, z, map, vertices, triangles, vertexMap);
                }
            }
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    void CheckNorthFace(int x, int z, bool[,] map, List<Vector3> vertices, List<int> triangles, Dictionary<Vector3, int> vertexMap)
    {
        // North neighbor exists and is floor
        if (z < map.GetLength(1) - 1 && map[x, z + 1])
        {
            AddWallQuad(
                new Vector3(x + 1, 0, z + 1),  // Bottom-right
                new Vector3(x, 0, z + 1),       // Bottom-left
                new Vector3(x, wallHeight, z + 1),    // Top-left
                new Vector3(x + 1, wallHeight, z + 1),  // Top-right
                vertices, triangles, vertexMap
            );
        }
    }

    void CheckSouthFace(int x, int z, bool[,] map, List<Vector3> vertices, List<int> triangles, Dictionary<Vector3, int> vertexMap)
    {
        // South neighbor exists and is floor
        if (z > 0 && map[x, z - 1])
        {
            AddWallQuad(
                new Vector3(x, 0, z),       // Bottom-left
                new Vector3(x + 1, 0, z),   // Bottom-right
                new Vector3(x + 1, wallHeight, z),    // Top-right
                new Vector3(x, wallHeight, z), // Top-left
                vertices, triangles, vertexMap
            );
        }
    }

    void CheckEastFace(int x, int z, bool[,] map, List<Vector3> vertices, List<int> triangles, Dictionary<Vector3, int> vertexMap)
    {
        // East neighbor exists and is floor
        if (x < map.GetLength(0) - 1 && map[x + 1, z])
        {
            AddWallQuad(
                new Vector3(x + 1, 0, z),
                new Vector3(x + 1, 0, z + 1),
                new Vector3(x + 1, wallHeight, z + 1),
                new Vector3(x + 1, wallHeight, z),
                vertices, triangles, vertexMap
            );
        }
    }

    void CheckWestFace(int x, int z, bool[,] map, List<Vector3> vertices, List<int> triangles, Dictionary<Vector3, int> vertexMap)
    {
        // West neighbor exists and is floor
        if (x > 0 && map[x - 1, z])
        {
            AddWallQuad(
                new Vector3(x, 0, z + 1),
                new Vector3(x, 0, z),
                new Vector3(x, wallHeight, z),
                new Vector3(x, wallHeight, z + 1),
                vertices, triangles, vertexMap
            );
        }
    }

    void AddWallQuad(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3,
                    List<Vector3> vertices, List<int> triangles,
                    Dictionary<Vector3, int> vertexMap)
    {
        int[] indices = new int[4];
        Vector3[] points = { v0, v1, v2, v3 };

        for (int i = 0; i < 4; i++)
        {
            if (!vertexMap.TryGetValue(points[i], out indices[i]))
            {
                indices[i] = vertices.Count;
                vertexMap.Add(points[i], indices[i]);
                vertices.Add(points[i]);
            }
        }

        // Add two triangles with correct winding order
        triangles.Add(indices[0]);
        triangles.Add(indices[2]);
        triangles.Add(indices[1]);

        triangles.Add(indices[0]);
        triangles.Add(indices[3]);
        triangles.Add(indices[2]);
    }
}
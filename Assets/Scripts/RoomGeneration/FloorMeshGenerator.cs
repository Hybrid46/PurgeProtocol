using UnityEngine;
using System.Collections.Generic;

public class FloorMeshGenerator : MonoBehaviour
{
    public void GenerateFloorMesh(bool[,] map)
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = CreateFloorMesh(map);
    }

    Mesh CreateFloorMesh(bool[,] map)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        Dictionary<Vector2Int, int> vertexMap = new Dictionary<Vector2Int, int>();

        for (int x = 0; x < map.GetLength(0); x++)
        {
            for (int y = 0; y < map.GetLength(1); y++)
            {
                if (map[x, y])
                {
                    // Get or create vertices for this floor tile
                    Vector2Int[] corners = {
                        new Vector2Int(x, y),     // Bottom-left
                        new Vector2Int(x + 1, y), // Bottom-right
                        new Vector2Int(x + 1, y + 1), // Top-right
                        new Vector2Int(x, y + 1)  // Top-left
                    };

                    int[] vertexIndices = new int[4];

                    // Process all four corners
                    for (int i = 0; i < 4; i++)
                    {
                        if (!vertexMap.TryGetValue(corners[i], out vertexIndices[i]))
                        {
                            vertexIndices[i] = vertices.Count;
                            vertexMap.Add(corners[i], vertexIndices[i]);
                            vertices.Add(new Vector3(corners[i].x, 0, corners[i].y));
                        }
                    }

                    // Add two triangles for the floor quad
                    triangles.Add(vertexIndices[0]);
                    triangles.Add(vertexIndices[2]);
                    triangles.Add(vertexIndices[1]);

                    triangles.Add(vertexIndices[0]);
                    triangles.Add(vertexIndices[3]);
                    triangles.Add(vertexIndices[2]);
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
}
using UnityEngine;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using System;
using Unity.VisualScripting;

public class RoomGenerator : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridWidth = 50;
    public int gridHeight = 50;

    [Header("Room Settings")]
    public int minRoomSizeX = 2;
    public int maxRoomSizeX = 10;
    public int minRoomSizeY = 2;
    public int maxRoomSizeY = 10;

    [SerializeField] private bool[,] grid;
    [SerializeField] private List<Room> rooms = new List<Room>();

    private HashSet<Vector2Int> openSet;
    private HashSet<Vector2Int> walls = new HashSet<Vector2Int>();

    [Serializable]
    private class Room
    {
        public HashSet<Vector2Int> coords;
        public HashSet<Vector2Int> walls;
        public Vector2Int startCoord;
        public Color color;

        public Room(Vector2Int startPosition)
        {
            walls = new HashSet<Vector2Int>();
            coords = new HashSet<Vector2Int> { startPosition };
            this.startCoord = startPosition;
            color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 0.4f);
        }
    }

    void Start()
    {
        InitializeGrid();
        GenerateRooms();
    }

    void InitializeGrid()
    {
        grid = new bool[gridWidth, gridHeight];
        openSet = new HashSet<Vector2Int>(gridWidth * gridHeight);

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                grid[x, y] = false;
                openSet.Add(new Vector2Int(x, y));
            }
        }
    }

    void GenerateRooms()
    {
        rooms = new List<Room>();

        while (openSet.Count > 0)
        {
            Vector2Int coord = GetFirstElementFromHashSet(openSet);
            int width = Random.Range(minRoomSizeX, maxRoomSizeX + 1);
            int height = Random.Range(minRoomSizeY, maxRoomSizeY + 1);
            Room room = new Room(coord);

            rooms.Add(room);
            grid[coord.x, coord.y] = true;
            openSet.Remove(coord);

            ExpandRoom(coord, width, height, room);
        }
    }

    private void ExpandRoom(Vector2Int coord, int width, int height, Room room)
    {
        List<Vector2Int> offsetDirections = GetOffsetDirections();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int offsetedCoord = coord + new Vector2Int(x, y);

                //bounds check
                if (!IsWithinGrid(offsetedCoord)) continue;
                //occupancy check
                if (grid[offsetedCoord.x, offsetedCoord.y]) continue;

                room.coords.Add(offsetedCoord);
                grid[offsetedCoord.x, offsetedCoord.y] = true;
                openSet.Remove(offsetedCoord);
            }
        }

        foreach (Vector2Int roomCoord in room.coords)
        {
            //walls

            //Is room edge?
            //if (x == 0 || y == 0 || x == width - 1 || y == height - 1) -> if roomcord has false neighbour or something ....
            {
                //Add walls in all 8 directions
                foreach (Vector2Int offsetDirection in offsetDirections)
                {
                    Vector2Int wallCoord = roomCoord + offsetDirection;

                    //bounds check
                    if (!IsWithinGrid(wallCoord)) continue;
                    //occupancy check
                    if (grid[wallCoord.x, wallCoord.y]) continue;

                    room.walls.Add(wallCoord);
                    walls.Add(wallCoord);
                    grid[wallCoord.x, wallCoord.y] = true;
                    openSet.Remove(wallCoord);
                }
            }
        }
    }

    private List<Vector2Int> GetOffsetDirections()
    {
        List<Vector2Int> offsetDirections = new List<Vector2Int>(8);

        for (int y = -1; y <= 1; y++)
        {
            for (int x = -1; x <= 1; x++)
            {
                if (x == 0 && y == 0) continue;
                offsetDirections.Add(new Vector2Int(x, y));
            }
        }

        return offsetDirections;
    }

    private bool IsWithinGrid(Vector2Int pos) => pos.x >= 0 && pos.x < gridWidth && pos.y >= 0 && pos.y < gridHeight;

    private bool IsGridEdge(Vector2Int pos) => pos.x == 0 || pos.x == gridWidth - 1 || pos.y == 0 || pos.y == gridHeight - 1;

    private T GetFirstElementFromHashSet<T>(HashSet<T> hashSet)
    {
        foreach (T t in hashSet)
        {
            return t;
        }

        return default;
    }

    void OnDrawGizmos()
    {
        if (rooms == null) return;

        foreach (Room room in rooms)
        {
            Gizmos.color = room.color;

            foreach (Vector2Int pos in room.coords)
            {
                Gizmos.DrawCube(new Vector3(pos.x, 0f, pos.y), Vector3.one);
                Gizmos.DrawWireCube(new Vector3(pos.x, 0f, pos.y), Vector3.one);
            }

            Gizmos.color = Color.white;
            Gizmos.DrawSphere(new Vector3(room.startCoord.x, 0f, room.startCoord.y), 0.25f);
        }
    }
}
using UnityEngine;
using System.Collections.Generic;
using Random = UnityEngine.Random;

public class RoomGenerator : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridWidth = 50;
    public int gridHeight = 50;

    [Header("Room Settings")]
    public int partitionsX = 10;
    public int partitionsY = 10;

    [SerializeField] private bool[,] grid;
    private List<Room> rooms = new List<Room>();

    [SerializeField] private Color[] roomColors;
    private HashSet<Vector2Int> openSet;

    private class Room
    {
        public HashSet<Vector2Int> roomPositions;
        public Vector2Int startCoord;

        public Room(Vector2Int roomStartPosition)
        {
            roomPositions = new HashSet<Vector2Int> { roomStartPosition };
            this.startCoord = roomStartPosition;
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
        int roomCount = partitionsX * partitionsY;

        roomColors = new Color[roomCount];
        for (int i = 0; i < roomCount; i++)
        {
            roomColors[i] = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 0.4f);
        }

        List<Vector2Int> roomStartCoordinates = RandomPartitionSampling(grid, partitionsX, partitionsY);

        if (roomStartCoordinates.Count == 0)
        {
            Debug.LogError("Failed to generate room start coordinates");
            return;
        }

        rooms = new List<Room>(roomStartCoordinates.Count);

        foreach (Vector2Int coord in roomStartCoordinates)
        {
            rooms.Add(new Room(coord));
            grid[coord.x, coord.y] = true;
            openSet.Remove(coord);
        }

        ExpandRooms();
    }

    private List<Vector2Int> RandomPartitionSampling(bool[,] grid, int partitionsX, int partitionsY)
    {
        List<Vector2Int> randomStartCoords = new List<Vector2Int>();

        int remainderX = grid.GetLength(0) % partitionsX;
        int remainderY = grid.GetLength(1) % partitionsY;
        int baseWidth = grid.GetLength(0) / partitionsX;
        int baseHeight = grid.GetLength(1) / partitionsY;

        for (int xPart = 0; xPart < partitionsX; xPart++)
        {
            int startX = xPart * baseWidth + Mathf.Min(xPart, remainderX);
            int width = baseWidth + (xPart < remainderX ? 1 : 0);
            int endX = startX + width - 1;

            for (int yPart = 0; yPart < partitionsY; yPart++)
            {
                int startY = yPart * baseHeight + Mathf.Min(yPart, remainderY);
                int height = baseHeight + (yPart < remainderY ? 1 : 0);
                int endY = startY + height - 1;

                int randomX = Random.Range(startX, endX + 1);
                int randomY = Random.Range(startY, endY + 1);

                randomStartCoords.Add(new Vector2Int(randomX, randomY));
            }
        }

        return randomStartCoords;
    }

    private void ExpandRooms()
    {
        bool expandedAny;
        do
        {
            expandedAny = false;

            List<Room> roomsToProcess = new List<Room>(rooms);
            Shuffle(roomsToProcess);

            foreach (Room room in roomsToProcess)
            {
                List<Vector2Int> currentPositions = new List<Vector2Int>(room.roomPositions);
                //Shuffle(currentPositions);

                foreach (Vector2Int pos in currentPositions)
                {
                    List<Vector2Int> directions = new List<Vector2Int>
                    {
                        new Vector2Int(1, 0),
                        new Vector2Int(-1, 0),
                        new Vector2Int(0, 1),
                        new Vector2Int(0, -1)
                    };
                    //Shuffle(directions);

                    foreach (Vector2Int dir in directions)
                    {
                        Vector2Int newPos = pos + dir;
                        if (IsWithinGrid(newPos) && openSet.Contains(newPos))
                        {
                            room.roomPositions.Add(newPos);
                            openSet.Remove(newPos);
                            grid[newPos.x, newPos.y] = true;
                            expandedAny = true;
                            break;
                        }
                    }

                    if (expandedAny)
                        break;
                }

                if (expandedAny)
                    break;
            }
        } while (expandedAny);
    }

    private bool IsWithinGrid(Vector2Int pos) => pos.x >= 0 && pos.x < gridWidth && pos.y >= 0 && pos.y < gridHeight;

    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    void OnDrawGizmos()
    {
        if (rooms == null) return;

        int index = 0;
        foreach (Room room in rooms)
        {
            if (index >= roomColors.Length)
                break;

            Gizmos.color = roomColors[index];
            index++;

            foreach (Vector2Int pos in room.roomPositions)
            {
                Gizmos.DrawCube(new Vector3(pos.x, 0f, pos.y), Vector3.one);
                Gizmos.DrawWireCube(new Vector3(pos.x, 0f, pos.y), Vector3.one);
            }

            Gizmos.color = Color.white;
            Gizmos.DrawSphere(new Vector3(room.startCoord.x, 2f, room.startCoord.y), 0.5f);
        }
    }
}
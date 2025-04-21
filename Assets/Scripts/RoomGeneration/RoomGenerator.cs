using UnityEngine;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using System;

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
    [Range(0f, 1f)] public float extraDoorChance = 0.0f;

    private bool[,] grid;
    private List<Room> rooms = new List<Room>();

    private HashSet<Vector2Int> openSet;
    private HashSet<Vector2Int> wallSet = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> roomSet = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> doorSet = new HashSet<Vector2Int>();

    private HashSet<Vector2Int> removedDoubleWalls;

    //TODO store on grid
    private Dictionary<Vector2Int, Room> coordToRoomMap;

    private class Room
    {
        public HashSet<Vector2Int> coords;
        public HashSet<Vector2Int> edgeCoords;
        public HashSet<Vector2Int> walls;
        public HashSet<Vector2Int> doors;
        public Vector2Int startCoord { get; private set; }
        public Color color { get; private set; }

        public Room(Vector2Int startPosition)
        {
            walls = new HashSet<Vector2Int>();
            doors = new HashSet<Vector2Int>();
            coords = new HashSet<Vector2Int> { startPosition };
            this.startCoord = startPosition;
            color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 0.4f);
        }

        public void SetEdges(RoomGenerator roomGenerator)
        {
            edgeCoords = new HashSet<Vector2Int>();

            foreach (Vector2Int coord in coords)
            {
                if (IsEdge(roomGenerator, coord)) edgeCoords.Add(coord);
            }
        }

        private bool IsEdge(RoomGenerator roomGenerator, Vector2Int coord)
        {
            foreach (Vector2Int offset in roomGenerator.GetOffsetDirections()) //TODO should be cardinal?
            {
                Vector2Int roomCoord = coord + offset;
                if (!roomGenerator.IsWithinGrid(roomCoord)) return true;
                if (!coords.Contains(roomCoord)) return true;
            }

            return false;
        }
    }

    private class Door
    {
        public Vector2Int position;
        public Room roomA;
        public Room roomB;

        public Door(Vector2Int pos, Room a, Room b)
        {
            position = pos;
            roomA = a;
            roomB = b;
        }
    }

    void Start()
    {
        InitializeGrid();
        GenerateRooms();
        foreach (Room room in rooms) room.SetEdges(this);

        //removing doulbe walls
        AttachDoubleWallsToRooms();
        foreach (Room room in rooms) room.SetEdges(this);

        //Doors
        //MapCoordsToRooms();
        GenerateDoors();
    }

    void InitializeGrid()
    {
        grid = new bool[gridWidth, gridHeight];
        openSet = new HashSet<Vector2Int>(gridWidth * gridHeight);

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                //is edge? -> generate a border wall
                if (x == 0 || y == 0 || x == gridWidth - 1 || y == gridHeight - 1)
                {
                    grid[x, y] = true;
                    wallSet.Add(new Vector2Int(x, y));
                }
                else
                {
                    grid[x, y] = false;
                    openSet.Add(new Vector2Int(x, y));
                }
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

            roomSet.Add(coord);
            rooms.Add(room);
            grid[coord.x, coord.y] = true;
            openSet.Remove(coord);

            ExpandRoom(coord, width, height, room);
            GenerateWallsAroundRoom(room);
        }
    }

    private void ExpandRoom(Vector2Int coord, int width, int height, Room room)
    {
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
                roomSet.Add(offsetedCoord);
                grid[offsetedCoord.x, offsetedCoord.y] = true;
                openSet.Remove(offsetedCoord);
            }
        }
    }

    private void GenerateWallsAroundRoom(Room room)
    {
        List<Vector2Int> offsetDirections = GetOffsetDirections();

        foreach (Vector2Int roomCoord in room.coords)
        {
            foreach (Vector2Int offsetDirection in offsetDirections)
            {
                Vector2Int wallCoord = roomCoord + offsetDirection;

                //bounds check
                if (!IsWithinGrid(wallCoord)) continue;
                //occupancy check
                if (grid[wallCoord.x, wallCoord.y]) continue;

                room.walls.Add(wallCoord);
                wallSet.Add(wallCoord);
                grid[wallCoord.x, wallCoord.y] = true;
                openSet.Remove(wallCoord);
            }
        }
    }

    private void AttachDoubleWallsToRooms()
    {
        removedDoubleWalls = new HashSet<Vector2Int>();

        foreach (Room room in rooms)
        {
            foreach (Vector2Int edge in room.edgeCoords)
            {
                foreach (Vector2Int dir in GetCardinalDirections())
                {
                    Vector2Int singleOffset = edge + dir;
                    Vector2Int doubleOffset = edge + (dir * 2);

                    if (wallSet.Contains(singleOffset) && wallSet.Contains(doubleOffset)) //is double wall?
                    {
                        HashSet<Vector2Int> wallNeighbours = new HashSet<Vector2Int>();
                        HashSet<Room> roomNeighbours = new HashSet<Room>();

                        foreach (Vector2Int singleDir in GetOffsetDirections())
                        {
                            Vector2Int singleOffsetNeighbour = singleOffset + singleDir;

                            if (wallSet.Contains(singleOffsetNeighbour))
                            {
                                wallNeighbours.Add(singleOffsetNeighbour);
                            }

                            if (roomSet.Contains(singleOffsetNeighbour))
                            {
                                roomNeighbours.Add(CoordinateToRoom(singleOffsetNeighbour));
                            }
                        }

                        bool isAttachable = roomNeighbours.Count == 1;

                        if (isAttachable)
                        {
                            room.walls.Remove(singleOffset);
                            foreach (Vector2Int wall in wallNeighbours) room.walls.Add(wall);
                            room.coords.Add(singleOffset);
                            wallSet.Remove(singleOffset);
                            roomSet.Add(singleOffset);

                            removedDoubleWalls.Add(singleOffset);
                        }

                        break;
                    }
                }
            }
        }
    }

    private void MapCoordsToRooms()
    {
        coordToRoomMap = new Dictionary<Vector2Int, Room>(roomSet.Count);
        int roomAreas = 0;

        foreach (Room room in rooms)
        {
            roomAreas += room.coords.Count;

            foreach (Vector2Int coord in room.coords)
            {
                coordToRoomMap[coord] = room;
            }
        }

        if (roomAreas != roomSet.Count) Debug.LogError($"Room set({roomSet.Count}) and room coords({roomAreas}) not synced!");
    }

    private void GenerateDoors()
    {
        List<Door> doors = CollectDoors();
        ShuffleList(doors);

        UnionFind<Room> unionFind = new UnionFind<Room>(rooms);
        List<Door> selectedDoors = new List<Door>();

        // Minimum spanning tree for connectivity
        for (int i = 0; i < doors.Count; i++)
        {
            Door door = doors[i];
            if (!unionFind.AreConnected(door.roomA, door.roomB))
            {
                unionFind.Union(door.roomA, door.roomB);
                selectedDoors.Add(door);
                if (unionFind.GetNumberOfSets() == 1) break;
            }
        }

        // Random extra connections
        int remaining = doors.Count - selectedDoors.Count;
        int extraDoors = Mathf.FloorToInt(remaining * extraDoorChance); //TODO doors shouldn't be next to each other
        for (int i = 0; i < extraDoors; i++)
        {
            int randomIndex = Random.Range(selectedDoors.Count, doors.Count);
            selectedDoors.Add(doors[randomIndex]);
        }

        // Create door openings
        for (int i = 0; i < selectedDoors.Count; i++)
        {
            Vector2Int doorPos = selectedDoors[i].position;
            doorSet.Add(doorPos);
            wallSet.Remove(doorPos);

            for (int r = 0; r < rooms.Count; r++)
            {
                Room room = rooms[r];
                if (room.walls.Contains(doorPos))
                {
                    room.walls.Remove(doorPos);
                    room.doors.Add(doorPos);
                }
            }
        }
    }

    private List<Door> CollectDoors()
    {
        List<Door> doors = new List<Door>();
        HashSet<Vector2Int> processedWalls = new HashSet<Vector2Int>();

        foreach (Vector2Int wall in wallSet)
        {
            if (processedWalls.Contains(wall)) continue;

            List<Room> adjacentRooms = new List<Room>();
            foreach (Vector2Int dir in GetCardinalDirections())
            {
                Vector2Int neighbor = wall + dir;
                Room room = CoordinateToRoom(neighbor);

                if (room != null && !adjacentRooms.Contains(room))
                {
                    adjacentRooms.Add(room);
                }
            }

            if (adjacentRooms.Count >= 2)
            {
                for (int i = 0; i < adjacentRooms.Count; i++)
                {
                    for (int j = i + 1; j < adjacentRooms.Count; j++)
                    {
                        doors.Add(new Door(wall, adjacentRooms[i], adjacentRooms[j]));
                    }
                }

                processedWalls.Add(wall);
            }
        }
        return doors;
    }

    // Union-Find implementation
    public class UnionFind<T>
    {
        private Dictionary<T, T> parent;
        private Dictionary<T, int> rank;
        private int numSets;

        public UnionFind(IEnumerable<T> elements)
        {
            parent = new Dictionary<T, T>();
            rank = new Dictionary<T, int>();
            numSets = 0;

            foreach (T element in elements) MakeSet(element);
        }

        public void MakeSet(T element)
        {
            if (!parent.ContainsKey(element))
            {
                parent[element] = element;
                rank[element] = 0;
                numSets++;
            }
        }

        public T Find(T element)
        {
            if (!parent[element].Equals(element))
            {
                parent[element] = Find(parent[element]);
            }

            return parent[element];
        }

        public void Union(T a, T b)
        {
            T rootA = Find(a);
            T rootB = Find(b);

            if (rootA.Equals(rootB)) return;

            if (rank[rootA] < rank[rootB])
            {
                parent[rootA] = rootB;
            }
            else
            {
                parent[rootB] = rootA;
                if (rank[rootA] == rank[rootB]) rank[rootA]++;
            }
            numSets--;
        }

        public bool AreConnected(T a, T b) => Find(a).Equals(Find(b));
        public int GetNumberOfSets() => numSets;
    }

    //Helper methods

    private void ShuffleList<T>(List<T> list)
    {
        int n = list.Count;
        for (int i = 0; i < n; i++)
        {
            int r = i + Random.Range(0, n - i);
            T value = list[r];
            list[r] = list[i];
            list[i] = value;
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

    private List<Vector2Int> GetCardinalDirections()
    {
        List<Vector2Int> offsetDirections = new List<Vector2Int>(4)
        {
            new Vector2Int(-1, 0),
            new Vector2Int(1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };

        return offsetDirections;
    }

    private bool IsWithinGrid(Vector2Int pos) => pos.x >= 0 && pos.x < gridWidth && pos.y >= 0 && pos.y < gridHeight;

    private bool IsGridEdge(Vector2Int pos) => pos.x == 0 || pos.x == gridWidth - 1 || pos.y == 0 || pos.y == gridHeight - 1;

    //private Room CoordinateToRoom(Vector2Int coord)
    //{
    //    coordToRoomMap.TryGetValue(coord, out Room room);
    //    return room;
    //}

    private Room CoordinateToRoom(Vector2Int coord)
    {
        foreach (Room room in rooms)
        {
            if (room.coords.Contains(coord)) return room;
        }

        return null;
    }

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

        //rooms
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

            //room walls
            //foreach (Room room in rooms)
            //{
            //    Gizmos.color = room.color;

            //    foreach (Vector2Int pos in room.walls)
            //    {
            //        Gizmos.DrawCube(new Vector3(pos.x, 1f, pos.y), Vector3.one);
            //        Gizmos.DrawWireCube(new Vector3(pos.x, 1f, pos.y), Vector3.one);
            //    }
            //}
        }

        //walls
        if (wallSet != null)
        {
            Gizmos.color = Color.white * 0.5f;
            foreach (Vector2Int wall in wallSet)
            {
                Gizmos.DrawCube(new Vector3(wall.x, 0f, wall.y), Vector3.one * 0.5f);
                Gizmos.DrawWireCube(new Vector3(wall.x, 0f, wall.y), Vector3.one * 0.5f);
            }
        }

        //removed walls
        if (removedDoubleWalls != null)
        {
            Gizmos.color = Color.red * 2;
            foreach (Vector2Int wall in removedDoubleWalls)
            {
                Gizmos.DrawCube(new Vector3(wall.x, 0f, wall.y), Vector3.one * 0.5f);
                Gizmos.DrawWireCube(new Vector3(wall.x, 0f, wall.y), Vector3.one * 0.5f);
            }
        }

        //doors
        if (doorSet != null)
        {
            Gizmos.color = Color.blue * 2;
            foreach (Vector2Int door in doorSet)
            {
                Gizmos.DrawCube(new Vector3(door.x, 0f, door.y), Vector3.one * 0.5f);
                Gizmos.DrawWireCube(new Vector3(door.x, 0f, door.y), Vector3.one * 0.5f);
            }
        }
    }
}
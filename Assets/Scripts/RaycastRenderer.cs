using UnityEngine;

public class RaycastRenderer : MonoBehaviour
{
    public ComputeShader raycastShader;
    public Material displayMaterial;
    public Vector2Int mapSize = new Vector2Int(8, 8);
    public Vector2 playerStartPos = new Vector2(1.5f, 1.5f);
    public Vector2 playerStartDir = new Vector2(1, 0);
    [Range(0f, 1f)] public float wallChance = 0.2f;

    [Header("Rendering Settings")]
    public int textureWidth = 1024;
    public int textureHeight = 512;
    public float movementSpeed = 5f;
    public float rotationSpeed = 2f;

    [Header("Textures")]
    public Texture2D wallTexture;
    public Texture2D floorTexture;
    public Texture2D ceilingTexture;

    private RenderTexture resultTexture;
    private Texture2D mapTexture;
    private int kernelHandle;
    private Vector2 playerPos;
    private Vector2 playerDir;
    private Vector2 cameraPlane;

    void Start()
    {
        Application.targetFrameRate = 60;
        InitializeTexture();
        CreateMapTexture(GenerateMap());
        InitializeComputeShader();
        InitializePlayer();
    }

    void InitializeTexture()
    {
        resultTexture = new RenderTexture(textureWidth, textureHeight, 0)
        {
            enableRandomWrite = true
        };
        resultTexture.Create();
    }

    bool[,] GenerateMap()
    {
        bool[,] map = new bool[mapSize.x, mapSize.y];
        for (int x = 0; x < mapSize.x; x++)
        {
            for (int y = 0; y < mapSize.y; y++)
            {
                bool isEdge = x == 0 || y == 0 || x == mapSize.x - 1 || y == mapSize.y - 1;

                if (isEdge)
                {
                    map[x, y] = true;
                }
                else
                {
                    map[x, y] = Random.Range(0f, 1f) < wallChance;
                }
            }
        }
        return map;
    }

    void CreateMapTexture(bool[,] map)
    {
        mapTexture = new Texture2D(mapSize.x, mapSize.y, TextureFormat.R8, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        for (int x = 0; x < mapSize.x; x++)
        {
            for (int y = 0; y < mapSize.y; y++)
            {
                mapTexture.SetPixel(x, y, map[x, y] ? Color.white : Color.black);
            }
        }
        mapTexture.Apply();
    }

    void InitializeComputeShader()
    {
        kernelHandle = raycastShader.FindKernel("CSMain");
        raycastShader.SetTexture(kernelHandle, "Result", resultTexture);
        raycastShader.SetTexture(kernelHandle, "Map", mapTexture);
        raycastShader.SetInts("mapSize", new int[] { mapSize.x, mapSize.y });

        raycastShader.SetTexture(kernelHandle, "WallTex", wallTexture);
        raycastShader.SetTexture(kernelHandle, "FloorTex", floorTexture);
        raycastShader.SetTexture(kernelHandle, "CeilingTex", ceilingTexture);
    }

    void InitializePlayer()
    {
        playerPos = playerStartPos;
        playerDir = playerStartDir.normalized;
        cameraPlane = new Vector2(-playerDir.y, playerDir.x) * 0.66f;
    }

    void Update()
    {
        HandleInput();
        UpdateShaderParameters();
        raycastShader.Dispatch(kernelHandle, textureWidth / 64, 1, 1);
        displayMaterial.mainTexture = resultTexture;
    }

    void HandleInput()
    {
        // Rotation
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            float rot = rotationSpeed * Time.deltaTime;
            RotatePlayer(rot);
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            float rot = -rotationSpeed * Time.deltaTime;
            RotatePlayer(rot);
        }

        // Movement
        float moveSpeed = movementSpeed * Time.deltaTime;
        Vector2 moveDir = Vector2.zero;

        if (Input.GetKey(KeyCode.UpArrow)) moveDir += playerDir;
        if (Input.GetKey(KeyCode.DownArrow)) moveDir -= playerDir;

        if (moveDir.magnitude > 0)
        {
            Vector2 newPos = playerPos + moveDir.normalized * moveSpeed;
            if (!IsWall(newPos)) playerPos = newPos;
        }
    }

    void RotatePlayer(float angle)
    {
        playerDir = new Vector2(
            playerDir.x * Mathf.Cos(angle) - playerDir.y * Mathf.Sin(angle),
            playerDir.x * Mathf.Sin(angle) + playerDir.y * Mathf.Cos(angle)
        ).normalized;
        cameraPlane = new Vector2(-playerDir.y, playerDir.x) * 0.66f;
    }

    bool IsWall(Vector2 position)
    {
        int x = Mathf.FloorToInt(position.x);
        int y = Mathf.FloorToInt(position.y);
        if (x < 0 || y < 0 || x >= mapSize.x || y >= mapSize.y) return true;
        return mapTexture.GetPixel(x, y).r > 0.5f;
    }

    void UpdateShaderParameters()
    {
        raycastShader.SetVector("playerPos", new Vector4(playerPos.x, playerPos.y, 0, 0));
        raycastShader.SetVector("playerDir", new Vector4(playerDir.x, playerDir.y, 0, 0));
        raycastShader.SetVector("cameraPlane", new Vector4(cameraPlane.x, cameraPlane.y, 0, 0));
    }
}
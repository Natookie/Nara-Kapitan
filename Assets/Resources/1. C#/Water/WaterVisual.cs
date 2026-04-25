using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class WaterVisual : MonoBehaviour
{
    [Header("REFERENCES")]
    [SerializeField] private StormyOcean oceanPhysics;
    
    [Header("MESH TWEAKS")]
    [Range(50, 500)] public int meshResolution = 150;
    [SerializeField] private float oceanWidth = 30f;
    [SerializeField] private float oceanDepth = 15f;
    [Space(10)]
    [SerializeField] private float surfaceYPosition = 0f;
    
    [Header("PERFORMANCE")]
    [Range(1, 4)] public int updateEveryXFrames = 1;
    
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh mesh;
    
    private Vector3[] vertices;
    private float[] vertexXPositions;
    private int vertexCount;
    
    private int frameCount;
    private float currentSurfaceLevel;
    
    void Start(){
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        
        CreateWaterMesh();
        
        if(oceanPhysics == null)
            oceanPhysics = FindFirstObjectByType<StormyOcean>();
        
        currentSurfaceLevel = transform.position.y + surfaceYPosition;
    }
    
    void Update(){
        frameCount++;
        
        if(frameCount % updateEveryXFrames != 0) return;
        if(oceanPhysics == null) return;
        
        UpdateWaveVertices();
    }
    
    void CreateWaterMesh(){
        if(mesh != null){
            if(Application.isPlaying) Destroy(mesh);
            else DestroyImmediate(mesh);
        }
        
        mesh = new Mesh();
        mesh.name = "WaterMesh";
        
        vertexCount = meshResolution + 1;
        vertices = new Vector3[vertexCount * 2];
        vertexXPositions = new float[vertexCount];
        
        float localSurfaceY = surfaceYPosition;
        float localBottomY = surfaceYPosition - oceanDepth;
        
        for (int i = 0; i < vertexCount; i++){
            float t = i / (float)meshResolution;
            float x = Mathf.Lerp(-oceanWidth / 2, oceanWidth / 2, t);
            vertexXPositions[i] = x;
            
            vertices[i] = new Vector3(x, localSurfaceY, 0);
            vertices[i + vertexCount] = new Vector3(x, localBottomY, 0);
        }
        
        int[] triangles = new int[meshResolution * 6];
        int triIndex = 0;
        
        for (int i = 0; i < meshResolution; i++){
            int topLeft = i;
            int topRight = i + 1;
            int bottomLeft = i + vertexCount;
            int bottomRight = i + 1 + vertexCount;
            
            triangles[triIndex++] = topLeft;
            triangles[triIndex++] = topRight;
            triangles[triIndex++] = bottomLeft;
            
            triangles[triIndex++] = topRight;
            triangles[triIndex++] = bottomRight;
            triangles[triIndex++] = bottomLeft;
        }
        
        Vector2[] uvs = new Vector2[vertices.Length];
        for (int i = 0; i < vertexCount; i++){
            float u = i / (float)meshResolution;
            uvs[i] = new Vector2(u, 1f);
            uvs[i + vertexCount] = new Vector2(u, 0f);
        }
        
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        meshFilter.mesh = mesh;
    }
    
    void UpdateWaveVertices(){
        if(oceanPhysics == null || vertices == null) return;
        
        for (int i = 0; i < vertexCount; i++){
            float worldX = transform.position.x + vertexXPositions[i];
            float waterHeight = oceanPhysics.GetWaterHeightAt(worldX);
            vertices[i].y = waterHeight - transform.position.y;
        }
        
        mesh.vertices = vertices;
        
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        currentSurfaceLevel = transform.position.y + vertices[vertexCount / 2].y;
    }
    
    public void ForceMeshUpdate(){
        if(oceanPhysics != null) UpdateWaveVertices();
    }
    
    public float GetSurfaceHeightAt(float localX){
        if(vertices == null || vertexCount == 0)
            return surfaceYPosition;
        
        float t = Mathf.InverseLerp(-oceanWidth / 2, oceanWidth / 2, localX);
        int index = Mathf.RoundToInt(t * meshResolution);
        index = Mathf.Clamp(index, 0, vertexCount - 1);
        
        return vertices[index].y;
    }
    
    public Vector3 GetSurfacePointAt(float localX) => transform.position + new Vector3(localX, GetSurfaceHeightAt(localX), 0);
    public Vector2 GetWaterXRange() => new Vector2(-oceanWidth / 2f, oceanWidth / 2f);
    public Vector2 GetWaterDimensions() => new Vector2(oceanWidth, oceanDepth);
    public float GetCurrentSurfaceLevel() => currentSurfaceLevel;
    
    public void SetResolution(int resolution){
        meshResolution = Mathf.Clamp(resolution, 50, 500);
        CreateWaterMesh();
    }
    
    public void SetDimensions(float width, float depth){
        oceanWidth = width;
        oceanDepth = depth;
        CreateWaterMesh();
    }
    
    #if UNITY_EDITOR
    void OnValidate(){
        if(!Application.isPlaying && meshFilter != null && meshFilter.sharedMesh != null){
            UnityEditor.EditorApplication.delayCall += () =>{
                if(this != null && meshFilter != null)
                    CreateWaterMesh();
            };
        }
    }
    
    void OnDrawGizmosSelected(){
        if(!Application.isPlaying) return;
        
        Gizmos.color = new Color(0, 0.5f, 1f, 0.3f);
        Vector3 center = transform.position + new Vector3(0, surfaceYPosition - oceanDepth / 2, 0);
        Vector3 size = new Vector3(oceanWidth, oceanDepth, 0.1f);
        Gizmos.DrawWireCube(center, size);
        
        if(vertices != null && vertexCount > 0){
            Gizmos.color = Color.cyan;
            for (int i = 0; i < vertexCount - 1; i++){
                Vector3 p1 = transform.position + new Vector3(vertexXPositions[i], vertices[i].y, 0);
                Vector3 p2 = transform.position + new Vector3(vertexXPositions[i + 1], vertices[i + 1].y, 0);
                Gizmos.DrawLine(p1, p2);
            }
        }
    }
    #endif
}
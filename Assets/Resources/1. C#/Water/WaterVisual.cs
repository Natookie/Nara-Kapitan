using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class WaterVisuals : MonoBehaviour
{
    [Header("REFERENCES")]
    [SerializeField] private StormyOcean oceanPhysics;
    
    [Header("MESH TWEAKS")]
    [Range(50, 500)] public int meshResolution = 150;
    [SerializeField] private float oceanWidth = 30f;
    [SerializeField] private float oceanDepth = 15f;
    [Space(10)]
    [SerializeField] private float surfaceYPosition = 0f;
    
    [Header("WATER COLORS")]
    [SerializeField] private Color surfaceColor = new Color(0.05f, 0.1f, 0.2f, 0.9f);
    [SerializeField] private Color deepColor = new Color(0.02f, 0.05f, 0.1f, 0.9f);
    
    [Header("FOAM TWEAKS")]
    [SerializeField] private Color foamColor = new Color(0.9f, 0.9f, 1f, 0.8f);
    [Range(0f, 2f)] public float foamIntensity = 1.0f;
    [Range(0f, 1f)] public float foamSpread = 0.3f;
    [Range(0f, 1f)] public float foamSoftness = 0.3f;
    
    [Header("DEPTH EFFECT")]
    [Range(0f, 1f)] public float waveDarkening = 0.5f;
    [Range(0.5f, 3f)] public float depthGradientPower = 1.5f;
    
    [Header("WAVE SHADER")]
    [Range(0.1f, 2f)] public float waveScale = 0.3f;
    [Range(0.5f, 3f)] public float waveSpeed = 1.5f;
    
    [Header("Performance")]
    [Range(1, 4)] public int updateEveryXFrames = 1;
    
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh mesh;
    
    private Vector3[] vertices;
    private float[] vertexXPositions;
    private int vertexCount;
    
    private int stormIntensityID;
    private int customTimeID;
    private int surfaceColorID;
    private int deepColorID;
    private int foamColorID;
    private int foamIntensityID;
    private int foamSpreadID;
    private int waveDarkeningID;
    private int maxDepthID;
    private int waveScaleID;
    private int waveSpeedID;
    private int depthGradientPowerID;
    private int foamSoftnessID;
    private int surfaceLevelID;
    
    private int frameCount;
    private float shaderTime;
    private float currentSurfaceLevel;
    
    void Start(){
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        
        stormIntensityID = Shader.PropertyToID("_StormIntensity");
        customTimeID = Shader.PropertyToID("_CustomTime");
        surfaceColorID = Shader.PropertyToID("_SurfaceColor");
        deepColorID = Shader.PropertyToID("_DeepColor");
        foamColorID = Shader.PropertyToID("_FoamColor");
        foamIntensityID = Shader.PropertyToID("_FoamIntensity");
        foamSpreadID = Shader.PropertyToID("_FoamSpread");
        waveDarkeningID = Shader.PropertyToID("_WaveDarkening");
        maxDepthID = Shader.PropertyToID("_MaxDepth");
        waveScaleID = Shader.PropertyToID("_WaveScale");
        waveSpeedID = Shader.PropertyToID("_WaveSpeed");
        depthGradientPowerID = Shader.PropertyToID("_DepthGradientPower");
        foamSoftnessID = Shader.PropertyToID("_FoamSoftness");
        surfaceLevelID = Shader.PropertyToID("_SurfaceLevel");
        
        CreateWaterMesh();
        SetupShaderMaterial();
        
        if(oceanPhysics == null) oceanPhysics = FindFirstObjectByType<StormyOcean>();
        
        currentSurfaceLevel = transform.position.y + surfaceYPosition;
    }
    
    void Update(){
        frameCount++;
        shaderTime += Time.deltaTime;
        
        if(frameCount % updateEveryXFrames != 0) return;
        if(oceanPhysics == null) return;
        
        UpdateWaveVertices();
        UpdateShaderProperties();
    }
    
    void CreateWaterMesh(){
        mesh = new Mesh();
        mesh.name = "StormyOceanMesh";
        
        vertexCount = meshResolution + 1;
        vertices = new Vector3[vertexCount * 2];
        vertexXPositions = new float[vertexCount];
        
        float localSurfaceY = surfaceYPosition;
        float localBottomY = surfaceYPosition - oceanDepth;
        
        for(int i = 0; i < vertexCount; i++){
            float x = Mathf.Lerp(-oceanWidth / 2, oceanWidth / 2, i / (float)meshResolution);
            vertexXPositions[i] = x;
            
            vertices[i] = new Vector3(x, localSurfaceY, 0);
            vertices[i + vertexCount] = new Vector3(x, localBottomY, 0);
        }
        
        int[] triangles = new int[meshResolution * 6];
        int triIndex = 0;
        
        for(int i = 0; i < meshResolution; i++){
            int s0 = i;
            int s1 = i + 1;
            int b0 = i + vertexCount;
            int b1 = i + 1 + vertexCount;
            
            triangles[triIndex++] = s0;
            triangles[triIndex++] = s1;
            triangles[triIndex++] = b0;
            
            triangles[triIndex++] = s1;
            triangles[triIndex++] = b1;
            triangles[triIndex++] = b0;
        }
        
        Vector2[] uvs = new Vector2[vertices.Length];
        for(int i = 0; i < vertexCount; i++){
            float u = i / (float)meshResolution;
            uvs[i] = new Vector2(u, 1.0f);
            uvs[i + vertexCount] = new Vector2(u, 0.0f);
        }
        
        Vector3[] normals = new Vector3[vertices.Length];
        for(int i = 0; i < vertexCount; i++){
            Vector3 normal = Vector3.up;
            if(i > 0 && i < vertexCount - 1){
                Vector3 left = vertices[i - 1];
                Vector3 right = vertices[i + 1];
                Vector3 slope = right - left;
                normal = new Vector3(-slope.y, slope.x, 0).normalized;
            }
            normals[i] = normal;
            normals[i + vertexCount] = Vector3.down;
        }
        
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.normals = normals;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        
        meshFilter.mesh = mesh;
    }
    
    void SetupShaderMaterial(){
        Material mat = meshRenderer.material;
        
        if(mat == null || mat.shader.name != "Custom/StormyWater"){
            Shader waterShader = Shader.Find("Custom/StormyWater");
            if(waterShader != null){
                mat = new Material(waterShader);
                meshRenderer.material = mat;
                Debug.Log("Using StormyWater material");
            }else{
                Debug.LogWarning("Using Sprites/Default");
                mat = new Material(Shader.Find("Sprites/Default"));
                mat.color = surfaceColor;
                meshRenderer.material = mat;
                return;
            }
        }
        
        UpdateShaderProperties();
    }
    
    void UpdateWaveVertices(){
        if(oceanPhysics == null || vertices == null) return;
        
        for(int i = 0; i < vertexCount; i++){
            float worldX = transform.position.x + vertexXPositions[i];
            float waterHeight = oceanPhysics.GetWaterHeightAt(worldX);
            vertices[i].y = waterHeight - transform.position.y;
        }
        
        mesh.vertices = vertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        currentSurfaceLevel = transform.position.y + vertices[vertexCount/2].y;
    }
    
    void UpdateShaderProperties(){
        Material mat = meshRenderer.material;
        if(mat == null) return;
        
        mat.SetFloat(customTimeID, shaderTime);
        
        if(oceanPhysics != null) mat.SetFloat(stormIntensityID, oceanPhysics.GetStormIntensity());
        
        mat.SetFloat(maxDepthID, oceanDepth);
        mat.SetFloat(depthGradientPowerID, depthGradientPower);
        
        mat.SetFloat(surfaceLevelID, currentSurfaceLevel);
        
        mat.SetColor(surfaceColorID, surfaceColor);
        mat.SetColor(deepColorID, deepColor);
        mat.SetColor(foamColorID, foamColor);
        
        mat.SetFloat(foamIntensityID, foamIntensity);
        mat.SetFloat(foamSpreadID, foamSpread);
        mat.SetFloat(foamSoftnessID, foamSoftness);
        
        mat.SetFloat(waveDarkeningID, waveDarkening);
        mat.SetFloat(waveScaleID, waveScale);
        mat.SetFloat(waveSpeedID, waveSpeed);
    }
    
    #if UNITY_EDITOR
    [ContextMenu("Apply Gradient")]
    void ApplySmoothGradientPreset(){
        surfaceColor = new Color(0.05f, 0.1f, 0.2f, 0.9f);
        deepColor = new Color(0.02f, 0.05f, 0.1f, 0.9f);
        foamColor = new Color(0.9f, 0.9f, 1f, 0.7f);
        foamIntensity = 0.8f;
        foamSpread = 0.4f;
        foamSoftness = 0.4f;
        waveDarkening = 0.3f;
        depthGradientPower = 1.2f;
        waveScale = 0.25f;
        waveSpeed = 1.2f;
        
        UpdateShaderProperties();
    }
    
    [ContextMenu("Reset Mesh")]
    void CreateResetMesh(){
        CreateWaterMesh();
        SetupShaderMaterial();
    }
    #endif
}
using UnityEngine;

[System.Serializable]
public class WaveLayer
{
    [Range(0.1f, 2f)] public float amplitude = 0.5f;
    [Range(0.5f, 30f)] public float frequency = 5f;
    [Range(0.1f, 5f)] public float speed = 1f;
    public Vector2 direction = Vector2.right;
    public float seed;
}

public class StormyOcean : MonoBehaviour
{
    [Header("STORM TWEAKS")]
    [Range(0f, 1f)] public float stormIntensity = 0.5f;
    [SerializeField] private float stormTransitionSpeed = 0.1f;
    
    [Header("WAVE TWEAKS")]
    [SerializeField] private float frequencyMultiplier = 0.01f;
    [SerializeField] private float speedMultiplier = 1.0f;
    [SerializeField] private float maxWaveHeight = 1.2f;
    [Space(10)]
    [SerializeField] private WaveLayer[] waveLayers = new WaveLayer[4];
    
    [Header("RANDOM")]
    [SerializeField] private bool usePerlinNoise = true;
    [Range(0.1f, 5f)] public float noiseScale = 0.3f;
    [Range(0f, 1f)] public float noiseStrength = 0.05f;
    
    private float time;
    private float targetStormIntensity;
    private float stormMultiplier;
    private float perlinNoiseX;
    private float perlinNoiseY;
    private float cachedNoiseTime;

    void Start(){
        InitializeWaveLayers();
        targetStormIntensity = Random.Range(0.3f, 0.7f);
        time = 0f;
        stormMultiplier = 1f + stormIntensity * 2f;
    }
    
    void Update(){
        time += Time.deltaTime * speedMultiplier;
        
        stormIntensity = Mathf.Lerp(stormIntensity, targetStormIntensity, Time.deltaTime * stormTransitionSpeed);
        stormMultiplier = 1f + stormIntensity * 3f;
        
        if(Random.value < 0.005f) targetStormIntensity = Random.Range(0.2f, 0.8f);
        if(usePerlinNoise && Mathf.Abs(cachedNoiseTime - time) > 0.1f){
            perlinNoiseX = time * 0.1f;
            perlinNoiseY = time * 0.05f + 100f;
            cachedNoiseTime = time;
        }
    }
    
    void InitializeWaveLayers(){
        if(waveLayers == null || waveLayers.Length == 0) waveLayers = new WaveLayer[4];
        for(int i = 0; i < waveLayers.Length; i++){
            if(waveLayers[i] == null) waveLayers[i] = new WaveLayer();
            if(waveLayers[i].seed == 0) waveLayers[i].seed = Random.Range(0f, 1000f);
        }
        
        if(waveLayers[0].frequency < 1f || waveLayers[0].frequency > 10f){
            waveLayers[0].amplitude = 0.8f;
            waveLayers[0].frequency = 1.2f;
            waveLayers[0].speed = 0.8f;
            waveLayers[0].direction = Vector2.right;
            waveLayers[0].seed = Random.Range(0f, 1000f);
            
            waveLayers[1].amplitude = 0.5f;
            waveLayers[1].frequency = 2.5f;
            waveLayers[1].speed = 1.2f;
            waveLayers[1].direction = new Vector2(0.9f, 0.1f).normalized;
            waveLayers[1].seed = Random.Range(0f, 1000f);
            
            waveLayers[2].amplitude = 0.3f;
            waveLayers[2].frequency = 5f;
            waveLayers[2].speed = 1.8f;
            waveLayers[2].direction = new Vector2(0.7f, 0.3f).normalized;
            waveLayers[2].seed = Random.Range(0f, 1000f);
            
            waveLayers[3].amplitude = 0.2f;
            waveLayers[3].frequency = 8f;
            waveLayers[3].speed = 2f;
            waveLayers[3].direction = Vector2.right;
            waveLayers[3].seed = Random.Range(0f, 1000f);
        }
    }
    
    public float GetWaterHeightAt(float x){
        float localX = x - transform.position.x;
        float waveHeight = 0f;
        
        foreach (WaveLayer layer in waveLayers){
            if(layer == null) continue;
            
            float k = layer.frequency * frequencyMultiplier * 2f * Mathf.PI;
            float phase = k * (localX * layer.direction.x) - time * layer.speed + layer.seed;
            waveHeight += layer.amplitude * stormMultiplier * Mathf.Cos(phase);
        }
        
        if(usePerlinNoise){
            float noise = Mathf.PerlinNoise(localX * noiseScale * 0.05f + perlinNoiseX, perlinNoiseY) * 2f - 1f;
            waveHeight += noise * noiseStrength * stormMultiplier * 0.2f;
        }
        
        waveHeight = Mathf.Clamp(waveHeight, -maxWaveHeight, maxWaveHeight * stormMultiplier);
        
        return transform.position.y + waveHeight;
    }
    
    public Vector2 GetWaterNormalAt(float x){
        float delta = 0.1f;
        float h1 = GetWaterHeightAt(x - delta);
        float h2 = GetWaterHeightAt(x + delta);
        
        Vector2 normal = new Vector2(-(h2 - h1), delta * 2f).normalized;
        return normal;
    }

    public void SetStormIntensity(float intensity, bool immediate = false){
        targetStormIntensity = Mathf.Clamp01(intensity);
        if(immediate) stormIntensity = targetStormIntensity;
    }
    
    public float GetStormIntensity() => stormIntensity;
    public float GetTime() => time;
    public WaveLayer[] GetWaveLayers() => waveLayers;
    
    #if UNITY_EDITOR
    [ContextMenu("Randomize Biji")]
    void RandomizeSeeds(){
        foreach (var layer in waveLayers){
            if(layer != null) layer.seed = Random.Range(0f, 1000f);
        }
    }
    
    [ContextMenu("Set High Storm")]
    void SetHighStorm(){
        stormIntensity = 0.8f;
        targetStormIntensity = 0.8f;
    }
    #endif
}
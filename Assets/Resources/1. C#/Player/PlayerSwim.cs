using UnityEngine;

public class PlayerSwim : MonoBehaviour
{
    [Header("REFERENCES")]    
    [SerializeField] private StormPhysics playerPhysics;
    [SerializeField] private new Rigidbody2D rigidbody;
    
    [Header("WAVE PUSH - LEFT DIRECTION")]
    [SerializeField] private float baseLeftPush = 5f; // Constant leftward force
    [SerializeField] private float waveInfluence = 2f; // How much waves affect the push
    [SerializeField] private float pushSmoothing = 2f;
    [SerializeField] private float maxPushVelocity = 8f;
    
    [Header("TURBULENCE")]
    [SerializeField] private float turbulenceStrength = 1f;
    [SerializeField] private float turbulenceFrequency = 1.5f;
    
    [Header("UNDERTOW")]
    [SerializeField] private float undertowStrength = 0.5f;
    [SerializeField] private float depthThreshold = 1f;
    
    [Header("DEBUG")]
    [SerializeField] private bool showDebug = true;
    
    private float turbulenceOffset;
    private float currentPushForce;
    
    void Start(){
        turbulenceOffset = Random.Range(0f, 100f);
        if(rigidbody == null) rigidbody = GetComponent<Rigidbody2D>();
    }
    
    void Update(){
        HandleWaveDrag();
    }
    
    void HandleWaveDrag(){
        if(!playerPhysics.InWater) return;
        
        float x = transform.position.x;
        
        // Get wave data (just for variation, not for direction)
        float waveHeight = playerPhysics.WaveHeightAtPosition(x);
        float waveSlope = playerPhysics.WaveSlopeAtPosition(x);
        float waveSpeed = playerPhysics.WaveVelocityAtPosition(x);
        
        // Log values if debugging
        if(showDebug){
            Debug.Log($"waveHeight {waveHeight}");
            Debug.Log($"waveSlope {waveSlope}");
            Debug.Log($"waveSpeed {waveSpeed}");
        }
        
        // ARTIFICIAL LEFT PUSH - This is the key part!
        // Base push is always negative (left)
        float targetPush = -baseLeftPush; // Negative = left
        
        // Add some wave-based variation but keep it pushing left
        // Use absolute values of wave data to ensure we only affect magnitude, not direction
        float waveVariation = (Mathf.Abs(waveSlope) * waveInfluence) + (Mathf.Abs(waveSpeed) * 0.2f);
        targetPush -= waveVariation; // Subtract to make it more negative (stronger left)
        
        // Smooth the push force
        currentPushForce = Mathf.Lerp(currentPushForce, targetPush, Time.deltaTime * pushSmoothing);
        
        // Apply the leftward force
        Vector2 targetVelocity = rigidbody.linearVelocity;
        targetVelocity.x = Mathf.Lerp(targetVelocity.x, currentPushForce, Time.deltaTime * pushSmoothing);
        targetVelocity.x = Mathf.Clamp(targetVelocity.x, -maxPushVelocity, maxPushVelocity);
        
        rigidbody.linearVelocity = targetVelocity;
        
        // Small turbulence (reduced to not interfere with left push)
        float turbulenceX = Mathf.PerlinNoise(Time.time * turbulenceFrequency + turbulenceOffset, 0f) * 2f - 1f;
        float turbulenceY = Mathf.PerlinNoise(0f, Time.time * turbulenceFrequency + turbulenceOffset) * 2f - 1f;
        
        Vector2 turbulenceForce = new Vector2(turbulenceX * 0.3f, turbulenceY * 0.5f) * turbulenceStrength;
        rigidbody.AddForce(turbulenceForce, ForceMode2D.Force);
        
        // Undertow
        float waterDepth = waveHeight - transform.position.y;
        if(waterDepth > depthThreshold){
            float undertowForce = (waterDepth - depthThreshold) * undertowStrength;
            rigidbody.AddForce(Vector2.down * undertowForce, ForceMode2D.Force);
        }
        
        if(showDebug){
            Debug.DrawRay(transform.position, Vector3.left * 2f, Color.red); // Show left push direction
            Debug.DrawRay(transform.position + Vector3.up, new Vector3(currentPushForce * 0.5f, 0, 0), Color.cyan);
        }
    }
}
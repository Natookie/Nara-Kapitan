using UnityEngine;

public class PlayerSwim : MonoBehaviour
{
    [Header("REFERENCES")]    
    [SerializeField] private BuoyancyPhysics playerPhysics;
    [SerializeField] private new Rigidbody2D rigidbody;
    [SerializeField] private PlayerMovement playerMovement;
    
    [Header("WAVE PUSH")]
    [SerializeField] private float baseLeftPush = 5f;
    [SerializeField] private float waveInfluence = 2f;
    [SerializeField] private float pushSmoothing = 2f;
    [SerializeField] private float maxPushVelocity = 8f;
    
    [Header("GROUNDED PUSH")]
    [SerializeField] private bool enableGroundedPush = true;
    [SerializeField] private float groundedRightPush = 2f;
    [SerializeField] private float groundedPushSmoothing = 3f;
    
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
    private float currentGroundedPush;
    
    void Start(){
        turbulenceOffset = Random.Range(0f, 100f);
        
        if(rigidbody == null) rigidbody = GetComponent<Rigidbody2D>();
        if(playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
        if(playerPhysics == null) playerPhysics = GetComponent<BuoyancyPhysics>();
    }
    
    void Update(){
        HandleWaveDrag();
    }
    
    void HandleWaveDrag(){
        if(!playerPhysics.InWater){
            currentPushForce = 0;
            currentGroundedPush = 0;
            return;
        }
        
        float x = transform.position.x;
        
        float waveHeight = playerPhysics.WaveHeightAtPosition(x);
        float waveSlope = playerPhysics.WaveSlopeAtPosition(x);
        float waveSpeed = playerPhysics.WaveVelocityAtPosition(x);
        
        //LEFT PUSH
        float targetPush = -baseLeftPush;
        float waveVariation = (Mathf.Abs(waveSlope) * waveInfluence) + (Mathf.Abs(waveSpeed) * 0.2f);
        targetPush -= waveVariation;
        
        currentPushForce = Mathf.Lerp(currentPushForce, targetPush, Time.deltaTime * pushSmoothing);
        
        //RIGHT PUSH
        float targetGroundedPush = 0;
        if(enableGroundedPush && playerMovement != null && playerMovement.IsGrounded){
            targetGroundedPush = groundedRightPush;
            
            float waveResistance = Mathf.Abs(waveSlope) * 2f;
            targetGroundedPush = Mathf.Max(0, targetGroundedPush - waveResistance);
        }
        
        currentGroundedPush = Mathf.Lerp(currentGroundedPush, targetGroundedPush, Time.deltaTime * groundedPushSmoothing);
        
        float totalPushX = currentPushForce + currentGroundedPush;
        
        Vector2 targetVelocity = rigidbody.linearVelocity;
        targetVelocity.x = Mathf.Lerp(targetVelocity.x, totalPushX, Time.deltaTime * pushSmoothing);
        targetVelocity.x = Mathf.Clamp(targetVelocity.x, -maxPushVelocity, maxPushVelocity);
        
        rigidbody.linearVelocity = targetVelocity;
        
        bool isGrounded = playerMovement != null && playerMovement.IsGrounded;
        float turbulenceMultiplier = isGrounded ? 0.3f : 1f;
        float turbulenceX = Mathf.PerlinNoise(Time.time * turbulenceFrequency + turbulenceOffset, 0f) * 2f - 1f;
        float turbulenceY = Mathf.PerlinNoise(0f, Time.time * turbulenceFrequency + turbulenceOffset) * 2f - 1f;
        
        Vector2 turbulenceForce = new Vector2(turbulenceX * 0.3f, turbulenceY * 0.5f) * turbulenceStrength * turbulenceMultiplier;
        rigidbody.AddForce(turbulenceForce, ForceMode2D.Force);
        
        float undertowMultiplier = isGrounded ? 0.2f : 1f;
        float waterDepth = waveHeight - transform.position.y;
        if(waterDepth > depthThreshold){
            float undertowForce = (waterDepth - depthThreshold) * undertowStrength * undertowMultiplier;
            rigidbody.AddForce(Vector2.down * undertowForce, ForceMode2D.Force);
        }
        
        if(showDebug){
            Debug.DrawRay(transform.position, Vector3.left * Mathf.Abs(currentPushForce) * 0.5f, Color.red);
            
            if(isGrounded && currentGroundedPush > 0)
                Debug.DrawRay(transform.position, Vector3.right * currentGroundedPush * 0.5f, Color.green);
            Debug.DrawRay(transform.position + Vector3.up * 0.5f, new Vector3(totalPushX * 0.3f, 0, 0), Color.cyan);
        }
    }
    
    public bool IsGrounded => playerMovement != null && playerMovement.IsGrounded;
}
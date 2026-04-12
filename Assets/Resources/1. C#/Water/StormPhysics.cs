using UnityEngine;

public class StormPhysics : MonoBehaviour
{
    [Header("BUOYANCY")]
    [SerializeField] private float buoyancyForce = 25f;
    [SerializeField] private float buoyancyDamping = 3f;
    [SerializeField] private float waterDrag = 2f;
    [SerializeField] private float waterAngularDrag = 3f;
    [Range(0f, 1f)] public float targetSubmersion = 0.3f;
    
    [Header("STABILIZATION")]
    [SerializeField] private float stabilizationForce = 15f;
    [SerializeField] private float uprightTorque = 8f;
    [SerializeField] private float maxTiltAngle = 30f;
    [Range(0f, 1f)] public float stabilizationStrength = 0.7f;
    
    [Header("WAVE FOLLOW")]
    [SerializeField] private float waveFollowSpeed = 8f;
    [SerializeField] private float heightSmoothing = 0.2f;
    [SerializeField] private bool snapToWaveSurface = true;
    
    [Header("WAVE FORCES")]
    [SerializeField] private float horizontalForceMultiplier = 0.3f;
    [SerializeField] private float verticalForceMultiplier = 0.5f;
    [SerializeField] private float turbulenceStrength = 0.5f;
    
    [Header("PHYSICS")]
    [SerializeField] private float gravityMultiplier = 0.1f;
    [SerializeField] private float airDrag = 0.1f;
    [SerializeField] private float airAngularDrag = 0.5f;
    
    [Header("IMPACT FORCES")]
    [SerializeField] private float impactForceMultiplier = 2f;
    [SerializeField] private float impactDamping = 0.8f;
    
    [Header("DIMENSIONS")]
    [SerializeField] private float objLength = 2f;
    [SerializeField] private float objHeight = 0.5f;
    [SerializeField] private float objDraft = 0.2f;
    
    [Header("REFERENCES")]
    [SerializeField] private Rigidbody2D rb;

    [Header("DEBUG")]
    public bool showDebug = false;
    public bool showSubmersion = false;
    
    private StormyOcean ocean;
    private float targetHeight;
    private float currentHeight;
    private bool inWater = true;
    private Vector2[] buoyancyPoints;
    private float originalDrag;
    private float originalAngularDrag;
    
    private Vector2[] debugWaterLevels;
    private float[] debugSubmersionDepths;
    
    private float lastWaveHeight;
    private float lastWaveX;
    private float waveVelocity;
    
    void Start(){
        ocean = FindFirstObjectByType<StormyOcean>();
        
        originalDrag = rb.linearDamping;
        originalAngularDrag = rb.angularDamping;
        
        CreateBuoyancyPoints();
        
        debugWaterLevels = new Vector2[buoyancyPoints.Length];
        debugSubmersionDepths = new float[buoyancyPoints.Length];
        
        rb.gravityScale = gravityMultiplier;
        
        targetHeight = ocean.GetWaterHeightAt(transform.position.x);
        currentHeight = targetHeight;
        
        lastWaveHeight = targetHeight;
        lastWaveX = transform.position.x;
    }
    
    void CreateBuoyancyPoints(){
        int pointCount = 7;
        buoyancyPoints = new Vector2[pointCount];
        
        float halfLength = objLength * 0.5f;
        for(int i = 0;i < pointCount;i++){
            float t = i / (float)(pointCount - 1);
            float x = Mathf.Lerp(-halfLength, halfLength, t);
            
            
            float y = -objDraft * 0.5f;
            
            buoyancyPoints[i] = new Vector2(x, y);
        }
    }
    
    void FixedUpdate(){
        if(ocean == null || rb == null) return;
        
        CheckWaterStatus();
        ApplyBuoyancy();
        ApplyStabilization();
        ApplyWaveForces();
        FollowWaveSurface();
        
        
        PreventExcessiveSinking();
        
        float currentWaveHeight = ocean.GetWaterHeightAt(transform.position.x);
        float deltaX = transform.position.x - lastWaveX;
        float deltaTime = Time.fixedDeltaTime;
        
        if(deltaX != 0 && deltaTime > 0){
            waveVelocity = (currentWaveHeight - lastWaveHeight) / deltaX * Mathf.Sign(deltaX) * 5f;
            waveVelocity = Mathf.Clamp(waveVelocity, -10f, 10f);
        }
        
        lastWaveHeight = currentWaveHeight;
        lastWaveX = transform.position.x;
    }
    
    void Update(){
        if(showDebug) DebugDrawBuoyancy();
    }
    
    void CheckWaterStatus(){
        float waterHeight = ocean.GetWaterHeightAt(transform.position.x);
        float boatBottom = transform.position.y - objHeight * 0.5f;
        
        bool wasInWater = inWater;
        inWater = boatBottom < waterHeight;
        
        
        if(inWater){
            rb.linearDamping = waterDrag;
            rb.angularDamping = waterAngularDrag;
        }else{
            rb.linearDamping = airDrag;
            rb.angularDamping = airAngularDrag;
        }
    }
    
    void ApplyBuoyancy(){
        if(!inWater) return;
        
        float totalForce = 0f;
        int submergedPoints = 0;
        
        
        for(int i = 0;i < buoyancyPoints.Length;i++){
            Vector2 worldPoint = transform.TransformPoint(buoyancyPoints[i]);
            float waterHeight = ocean.GetWaterHeightAt(worldPoint.x);
            debugWaterLevels[i] = new Vector2(worldPoint.x, waterHeight);
            
            float submergedDepth = waterHeight - worldPoint.y;
            debugSubmersionDepths[i] = submergedDepth;
            
            if(submergedDepth > 0){
                submergedPoints++;
                float pointForce = buoyancyForce * submergedDepth * targetSubmersion;
                
                if(submergedDepth > objDraft * 2f) pointForce *= 2f;
                
                Vector2 force = Vector2.up * pointForce;
                Vector2 localVelocity = transform.InverseTransformDirection(rb.GetPointVelocity(worldPoint));
                force.y -= localVelocity.y * buoyancyDamping;
                
                
                rb.AddForceAtPosition(force, worldPoint);
                
                totalForce += pointForce;
                
                if(showDebug && showSubmersion){
                    Debug.DrawRay(worldPoint, force * 0.05f, Color.green);
                    Debug.DrawLine(worldPoint, new Vector3(worldPoint.x, waterHeight, 0), Color.Lerp(Color.green, Color.red, submergedDepth / (objDraft * 3f)));
                }
            }
        }
        
        
        if(submergedPoints > 0){
            float averageSubmersion = totalForce / submergedPoints;
            float targetForce = buoyancyForce * targetSubmersion;
            
            if(averageSubmersion < targetForce * 0.5f){
                rb.AddForce(Vector2.down * buoyancyForce * 0.1f);
            }
        }
    }
    
    void PreventExcessiveSinking(){
        if(!inWater) return;
        
        
        float centerDepth = ocean.GetWaterHeightAt(transform.position.x) - transform.position.y;
        
        if(centerDepth > objDraft * 2f) 
        {
            float excessDepth = centerDepth - objDraft;
            Vector2 antiSinkForce = Vector2.up * buoyancyForce * excessDepth * 2f;
            rb.AddForce(antiSinkForce);
            
            if(showDebug) Debug.DrawRay(transform.position, antiSinkForce * 0.1f, Color.magenta);
        }
    }
    
    void ApplyStabilization(){
        if(!inWater) return;
        
        float currentAngle = transform.eulerAngles.z;
        if(currentAngle > 180) currentAngle -= 360;
        
        float targetAngle = 0f;
        Vector2 waveNormal = ocean.GetWaterNormalAt(transform.position.x);
        float waveTilt = -waveNormal.x * maxTiltAngle * ocean.stormIntensity;
        targetAngle = Mathf.Lerp(0f, waveTilt, stabilizationStrength);
        targetAngle = Mathf.Clamp(targetAngle, -maxTiltAngle, maxTiltAngle);
        
        float angleDifference = Mathf.DeltaAngle(currentAngle, targetAngle);
        float torque = angleDifference * uprightTorque;
        
        torque -= rb.angularVelocity * 0.5f;
        
        rb.AddTorque(torque);
        
        Vector2 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
        Vector2 lateralStabilization = -transform.right * localVelocity.x * stabilizationForce;
        rb.AddForce(lateralStabilization);
    }
    
    void ApplyWaveForces(){
        if(!inWater) return;
        
        Vector2 waveNormal = ocean.GetWaterNormalAt(transform.position.x);
        
        Vector2 slopeForce = new Vector2(-waveNormal.x, 0) * horizontalForceMultiplier * ocean.stormIntensity;
        rb.AddForce(slopeForce);
        
        float waveHeight = ocean.GetWaterHeightAt(transform.position.x);
        float nextWaveHeight = ocean.GetWaterHeightAt(transform.position.x + 0.5f);
        float waveCurvature = nextWaveHeight - waveHeight;
        Vector2 curvatureForce = Vector2.up * waveCurvature * verticalForceMultiplier * ocean.stormIntensity;
        rb.AddForce(curvatureForce);
        
        float turbulence = Mathf.PerlinNoise(Time.time * 2f, transform.position.x * 0.1f) * 2f - 1f;
        Vector2 turbulenceForce = new Vector2(turbulence, Mathf.Abs(turbulence) * 0.3f) * turbulenceStrength * ocean.stormIntensity;
        rb.AddForce(turbulenceForce);
        
        if(showDebug) Debug.DrawRay(transform.position, waveNormal * 2f, Color.yellow);
    }
    
    void FollowWaveSurface(){
        if(!inWater || !snapToWaveSurface) return;
        
        float totalHeight = 0f;
        int sampleCount = 0;
        
        for(int i = 0;i < buoyancyPoints.Length;i++){
            float pointHeight = ocean.GetWaterHeightAt(transform.TransformPoint(buoyancyPoints[i]).x);
            totalHeight += pointHeight;
            sampleCount++;
        }
        
        targetHeight = totalHeight / sampleCount;
        targetHeight += objDraft;
        currentHeight = Mathf.Lerp(currentHeight, targetHeight, heightSmoothing);
        
        float currentHeightDiff = currentHeight - transform.position.y;
        float verticalVelocity = currentHeightDiff * waveFollowSpeed;
        
        Vector2 currentVel = rb.linearVelocity;
        currentVel.y = Mathf.Lerp(currentVel.y, verticalVelocity, Time.fixedDeltaTime * 5f);
        rb.linearVelocity = currentVel;
        
        if(showDebug){
            Debug.DrawLine(transform.position, new Vector3(transform.position.x, targetHeight, 0), Color.cyan);
            Debug.DrawLine(new Vector3(transform.position.x - 0.5f, targetHeight - objDraft, 0),
                         new Vector3(transform.position.x + 0.5f, targetHeight - objDraft, 0),
                         Color.white);
        }
    }
    
    public void ApplyImpactForce(Vector2 impactPoint, float force){
        if(!inWater) return;
        
        Vector2 direction = ((Vector2)transform.position - impactPoint).normalized;
        float distance = Vector2.Distance(impactPoint, transform.position);
        
        float scaledForce = force * impactForceMultiplier / Mathf.Max(1f, distance);
        
        rb.AddForce(direction * scaledForce, ForceMode2D.Impulse);
        
        Vector2 localImpactPoint = transform.InverseTransformPoint(impactPoint);
        float torque = localImpactPoint.x > 0 ? -scaledForce * 0.5f : scaledForce * 0.5f;
        rb.AddTorque(torque * impactDamping);
        
        if(showDebug) Debug.DrawLine(impactPoint, transform.position, Color.red, 1f);
    }
    
    void DebugDrawBuoyancy(){
        if(!showDebug || buoyancyPoints == null || ocean == null) return;
        
        DrawBoatOutline();
        for(int i = 0;i < buoyancyPoints.Length;i++){
            Vector2 worldPoint = transform.TransformPoint(buoyancyPoints[i]);
            
            Color pointColor = debugSubmersionDepths[i] > 0 ? Color.green : Color.red;
            Debug.DrawRay(worldPoint - Vector2.right * 0.05f, Vector2.right * 0.1f, pointColor);
            Debug.DrawRay(worldPoint - Vector2.up * 0.05f, Vector2.up * 0.1f, pointColor);
            
            if(showSubmersion && debugSubmersionDepths[i] > 0)
                Debug.DrawLine(worldPoint, new Vector3(worldPoint.x, debugWaterLevels[i].y, 0), Color.Lerp(Color.cyan, Color.blue, debugSubmersionDepths[i] / objDraft));
        }
    }
    
    void DrawBoatOutline(){
        Vector2 front = transform.TransformPoint(new Vector2(objLength * 0.5f, 0));
        Vector2 back = transform.TransformPoint(new Vector2(-objLength * 0.5f, 0));
        Vector2 frontTop = transform.TransformPoint(new Vector2(objLength * 0.5f, objHeight * 0.5f));
        Vector2 backTop = transform.TransformPoint(new Vector2(-objLength * 0.5f, objHeight * 0.5f));
        Vector2 frontBottom = transform.TransformPoint(new Vector2(objLength * 0.5f, -objHeight * 0.5f));
        Vector2 backBottom = transform.TransformPoint(new Vector2(-objLength * 0.5f, -objHeight * 0.5f));
        
        Debug.DrawLine(front, back, Color.white);
        Debug.DrawLine(frontTop, backTop, Color.white);
        Debug.DrawLine(frontBottom, backBottom, Color.white);
        Debug.DrawLine(frontTop, frontBottom, Color.white);
        Debug.DrawLine(backTop, backBottom, Color.white);
    }
    
    public void SetBuoyancyPoints(Vector2[] points){
        buoyancyPoints = points;
        debugWaterLevels = new Vector2[points.Length];
        debugSubmersionDepths = new float[points.Length];
    }
    
    public void AdjustBuoyancy(float multiplier){
        buoyancyForce *= multiplier;
        buoyancyDamping *= multiplier;
    }
    
    void OnDrawGizmosSelected(){
        if(!showDebug) return;
        
        Gizmos.color = Color.yellow;
        Vector3 center = transform.position;
        Vector3 size = new Vector3(objLength, objHeight, 0.1f);
        Gizmos.DrawWireCube(center, size);
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(
            new Vector3(center.x - objLength * 0.5f, center.y - objDraft, 0),
            new Vector3(center.x + objLength * 0.5f, center.y - objDraft, 0)
        );
    }

    public bool InWater => inWater;
    public float WaveHeightAtPosition(float x) => ocean.GetWaterHeightAt(x);
    public float WaveSlopeAtPosition(float x) => ocean.GetWaterNormalAt(x).x;
    public float WaveVelocityAtPosition(float x) => waveVelocity;
}
using UnityEngine;

public class BuoyancyPhysics : MonoBehaviour
{
    [Header("BUOYANCY")]
    [SerializeField] [Tooltip("How strongly water pushes up. Higher = more floaty")] private float buoyancyForce = 12f;
    [SerializeField] [Tooltip("Smooths out bobbing. Prevents springy bouncing")] private float buoyancyDamping = 5f;
    [SerializeField] [Tooltip("Resistance when moving through water")] private float waterDrag = 4f;
    [SerializeField] [Tooltip("Resistance when spinning in water")] private float waterAngularDrag = 3f;
    [Range(0f, 1f)] [Tooltip("How much of the boat sits underwater. 0.3 = 30% submerged")] public float targetSubmersion = 0.5f;
    
    [Header("STABILIZATION")]
    [SerializeField] [Tooltip("How hard the boat fights to stay level")] private float stabilizationForce = 8f;
    [SerializeField] [Tooltip("Rotational resistance. Higher = stiffer")] private float angularDamping = 5f;
    [SerializeField] [Tooltip("Max lean angle before boat tips too much")] private float maxTiltAngle = 45f;
    [Range(0f, 1f)] [Tooltip("How much boat follows wave tilt. 0 = flat, 1 = matches wave slope")] public float waveFollowStrength = 0.8f;
    [Range(0f, 1f)] [Tooltip("How quickly boat returns to neutral. 0 = never, 1 = instant")] public float selfRightingStrength = 0.3f;
    
    [Header("WAVE FORCES")]
    [SerializeField] [Tooltip("How much wave slope pushes boat sideways")] private float horizontalForceMultiplier = 0.3f;
    [SerializeField] [Tooltip("How much wave curvature pushes boat up/down")] private float verticalForceMultiplier = 0.5f;
    [SerializeField] [Tooltip("Random water chop that shakes the boat")] private float turbulenceStrength = 0.3f;
    [SerializeField] [Tooltip("How strongly waves pull boat to surface")] private float waveAttractionStrength = 3f;
    
    [Header("PHYSICS")]
    [SerializeField] [Tooltip("Gravity strength. Lower = floatier feeling")] private float gravityMultiplier = 0.5f;
    [SerializeField] [Tooltip("Resistance when flying through air")] private float airDrag = 0.1f;
    [SerializeField] [Tooltip("Resistance when spinning in air")] private float airAngularDrag = 0.5f;
    
    [Header("IMPACT FORCES")]
    [SerializeField] [Tooltip("How hard waves/objects smack the boat")] private float impactForceMultiplier = 2f;
    [SerializeField] [Tooltip("How quickly impact forces fade out")] private float impactDamping = 0.8f;
    
    [Header("DIMENSIONS")]
    [SerializeField] [Tooltip("Auto-detect size from sprite/renderer bounds")] private bool autoDetectBounds = true;
    [SerializeField] [Tooltip("Length of boat from tip to tail (if auto-detect is off)")] private float objLength = 2f;
    [SerializeField] [Tooltip("Total height from bottom to top (if auto-detect is off)")] private float objHeight = 0.5f;
    [SerializeField] [Tooltip("How deep hull sits underwater (if auto-detect is off)")] private float objDraft = 0.2f;
    [SerializeField] [Tooltip("Offset from bottom center for buoyancy points")] private float buoyancyOffsetY = 0f;
    
    [Header("REFERENCES")]
    [SerializeField] [Tooltip("Drag your boat's Rigidbody2D here")] private Rigidbody2D rb;
    [SerializeField] [Tooltip("Optional: Sprite renderer for auto bounds")] private SpriteRenderer spriteRenderer;
    [SerializeField] [Tooltip("Optional: Collider for auto bounds fallback")] private Collider2D boundsCollider;

    [Header("DEBUG")]
    [Tooltip("Draws buoyancy points and boat outline")]
    public bool showDebug = false;
    [Tooltip("Shows water level lines at each buoyancy point")]
    public bool showSubmersion = false;
    [Tooltip("Shows wave normal lines")]
    public bool showWaveNormals = false;
    
    private StormyOcean ocean;
    private bool inWater = true;
    private Vector2[] buoyancyPoints;
    private float originalDrag;
    private float originalAngularDrag;
    
    private Vector2[] debugWaterLevels;
    private float[] debugSubmersionDepths;
    
    private float lastWaveHeight;
    private float lastWaveX;
    private float waveVelocity;
    
    private float cachedLength;
    private float cachedHeight;
    private float cachedDraft;
    private Vector3 lastScale;
    
    private float currentHeightVelocity;
    private float targetWaterHeight;
    
    void Start(){
        ocean = FindFirstObjectByType<StormyOcean>();
        
        if(spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if(boundsCollider == null) boundsCollider = GetComponent<Collider2D>();
        
        originalDrag = rb.linearDamping;
        originalAngularDrag = rb.angularDamping;
        
        UpdateDimensionsFromBounds();
        CreateBuoyancyPoints();
        
        debugWaterLevels = new Vector2[buoyancyPoints.Length];
        debugSubmersionDepths = new float[buoyancyPoints.Length];
        
        rb.gravityScale = gravityMultiplier;
        
        targetWaterHeight = ocean.GetWaterHeightAt(transform.position.x) + cachedDraft;
        currentHeightVelocity = 0;
        
        lastWaveHeight = targetWaterHeight;
        lastWaveX = transform.position.x;
        lastScale = transform.localScale;
        
        rb.angularDamping = angularDamping;
    }
    
    void Update(){
        if(transform.localScale != lastScale){
            UpdateDimensionsFromBounds();
            CreateBuoyancyPoints();
            lastScale = transform.localScale;
            
            debugWaterLevels = new Vector2[buoyancyPoints.Length];
            debugSubmersionDepths = new float[buoyancyPoints.Length];
        }
        
        if(showDebug) DebugDrawBuoyancy();
    }
    
    void UpdateDimensionsFromBounds(){
        if(!autoDetectBounds) return;
        
        float detectedLength = objLength;
        float detectedHeight = objHeight;
        float detectedBottomY = 0f;
        
        if(spriteRenderer != null && spriteRenderer.sprite != null){
            Bounds bounds = spriteRenderer.bounds;
            Vector3 localBounds = bounds.size;
            
            detectedLength = localBounds.x / transform.localScale.x;
            detectedHeight = localBounds.y / transform.localScale.y;
            
            Vector3 localBottom = transform.InverseTransformPoint(bounds.min);
            detectedBottomY = localBottom.y;
        }
        else if(boundsCollider != null){
            Bounds bounds = boundsCollider.bounds;
            Vector3 localBounds = bounds.size;
            
            detectedLength = localBounds.x / transform.localScale.x;
            detectedHeight = localBounds.y / transform.localScale.y;
            
            Vector3 localBottom = transform.InverseTransformPoint(bounds.min);
            detectedBottomY = localBottom.y;
        }
        
        objLength = detectedLength;
        objHeight = detectedHeight;
        
        objDraft = Mathf.Abs(detectedBottomY) * 0.7f;
        if(objDraft <= 0) objDraft = objHeight * 0.3f;
    }
    
    void CreateBuoyancyPoints(){
        int pointCount = 7;
        buoyancyPoints = new Vector2[pointCount];
        
        float halfLength = cachedLength > 0 ? cachedLength * 0.5f : objLength * 0.5f;
        float baseY = buoyancyOffsetY - objDraft;
        
        for(int i = 0;i < pointCount;i++){
            float t = i / (float)(pointCount - 1);
            float x = Mathf.Lerp(-halfLength, halfLength, t);
            float y = baseY;
            
            buoyancyPoints[i] = new Vector2(x, y);
        }
    }
    
    void FixedUpdate(){
        if(ocean == null || rb == null) return;
        
        cachedLength = objLength * transform.localScale.x;
        cachedHeight = objHeight * transform.localScale.y;
        cachedDraft = objDraft * transform.localScale.y;
        
        UpdateTargetWaterHeight();
        CheckWaterStatus();
        ApplyBuoyancy();
        ApplyWaveRotation();
        ApplyStabilization();
        ApplyWaveForces();
        ApplySmoothWaveFollowing();
        
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
        
        if(showDebug && showWaveNormals){
            Vector2 waveNormal = ocean.GetWaterNormalAt(transform.position.x);
            Debug.DrawRay(transform.position, waveNormal * 2f, Color.magenta);
        }
    }
    
    void UpdateTargetWaterHeight(){
        float totalHeight = 0f;
        int sampleCount = 0;
        
        for(int i = 0;i < buoyancyPoints.Length;i++){
            float pointHeight = ocean.GetWaterHeightAt(transform.TransformPoint(buoyancyPoints[i]).x);
            totalHeight += pointHeight;
            sampleCount++;
        }
        
        targetWaterHeight = (totalHeight / sampleCount) + cachedDraft;
    }
    
    void CheckWaterStatus(){
        float waterHeight = ocean.GetWaterHeightAt(transform.position.x);
        float boatBottom = transform.position.y - cachedHeight * 0.5f;
        
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
                float pointForce = buoyancyForce * (submergedDepth / cachedDraft) * targetSubmersion;
                
                if(submergedDepth > cachedDraft * 2f) pointForce *= 2f;
                
                Vector2 force = Vector2.up * pointForce;
                Vector2 localVelocity = transform.InverseTransformDirection(rb.GetPointVelocity(worldPoint));
                force.y -= localVelocity.y * buoyancyDamping;
                
                rb.AddForceAtPosition(force, worldPoint);
                
                totalForce += pointForce;
                
                if(showDebug && showSubmersion){
                    Debug.DrawRay(worldPoint, force * 0.05f, Color.green);
                    Debug.DrawLine(worldPoint, new Vector3(worldPoint.x, waterHeight, 0), Color.Lerp(Color.green, Color.red, submergedDepth / (cachedDraft * 3f)));
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
    
    void ApplySmoothWaveFollowing(){
        if(!inWater) return;
        
        float heightDifference = targetWaterHeight - transform.position.y;
        float springForce = heightDifference * waveAttractionStrength;
        float dampingForce = -currentHeightVelocity * buoyancyDamping;
        float acceleration = springForce + dampingForce;
        
        currentHeightVelocity += acceleration * Time.fixedDeltaTime;
        currentHeightVelocity = Mathf.Clamp(currentHeightVelocity, -5f, 5f);
        
        Vector2 currentVel = rb.linearVelocity;
        currentVel.y += currentHeightVelocity * Time.fixedDeltaTime * 2f;
        rb.linearVelocity = currentVel;
        
        if(showDebug){
            Debug.DrawLine(transform.position, new Vector3(transform.position.x, targetWaterHeight, 0), Color.cyan);
            Debug.DrawLine(new Vector3(transform.position.x - cachedLength * 0.25f, targetWaterHeight - cachedDraft, 0),
                         new Vector3(transform.position.x + cachedLength * 0.25f, targetWaterHeight - cachedDraft, 0),
                         Color.white);
        }
    }
    
    void ApplyWaveRotation(){
        if(!inWater) return;
        
        Vector2 waveNormal = ocean.GetWaterNormalAt(transform.position.x);
        
        float targetAngle = -waveNormal.x * maxTiltAngle * ocean.GetStormIntensity();
        targetAngle = Mathf.Clamp(targetAngle, -maxTiltAngle, maxTiltAngle);
        
        float currentAngle = transform.eulerAngles.z;
        if(currentAngle > 180) currentAngle -= 360;
        
        float finalTargetAngle = Mathf.Lerp(currentAngle, targetAngle, waveFollowStrength);
        
        float neutralPull = -currentAngle * selfRightingStrength;
        finalTargetAngle += neutralPull * Time.fixedDeltaTime;
        
        float angleDifference = Mathf.DeltaAngle(currentAngle, finalTargetAngle);
        float torque = angleDifference * stabilizationForce;
        torque -= rb.angularVelocity * angularDamping;
        
        rb.AddTorque(torque);
        
        if(showDebug){
            Debug.DrawRay(transform.position, Quaternion.Euler(0, 0, targetAngle) * Vector2.right * 1.5f, Color.yellow);
            Debug.DrawRay(transform.position, Quaternion.Euler(0, 0, currentAngle) * Vector2.right * 1.5f, Color.white);
        }
    }
    
    void ApplyStabilization(){
        if(!inWater) return;
        
        Vector2 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
        Vector2 lateralStabilization = -transform.right * localVelocity.x * stabilizationForce * 0.5f;
        rb.AddForce(lateralStabilization);
    }
    
    void ApplyWaveForces(){
        if(!inWater) return;
        
        Vector2 waveNormal = ocean.GetWaterNormalAt(transform.position.x);
        
        Vector2 slopeForce = new Vector2(-waveNormal.x, 0) * horizontalForceMultiplier * ocean.GetStormIntensity();
        rb.AddForce(slopeForce);
        
        float waveHeight = ocean.GetWaterHeightAt(transform.position.x);
        float nextWaveHeight = ocean.GetWaterHeightAt(transform.position.x + 0.5f);
        float waveCurvature = nextWaveHeight - waveHeight;
        Vector2 curvatureForce = Vector2.up * waveCurvature * verticalForceMultiplier * ocean.GetStormIntensity();
        rb.AddForce(curvatureForce);
        
        float turbulence = Mathf.PerlinNoise(Time.time * 2f, transform.position.x * 0.1f) * 2f - 1f;
        Vector2 turbulenceForce = new Vector2(turbulence, Mathf.Abs(turbulence) * 0.3f) * turbulenceStrength * ocean.GetStormIntensity();
        rb.AddForce(turbulenceForce);
        
        if(showDebug) Debug.DrawRay(transform.position, waveNormal * 2f, Color.yellow);
    }
    
    void PreventExcessiveSinking(){
        if(!inWater) return;
        
        float centerDepth = ocean.GetWaterHeightAt(transform.position.x) - transform.position.y;
        
        if(centerDepth > cachedDraft * 2f){
            float excessDepth = centerDepth - cachedDraft;
            Vector2 antiSinkForce = Vector2.up * buoyancyForce * excessDepth * 2f;
            rb.AddForce(antiSinkForce);
            
            if(showDebug) Debug.DrawRay(transform.position, antiSinkForce * 0.1f, Color.magenta);
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
                Debug.DrawLine(worldPoint, new Vector3(worldPoint.x, debugWaterLevels[i].y, 0), Color.Lerp(Color.cyan, Color.blue, debugSubmersionDepths[i] / cachedDraft));
        }
    }
    
    void DrawBoatOutline(){
        float halfLength = cachedLength * 0.5f;
        float halfHeight = cachedHeight * 0.5f;
        
        Vector2 front = transform.TransformPoint(new Vector2(halfLength, 0));
        Vector2 back = transform.TransformPoint(new Vector2(-halfLength, 0));
        Vector2 frontTop = transform.TransformPoint(new Vector2(halfLength, halfHeight));
        Vector2 backTop = transform.TransformPoint(new Vector2(-halfLength, halfHeight));
        Vector2 frontBottom = transform.TransformPoint(new Vector2(halfLength, -halfHeight));
        Vector2 backBottom = transform.TransformPoint(new Vector2(-halfLength, -halfHeight));
        
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
        Vector3 size = new Vector3(objLength * transform.localScale.x, objHeight * transform.localScale.y, 0.1f);
        Gizmos.DrawWireCube(center, size);
        
        Gizmos.color = Color.cyan;
        float draftWorld = objDraft * transform.localScale.y;
        Gizmos.DrawLine(
            new Vector3(center.x - objLength * 0.5f * transform.localScale.x, center.y - draftWorld, 0),
            new Vector3(center.x + objLength * 0.5f * transform.localScale.x, center.y - draftWorld, 0)
        );
    }

    public bool InWater => inWater;
    public float WaveHeightAtPosition(float x) => ocean.GetWaterHeightAt(x);
    public float WaveSlopeAtPosition(float x) => ocean.GetWaterNormalAt(x).x;
    public float WaveVelocityAtPosition(float x) => waveVelocity;
}
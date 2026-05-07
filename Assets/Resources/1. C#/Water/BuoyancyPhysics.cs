using UnityEngine;
using NaughtyAttributes;
using System.Collections.Generic;

public class BuoyancyPhysics : MonoBehaviour
{
    [Foldout("BUOYANCY")][SerializeField] [Tooltip("How strongly water pushes up. Higher = more floaty. Recommended: 500-1500 for large boats")] private float buoyancyForce = 25f;
    [Foldout("BUOYANCY")][SerializeField] [Tooltip("Smooths out bobbing motion. Higher = less springy")] private float buoyancyDamping = 3f;
    [Foldout("BUOYANCY")][SerializeField] [Tooltip("Resistance when moving through water. Higher = slower movement")] private float waterDrag = 2f;
    [Foldout("BUOYANCY")][SerializeField] [Tooltip("Resistance when spinning in water. Higher = less rotation")] private float waterAngularDrag = 1.5f;
    [Foldout("BUOYANCY")][Range(0f, 1f)] [Tooltip("How much of the boat sits underwater. 0.3 = 30% submerged")] public float targetSubmersion = 0.4f;

    [Foldout("WAVE RIDING")][SerializeField] [Tooltip("How strongly the boat follows wave slopes. Higher = more tilt")] private float waveFollowStrength = 1.2f;
    [Foldout("WAVE RIDING")][SerializeField] [Tooltip("Multiplier for wave rotation torque. Higher = more dramatic tilting")] private float waveTorqueMultiplier = 2f;
    [Foldout("WAVE RIDING")][SerializeField] [Tooltip("Maximum angle the boat can tilt (degrees)")] private float maxTiltAngle = 35f;
    [Foldout("WAVE RIDING")][SerializeField] [Tooltip("How quickly boat returns to level. Higher = faster self-correction")] private float selfRightingStrength = 0.5f;

    [Foldout("CHAOS FORCES")][SerializeField] [Tooltip("How strongly waves push boat sideways")] private float horizontalWavePush = 1.5f;
    [Foldout("CHAOS FORCES")][SerializeField] [Tooltip("How strongly wave curvature lifts/drops the boat")] private float verticalWavePush = 1.2f;
    [Foldout("CHAOS FORCES")][SerializeField] [Tooltip("Random water chop intensity. Higher = more chaotic bobbing")] private float chopIntensity = 1.2f;
    [Foldout("CHAOS FORCES")][SerializeField] [Tooltip("How strongly boat is pulled to wave surface")] private float waveAttraction = 8f;

    [Foldout("PHYSICS")][SerializeField] [Tooltip("Gravity strength. Lower = floatier feeling. 0 = no gravity, 1 = normal")] private float gravityScale = 0.3f;
    [Foldout("PHYSICS")][SerializeField] [Tooltip("Resistance when boat is flying through air")] private float airDrag = 0.3f;
    [Foldout("PHYSICS")][SerializeField] [Tooltip("Rotation resistance when boat is in air")] private float airAngularDrag = 0.8f;
    [Foldout("PHYSICS")][SerializeField] [Tooltip("Boat weight in kilograms. Heavier = harder to lift")] private float mass = 800f;

    [Foldout("DIMENSIONS")][SerializeField] [Tooltip("Automatically detect size from sprite/collider bounds")] private bool autoDetectBounds = true;
    [Foldout("DIMENSIONS")][SerializeField] [Tooltip("Distance from LEFT to RIGHT edge of boat (world units)")] private float boatLength = 3f;
    [Foldout("DIMENSIONS")][SerializeField] [Tooltip("Distance from BOTTOM to TOP edge of boat (world units)")] private float boatHeight = 1.2f;
    [Foldout("DIMENSIONS")][SerializeField] [Tooltip("How deep the hull sits underwater (from bottom edge)")] private float draftDepth = 0.4f;
    [Foldout("DIMENSIONS")][SerializeField] [Tooltip("Number of buoyancy calculation points along boat length. More = smoother but heavier")] private int buoyancyPointCount = 5;

    [Foldout("VISUAL")][SerializeField] [Tooltip("Show debug lines and gizmos in scene view")] private bool showDebug = true;
    [Foldout("VISUAL")][SerializeField] [Tooltip("Show water line visual effect")] private bool showWaterLine = true;
    [Foldout("VISUAL")][SerializeField] [Tooltip("Color of the water line visual")] private Color waterLineColor = Color.cyan;

    [Required, Foldout("REFERENCES")][SerializeField] private Rigidbody2D rb;
    [Required, Foldout("REFERENCES")][SerializeField] private SpriteRenderer spriteRenderer;
    [Required, Foldout("REFERENCES")][SerializeField] private Collider2D boundsCollider;
    
    private StormyOcean ocean;
    private bool inWater = true;
    private Vector2[] buoyancyPoints;
    private LineRenderer waterLineRenderer;
    
    private float cachedLength;
    private float cachedHeight;
    private float cachedDraft;
    private Vector3 lastScale;
    private float lastWaveHeight;
    private float waveVelocity;
    private float currentHeightVelocity;

    void Start(){
        ocean = FindFirstObjectByType<StormyOcean>();
        if(spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if(boundsCollider == null) boundsCollider = GetComponent<Collider2D>();
        
        rb.mass = mass;
        rb.gravityScale = gravityScale;
        rb.angularDamping = waterAngularDrag;
        
        UpdateDimensions();
        CreateBuoyancyPoints();
        CreateWaterLineVisual();
        
        lastScale = transform.localScale;
    }
    
    void Update(){
        if(transform.localScale != lastScale){
            UpdateDimensions();
            CreateBuoyancyPoints();
            lastScale = transform.localScale;
        }
        
        if(showDebug) DebugDraw();
        UpdateWaterLineVisual();
    }
    
    void UpdateDimensions(){
        if(!autoDetectBounds) return;
        
        if(spriteRenderer != null && spriteRenderer.sprite != null){
            Bounds bounds = spriteRenderer.bounds;
            boatLength = bounds.size.x / transform.localScale.x;
            boatHeight = bounds.size.y / transform.localScale.y;
            
            Vector3 localBottom = transform.InverseTransformPoint(bounds.min);
            draftDepth = Mathf.Abs(localBottom.y) * 0.6f;
        }
        else if(boundsCollider != null){
            Bounds bounds = boundsCollider.bounds;
            boatLength = bounds.size.x / transform.localScale.x;
            boatHeight = bounds.size.y / transform.localScale.y;
        }
        
        if(draftDepth <= 0) draftDepth = boatHeight * 0.3f;
    }
    
    [ContextMenu("Recreate Buoyancy Points")]
        void CreateBuoyancyPoints(){
        buoyancyPoints = new Vector2[buoyancyPointCount];
        
        float halfLength = cachedLength > 0 ? cachedLength * 0.5f : boatLength * 0.5f;
        
        float bottomY = 0f;
        if(spriteRenderer != null){
            Vector3 localBottom = transform.InverseTransformPoint(spriteRenderer.bounds.min);
            bottomY = localBottom.y;
        }
        else if(boundsCollider != null){
            Vector3 localBottom = transform.InverseTransformPoint(boundsCollider.bounds.min);
            bottomY = localBottom.y;
        }
        else bottomY = -cachedHeight * 0.5f;
        
        float buoyancyY = bottomY;
        for(int i = 0; i < buoyancyPointCount; i++){
            float t = i / (float)(buoyancyPointCount - 1);
            float x = Mathf.Lerp(-halfLength, halfLength, t);
            buoyancyPoints[i] = new Vector2(x, buoyancyY);
        }
    }
    
    void CreateWaterLineVisual(){
        GameObject lineObj = new GameObject("WaterLine");
        lineObj.transform.SetParent(transform);
        lineObj.transform.localPosition = Vector3.zero;
        waterLineRenderer = lineObj.AddComponent<LineRenderer>();
        waterLineRenderer.startWidth = 0.05f;
        waterLineRenderer.endWidth = 0.05f;
        waterLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        waterLineRenderer.startColor = waterLineColor;
        waterLineRenderer.endColor = waterLineColor;
        waterLineRenderer.positionCount = 2;
        waterLineRenderer.enabled = showWaterLine;
    }
    
    void UpdateWaterLineVisual(){
        if(waterLineRenderer == null) return;
        waterLineRenderer.enabled = showWaterLine && inWater;
        
        if(inWater && ocean != null){
            float waterHeight = ocean.GetWaterHeightAt(transform.position.x);
            float halfLength = cachedLength * 0.5f;
            
            Vector3 leftPoint = new Vector3(transform.position.x - halfLength, waterHeight, 0);
            Vector3 rightPoint = new Vector3(transform.position.x + halfLength, waterHeight, 0);
            
            waterLineRenderer.SetPosition(0, leftPoint);
            waterLineRenderer.SetPosition(1, rightPoint);
        }
    }
    
    void FixedUpdate(){
        if(ocean == null || rb == null) return;
        
        cachedLength = boatLength * transform.localScale.x;
        cachedHeight = boatHeight * transform.localScale.y;
        cachedDraft = draftDepth * transform.localScale.y;
        
        UpdateWaterStatus();
        ApplyChaosBuoyancy();
        ApplyWaveForces();
        ApplyWaveRotation();
        SmoothWaveFollowing();
        LimitExcessTilt();
        
        float currentWaveHeight = ocean.GetWaterHeightAt(transform.position.x);
        float deltaX = transform.position.x - lastWaveHeight;
        if(deltaX != 0) waveVelocity = (currentWaveHeight - lastWaveHeight) / deltaX * 5f;
        lastWaveHeight = currentWaveHeight;
    }
    
    void UpdateWaterStatus(){
        float waterHeight = ocean.GetWaterHeightAt(transform.position.x);
        float boatBottom = transform.position.y - cachedHeight * 0.5f;
        bool wasInWater = inWater;
        inWater = boatBottom < waterHeight;
        
        if(inWater){
            rb.linearDamping = waterDrag;
            rb.angularDamping = waterAngularDrag;
        } else {
            rb.linearDamping = airDrag;
            rb.angularDamping = airAngularDrag;
        }
    }
    
    void ApplyChaosBuoyancy(){
        if(!inWater) return;
        
        for(int i = 0; i < buoyancyPoints.Length; i++){
            Vector2 worldPoint = transform.TransformPoint(buoyancyPoints[i]);
            float waterHeight = ocean.GetWaterHeightAt(worldPoint.x);
            float submergedDepth = waterHeight - worldPoint.y;
            
            if(submergedDepth > 0){
                float forceMultiplier = Mathf.Clamp01(submergedDepth / cachedDraft);
                float pointForce = buoyancyForce * forceMultiplier;
                
                if(submergedDepth > cachedDraft) pointForce *= (submergedDepth / cachedDraft);
                
                Vector2 force = Vector2.up * pointForce;
                rb.AddForceAtPosition(force, worldPoint);
            }
        }
}
    
    void ApplyWaveForces(){
        if(!inWater) return;
        
        Vector2 waveNormal = ocean.GetWaterNormalAt(transform.position.x);
        float intensity = ocean.GetStormIntensity();
        
        Vector2 slopeForce = new Vector2(-waveNormal.x * horizontalWavePush, Mathf.Abs(waveNormal.x) * 0.5f) * intensity;
        rb.AddForce(slopeForce);
        
        float waveHeight = ocean.GetWaterHeightAt(transform.position.x);
        float nextWave = ocean.GetWaterHeightAt(transform.position.x + 0.3f);
        float curvature = (nextWave - waveHeight) * verticalWavePush * intensity;
        rb.AddForce(Vector2.up * curvature);
        
        float chop = Mathf.PerlinNoise(Time.time * 3f, transform.position.x * 0.5f) * 2f - 1f;
        rb.AddForce(new Vector2(chop * chopIntensity, Mathf.Abs(chop) * 0.5f) * intensity);
    }
    
    void ApplyWaveRotation(){
        if(!inWater) return;
        
        Vector2 waveNormal = ocean.GetWaterNormalAt(transform.position.x);
        float targetAngle = -waveNormal.x * maxTiltAngle * ocean.GetStormIntensity();
        targetAngle = Mathf.Clamp(targetAngle, -maxTiltAngle, maxTiltAngle);
        
        float currentAngle = transform.eulerAngles.z;
        if(currentAngle > 180) currentAngle -= 360;
        
        float angleDiff = Mathf.DeltaAngle(currentAngle, targetAngle);
        float torque = angleDiff * waveFollowStrength * waveTorqueMultiplier;
        torque -= rb.angularVelocity * waterAngularDrag;
        
        rb.AddTorque(torque);
    }
    
    void SmoothWaveFollowing(){
        if(!inWater) return;
        
        float targetHeight = 0;
        foreach(Vector2 point in buoyancyPoints){
            targetHeight += ocean.GetWaterHeightAt(transform.TransformPoint(point).x);
        }
        targetHeight = (targetHeight / buoyancyPoints.Length) + cachedDraft;
        
        float heightDiff = targetHeight - transform.position.y;
        float springForce = heightDiff * waveAttraction;
        float damping = -currentHeightVelocity * buoyancyDamping;
        float acceleration = springForce + damping;
        
        currentHeightVelocity += acceleration * Time.fixedDeltaTime;
        Vector2 vel = rb.linearVelocity;
        vel.y += currentHeightVelocity * Time.fixedDeltaTime;
        rb.linearVelocity = vel;
    }
    
    void LimitExcessTilt(){
        float currentAngle = transform.eulerAngles.z;
        if(currentAngle > 180) currentAngle -= 360;
        
        if(Mathf.Abs(currentAngle) > maxTiltAngle + 15f){
            float returnTorque = -currentAngle * selfRightingStrength * 3f;
            rb.AddTorque(returnTorque);
        }
    }
    
    void DebugDraw(){
        if(buoyancyPoints == null || ocean == null) return;
        
        float halfLength = cachedLength * 0.5f;
        float halfHeight = cachedHeight * 0.5f;
        
        Vector2 front = transform.TransformPoint(new Vector2(halfLength, 0));
        Vector2 back = transform.TransformPoint(new Vector2(-halfLength, 0));
        Debug.DrawLine(front, back, Color.white);
        
        for(int i = 0; i < buoyancyPoints.Length; i++){
            Vector2 worldPoint = transform.TransformPoint(buoyancyPoints[i]);
            float waterHeight = ocean.GetWaterHeightAt(worldPoint.x);
            bool submerged = worldPoint.y < waterHeight;
            
            Debug.DrawRay(worldPoint - Vector2.right * 0.1f, Vector2.right * 0.2f, submerged ? Color.green : Color.red);
            if(showWaterLine) Debug.DrawLine(worldPoint, new Vector3(worldPoint.x, waterHeight, 0), Color.cyan);
        }
        
        Vector2 waveNormal = ocean.GetWaterNormalAt(transform.position.x);
        Debug.DrawRay(transform.position, waveNormal * 2f, Color.magenta);
        
        float waterAtCenter = ocean.GetWaterHeightAt(transform.position.x);
        Debug.DrawLine(new Vector3(transform.position.x - halfLength, waterAtCenter), new Vector3(transform.position.x + halfLength, waterAtCenter), Color.blue);
    }
    
    void OnDrawGizmosSelected(){
        if(!showDebug) return;
        
        Gizmos.color = Color.yellow;
        Vector3 size = new Vector3(boatLength * transform.localScale.x, boatHeight * transform.localScale.y, 0.1f);
        Gizmos.DrawWireCube(transform.position, size);
        
        Gizmos.color = Color.green;
        float bottomY = transform.position.y - (boatHeight * transform.localScale.y * 0.5f);
        float buoyancyLineY = bottomY + (draftDepth * transform.localScale.y);
        Gizmos.DrawLine(
            new Vector3(transform.position.x - boatLength * 0.5f * transform.localScale.x, buoyancyLineY),
            new Vector3(transform.position.x + boatLength * 0.5f * transform.localScale.x, buoyancyLineY)
        );
        
        if(buoyancyPoints != null){
            Gizmos.color = Color.red;
            foreach(var point in buoyancyPoints) Gizmos.DrawWireSphere(transform.TransformPoint(point), 0.08f);
        }
    }
    
    public bool InWater => inWater;
    public float GetWaveHeight(float x) => ocean?.GetWaterHeightAt(x) ?? 0;
    public float GetWaveSlope(float x) => ocean?.GetWaterNormalAt(x).x ?? 0;

    public float WaveHeightAtPosition(float x) => ocean?.GetWaterHeightAt(x) ?? 0;
    public float WaveSlopeAtPosition(float x) => ocean?.GetWaterNormalAt(x).x ?? 0;
    public float WaveVelocityAtPosition(float x) => waveVelocity;
}
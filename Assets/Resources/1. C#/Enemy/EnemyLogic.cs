using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using NaughtyAttributes;

public abstract class EnemyLogic : MonoBehaviour, IDamageable
{
    [Header("REFERENCES")]
    [SerializeField] protected EnemyData enemyData;
    [ReadOnly] public Transform player;
    [ReadOnly] public Camera playerCamera;
    
    [Header("WANDER AREA")]
    protected Vector2 wanderAreaCenter;
    protected Vector2 wanderAreaSize;
    protected bool hasWanderArea = false;
    protected Vector2 currentWanderTarget;
    protected float wanderTargetChangeTimer;
    
    [Header("FLEE CONFINER")]
    protected BoxCollider2D fleeConfiner;
    protected bool isFleeing = false;
    
    public System.Action<GameObject> OnEnemyDestroyed;
    public bool IsAlive => isAlive;

    protected SpriteRenderer spriteRenderer;
    protected Rigidbody2D rb;
    protected int currentHealth;
    protected bool isAlive = true;
    protected bool isAvoiding = false;
    protected bool hasBeenAttacked = false;
    protected Mouse mouse;
    
    protected virtual void Start(){
        InitializeComponents();
        FindPlayer();
        SetupEnemy();
        SetNewWanderTarget();

        mouse = Mouse.current;
    }
    
    protected virtual void InitializeComponents(){
        rb = GetComponent<Rigidbody2D>();
        if(rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        
        spriteRenderer = GetComponent<SpriteRenderer>();
        if(spriteRenderer == null) spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        
        rb.gravityScale = 0f;
        rb.linearDamping = 2f;
        
        if(GetComponent<Collider2D>() == null){
            var collider = gameObject.AddComponent<CircleCollider2D>();
            collider.radius = 0.5f;
        }
    }
    
    protected virtual void FindPlayer(){
        if(player == null){
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if(playerObj != null) player = playerObj.transform;
        }
        
        if(playerCamera == null && Camera.main != null) playerCamera = Camera.main;
    }
    
    protected virtual void SetupEnemy(){
        if(enemyData == null){
            Debug.LogError("EnemyData not assigned to " + gameObject.name);
            enabled = false;
            return;
        }
        
        currentHealth = enemyData.maxHealth;
        
        if(spriteRenderer != null){
            spriteRenderer.sprite = enemyData.enemySprite;
            spriteRenderer.color = enemyData.spriteColor;
        }
    }
    
    protected virtual void Update(){
        if(!isAlive || player == null) return;
        CheckFleeBounds();
    }
    
    protected virtual void FixedUpdate(){
        if(!isAlive || player == null) return;
    }
    
    protected void SetNewWanderTarget(){
        if(!hasWanderArea) return;
        
        currentWanderTarget = GetRandomPointInWanderArea();
        wanderTargetChangeTimer = Random.Range(2f, 5f);
    }
    
    protected virtual void HandleWanderMovement(){
        if(!hasWanderArea) return;
        
        wanderTargetChangeTimer -= Time.fixedDeltaTime;
        
        if(wanderTargetChangeTimer <= 0 || Vector2.Distance(transform.position, currentWanderTarget) < 0.5f)
            SetNewWanderTarget();
        
        Vector2 directionToTarget = (currentWanderTarget - (Vector2)transform.position).normalized;
        rb.linearVelocity = directionToTarget * enemyData.movementSpeed;
        
        if(rb.linearVelocity.magnitude > 0.1f){
            float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, 0, angle), Time.fixedDeltaTime * 10f);
        }
    }
    
    protected virtual Vector2 GetRandomPointInWanderArea(){
        float randomX = Random.Range(wanderAreaCenter.x - wanderAreaSize.x / 2, wanderAreaCenter.x + wanderAreaSize.x / 2);
        float randomY = Random.Range(wanderAreaCenter.y - wanderAreaSize.y / 2, wanderAreaCenter.y + wanderAreaSize.y / 2);
        return new Vector2(randomX, randomY);
    }
    
    protected virtual void HandleMovement(){
        Vector2 directionToPlayer = ((Vector2)player.position - (Vector2)transform.position).normalized;
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        
        float minStopDistance = enemyData.stopRadius - enemyData.stopRadiusTolerance;
        float maxStopDistance = enemyData.stopRadius + enemyData.stopRadiusTolerance;
        
        Vector2 desiredVelocity;
        
        if(ShouldAvoidAiming()){
            desiredVelocity = GetAvoidanceVelocity();
            isAvoiding = true;
        }
        else{
            isAvoiding = false;
            
            if(distanceToPlayer > maxStopDistance) desiredVelocity = directionToPlayer * enemyData.movementSpeed;
            else if(distanceToPlayer < minStopDistance) desiredVelocity = -directionToPlayer * enemyData.movementSpeed * 0.5f;
            else desiredVelocity = Random.insideUnitCircle * (enemyData.movementSpeed * 0.3f);
        }
        
        rb.linearVelocity = desiredVelocity;
        
        if(rb.linearVelocity.magnitude > 0.1f){
            float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, 0, angle), Time.fixedDeltaTime * 10f);
        }
    }
    
    protected virtual float GetDistanceToShootLine(){
        if(PlayerShooting.Instance == null) return float.MaxValue;
        if(!PlayerShooting.Instance.IsAiming) return float.MaxValue;
        
        Transform firePoint = PlayerShooting.Instance.GetFirePoint();
        if(firePoint == null) return float.MaxValue;
        
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        Vector2 shootDirection = (mouseWorldPos - firePoint.position).normalized;
        Vector2 shootOrigin = firePoint.position;
        
        Vector2 enemyPos = transform.position;
        
        float t = Vector2.Dot(enemyPos - shootOrigin, shootDirection);
        if(t < 0) return float.MaxValue;

        Vector2 closestPoint = shootOrigin + shootDirection * t;
        float distance = Vector2.Distance(enemyPos, closestPoint);

        Debug.DrawLine(shootOrigin, shootOrigin + shootDirection * 50f, Color.white);
        Debug.DrawLine(transform.position, closestPoint, Color.red);
        
        return distance;
    }
    
    protected virtual Vector3 GetMouseWorldPosition(){
        if(mouse == null || playerCamera == null) return Vector3.zero;
        
        Vector2 mouseScreenPos = mouse.position.ReadValue();
        Vector3 mouseWorldPos = playerCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, 0));
        mouseWorldPos.z = 0;
        return mouseWorldPos;
    }
    
    protected virtual Vector2 GetAvoidanceVelocity(){
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        Vector2 directionToPlayer = ((Vector2)player.position - (Vector2)transform.position).normalized;
        Vector2 directionFromMouse = ((Vector2)transform.position - (Vector2)mouseWorldPos).normalized;
        Vector2 avoidanceDir = (directionFromMouse + GetPerpendicularDirection(directionToPlayer)).normalized;
        
        return avoidanceDir * enemyData.avoidSpeed;
    }

    protected virtual void ApplyAvoidance(){
        if(ShouldAvoidAiming()){
            Vector2 avoidanceVel = GetAvoidanceVelocity();
            if(rb.linearVelocity.magnitude > 0.1f) rb.linearVelocity = (rb.linearVelocity + avoidanceVel).normalized * rb.linearVelocity.magnitude;
            else rb.linearVelocity = avoidanceVel;
        }
    }

    protected virtual bool ShouldAvoidAiming(){
        if(enemyData.avoidAimingRadius <= 0) return false;
        
        float distanceToShootLine = GetDistanceToShootLine();
        if(distanceToShootLine == float.MaxValue) return false;
        return distanceToShootLine < enemyData.avoidAimingRadius;
    }
    
    protected virtual Vector2 GetPerpendicularDirection(Vector2 direction){
        float randomChoice = Random.Range(-1f, 1f);
        return new Vector2(-direction.y, direction.x) * Mathf.Sign(randomChoice);
    }
    
    protected virtual void CheckFleeBounds(){
        if(!isFleeing) return;
        if(fleeConfiner == null) return;
        
        if(!fleeConfiner.bounds.Contains(transform.position)){
            OnEnemyDestroyed?.Invoke(gameObject);
            Destroy(gameObject);
        }
    }
    
    protected virtual void EnterFleeingState() => isFleeing = true;
    protected virtual void ExitFleeingState() => isFleeing = false;
    
    public virtual void SetWanderArea(Vector2 center, Vector2 size){
        wanderAreaCenter = center;
        wanderAreaSize = size;
        hasWanderArea = true;
        SetNewWanderTarget();
    }
    public virtual void SetFleeConfiner(BoxCollider2D confiner) => fleeConfiner = confiner;
    
    public virtual void TakeDamage(int damage){
        if(!isAlive) return;
        
        currentHealth -= damage;
        hasBeenAttacked = true;
        StartCoroutine(DamageFlash());
        if(currentHealth <= 0) Die();
    }
    
    protected virtual IEnumerator DamageFlash(){
        if(spriteRenderer != null){
            Color originalColor = spriteRenderer.color;
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = originalColor;
        }
    }
    
    public virtual void Die(){
        isAlive = false;
        OnEnemyDestroyed?.Invoke(gameObject);

        GameManager.Instance.AddScore(enemyData.scoreValue);
        Destroy(gameObject, 0.5f);
    }
}
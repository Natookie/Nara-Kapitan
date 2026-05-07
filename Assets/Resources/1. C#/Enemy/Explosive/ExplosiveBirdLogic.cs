using UnityEngine;
using NaughtyAttributes;

public class ExplosiveBirdLogic : EnemyLogic
{
    private ExplosiveBirdData ExplosiveData => enemyData as ExplosiveBirdData;
    
    public enum ExplosiveState
    {
        Wandering,
        Fleeing,
        Kamikaze
    }
    [ReadOnly] public ExplosiveState currentState = ExplosiveState.Wandering;

    private bool hasExploded = false;
    private float stateTimer;
    private float lastShotDetectionTime;
    private Vector2 fleeDirection;
    
    protected override void Start(){
        base.Start();
        currentState = ExplosiveState.Wandering;
        stateTimer = Random.Range(ExplosiveData.wanderBeforeFleeMin, ExplosiveData.wanderBeforeFleeMax);
    }
    
    protected override void Update(){
        base.Update();
        
        if(!isAlive || player == null) return;
        
        if(PlayerShooting.Instance != null && PlayerShooting.Instance.IsAiming) lastShotDetectionTime = Time.time;
        bool playerJustShot = Time.time - lastShotDetectionTime < ExplosiveData.kamikazeDetectionWindow;
        
        if(playerJustShot && currentState != ExplosiveState.Kamikaze){
            currentState = ExplosiveState.Kamikaze;
            return;
        }
        
        switch(currentState){
            case ExplosiveState.Wandering: HandleWanderingState(); break;
            case ExplosiveState.Fleeing: HandleFleeingState(); break;
            case ExplosiveState.Kamikaze: HandleKamikazeState(); break;
        }
    }

    protected override bool ShouldAvoidAiming() => false; 
    protected override void ApplyAvoidance(){
        return;
    }

    void HandleWanderingState(){
        stateTimer -= Time.deltaTime;
        
        HandleWanderMovement();
        ApplyAvoidance();
        
        if(stateTimer <= 0){
            currentState = ExplosiveState.Fleeing;
            EnterFleeingState();
            fleeDirection = ((Vector2)transform.position - (Vector2)player.position).normalized;
        }
    }
    
    void HandleFleeingState(){
        rb.linearVelocity = fleeDirection * ExplosiveData.movementSpeed;
        RotateTowardsVelocity();
        ApplyAvoidance();
        CheckFleeBounds();
    }
    
    void HandleKamikazeState(){
        Vector2 directionToPlayer = ((Vector2)player.position - (Vector2)transform.position).normalized;
        rb.linearVelocity = directionToPlayer * ExplosiveData.kamikazeSpeed;
        RotateTowardsVelocity();
        ApplyAvoidance();
    }
    
    void RotateTowardsVelocity(){
        if(rb.linearVelocity.magnitude > 0.1f){
            float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, 0, angle), Time.deltaTime * 10f);
        }
    }
    
    protected virtual void OnTriggerEnter2D(Collider2D other){
        if(currentState == ExplosiveState.Kamikaze && (other.CompareTag("Player")))
            Explode();
    }
    
    void Explode(){
        if(hasExploded) return;
        if(ExplosiveData.explosionEffect != null) Instantiate(ExplosiveData.explosionEffect, transform.position, Quaternion.identity);
        
        hasExploded = true;
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, ExplosiveData.explosionRadius);
        foreach(var hit in hitColliders){
            IDamageable damageable = hit.GetComponent<IDamageable>();
            damageable?.TakeDamage(ExplosiveData.explosionDamage);
        }
        
        OnEnemyDestroyed?.Invoke(gameObject);
        Destroy(gameObject);
    }
    
    public override void Die(){
        if(hasExploded) return;
        Explode();
    }
    
    protected override void EnterFleeingState(){
        base.EnterFleeingState();
        fleeDirection = ((Vector2)transform.position - (Vector2)player.position).normalized;
    }
}
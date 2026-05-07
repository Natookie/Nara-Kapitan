using UnityEngine;
using NaughtyAttributes;

public class RoboticBirdLogic : EnemyLogic
{
    private RoboticBirdData RobotData => enemyData as RoboticBirdData;
    
    public enum RoboticState
    {
        Wandering,
        Approaching,
        Fleeing,
        Attacking
    }
    [ReadOnly] public RoboticState currentState = RoboticState.Wandering;
    
    private Transform foodCrate;
    private float stateTimer;
    private bool hasStolenFood = false;
    private Vector2 fleeDirection;
    private Rigidbody2D playerRb;
    
    protected override void Start(){
        base.Start();
        FindFoodCrate();
        currentState = RoboticState.Wandering;
        playerRb = player.GetComponent<Rigidbody2D>();
    }
    
    void FindFoodCrate(){
        GameObject crate = GameObject.FindGameObjectWithTag("FoodCrate");
        foodCrate = crate != null ? crate.transform : null;
        if(foodCrate == null) Debug.LogWarning("No FoodCrate found with tag 'FoodCrate'");
    }
    
    protected override void Update(){
        base.Update();
        
        if(!isAlive || player == null) return;
        
        if(hasBeenAttacked && currentState != RoboticState.Attacking && currentState != RoboticState.Fleeing){
            currentState = RoboticState.Attacking;
            stateTimer = 0f;
            return;
        }
        
        switch(currentState){
            case RoboticState.Wandering: HandleWanderingState(); break;
            case RoboticState.Approaching: HandleApproachingState(); break;
            case RoboticState.Fleeing: HandleFleeingState(); break;
            case RoboticState.Attacking: HandleAttackingState(); break;
        }
    }
    
    void HandleWanderingState(){
        stateTimer += Time.deltaTime;
        
        HandleWanderMovement();
        if(stateTimer >= Random.Range(RobotData.approachDelay.x, RobotData.approachDelay.y) && foodCrate != null){
            currentState = RoboticState.Approaching;
            stateTimer = 0f;
        }
    }
    
    void HandleApproachingState(){
        if(foodCrate == null){
            currentState = RoboticState.Wandering;
            return;
        }
        
        Vector2 directionToCrate = ((Vector2)foodCrate.position - (Vector2)transform.position).normalized;
        rb.linearVelocity = directionToCrate * RobotData.approachSpeed;
        
        float distanceToCrate = Vector2.Distance(transform.position, foodCrate.position);
        
        if(distanceToCrate < RobotData.stealRadius && !hasStolenFood){
            StealFood();
            hasStolenFood = true;
            currentState = RoboticState.Fleeing;
        }
        
        RotateTowardsVelocity();
    }
    
    void HandleFleeingState(){
        if(!isFleeing){
            EnterFleeingState();
            fleeDirection = ((Vector2)transform.position - (Vector2)player.position).normalized;
        }
        
        rb.linearVelocity = fleeDirection * RobotData.fleeSpeed;
        RotateTowardsVelocity();
        ApplyAvoidance();
        CheckFleeBounds();
    }
    
    void HandleAttackingState(){
        stateTimer += Time.deltaTime;
        
        if(stateTimer >= RobotData.attackDelay){
            ShootAtPlayer();
            stateTimer = 0;
        }
        
        HandleMovement();
    }
    
    void RotateTowardsVelocity(){
        if(rb.linearVelocity.magnitude > 0.1f){
            float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, 0, angle), Time.deltaTime * 10f);
        }
    }
    
    void ShootAtPlayer(){
        if(RobotData.projectilePrefab == null || player == null) return;
        
        Vector2 playerVelocity = playerRb != null ? playerRb.linearVelocity : Vector2.zero;
        
        Vector2 predictedTarget = CalculateInterceptionPoint(
            transform.position, 
            player.position, 
            playerVelocity, 
            RobotData.projectileSpeed
        );
        
        GameObject projectile = Instantiate(RobotData.projectilePrefab, transform.position, Quaternion.identity);
        EnemyProjectile projectileScript = projectile.GetComponent<EnemyProjectile>();
        
        if(projectileScript != null) projectileScript.Initialize(predictedTarget, RobotData.projectileDamage, RobotData.projectileSpeed);
        else{
            Rigidbody2D projRb = projectile.GetComponent<Rigidbody2D>();
            if(projRb != null){
                Vector2 direction = (predictedTarget - (Vector2)transform.position).normalized;
                projRb.linearVelocity = direction * RobotData.projectileSpeed;
            }
        }
    }
    
    Vector2 CalculateInterceptionPoint(Vector2 shooterPos, Vector2 targetPos, Vector2 targetVelocity, float projectileSpeed){
        Vector2 deltaPos = targetPos - shooterPos;
        float a = targetVelocity.sqrMagnitude - projectileSpeed * projectileSpeed;
        float b = 2 * Vector2.Dot(deltaPos, targetVelocity);
        float c = deltaPos.sqrMagnitude;
        
        float discriminant = b * b - 4 * a * c;
        if(discriminant < 0) return targetPos;
        
        float t = (-b - Mathf.Sqrt(discriminant)) / (2 * a);
        if(t < 0) t = (-b + Mathf.Sqrt(discriminant)) / (2 * a);
        if(t < 0) return targetPos;
        
        return targetPos + targetVelocity * t;
    }
    
    void StealFood(){
        Debug.Log($"Robotic Bird stole {RobotData.stealFoodAmount} food from the crate!");
    }
    
    public override void TakeDamage(int damage){
        base.TakeDamage(damage);
        
        if(currentState != RoboticState.Attacking && currentState != RoboticState.Fleeing)
            currentState = RoboticState.Fleeing;
    }
    
    protected override void EnterFleeingState(){
        base.EnterFleeingState();
        fleeDirection = ((Vector2)transform.position - (Vector2)player.position).normalized;
    }
}
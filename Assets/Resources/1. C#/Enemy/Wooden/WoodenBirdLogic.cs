using UnityEngine;
using NaughtyAttributes;

public class WoodenBirdLogic : EnemyLogic
{
    private WoodenBirdData WoodenData => enemyData as WoodenBirdData;
    
    public enum WoodenState
    {
        Wandering,
        Fleeing
    }
    [ReadOnly] public WoodenState currentState = WoodenState.Wandering;
    
    private float stateTimer;
    private Vector2 fleeDirection;
    
    protected override void Start(){
        base.Start();
        currentState = WoodenState.Wandering;
    }
    
    protected override void Update(){
        base.Update();
        
        if(!isAlive || player == null) return;
        
        if(hasBeenAttacked && currentState != WoodenState.Fleeing){
            currentState = WoodenState.Fleeing;
            EnterFleeingState();
            fleeDirection = ((Vector2)transform.position - (Vector2)player.position).normalized;
            stateTimer = WoodenData.fleeDuration;
            return;
        }
        
        switch(currentState){
            case WoodenState.Wandering: HandleWanderingState(); break;
            case WoodenState.Fleeing: HandleFleeingState(); break;
        }
    }
    
    private void HandleWanderingState(){
        HandleWanderMovement();
        ApplyAvoidance();
    }
    
    private void HandleFleeingState(){
        stateTimer -= Time.deltaTime;
        
        rb.linearVelocity = fleeDirection * WoodenData.fleeSpeed;
        
        if(rb.linearVelocity.magnitude > 0.1f){
            float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, 0, angle), Time.fixedDeltaTime * 10f);
        }
        
        ApplyAvoidance();
        
        if(stateTimer <= 0){
            currentState = WoodenState.Wandering;
            ExitFleeingState();
        }
    }
    
    protected override void EnterFleeingState(){
        base.EnterFleeingState();
        isFleeing = true;
    }
    
    protected override void ExitFleeingState(){
        base.ExitFleeingState();
        isFleeing = false;
    }
}
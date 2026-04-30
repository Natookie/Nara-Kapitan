using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Animator))]
public class PlayerAnimationManager : MonoBehaviour
{
    [Header("REFERENCES")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform playerTransform;
    
    [Header("ANIMATION STATE NAMES")]
    [SerializeField] private string idleState = "PlayerIdle";
    [SerializeField] private string runState = "PlayerRun";
    [SerializeField] private string anticipationState = "PlayerAnticipation";
    [SerializeField] private string jumpState = "PlayerJump";
    [SerializeField] private string fallState = "PlayerFall";
    [SerializeField] private string landState = "PlayerLand";
    
    [Header("TRANSITION SETTINGS")]
    [SerializeField] private float landAnimationLength = 0.2f;
    [SerializeField] private float jumpToFallThreshold = 0.8f;
    
    public string currentState;
    private bool wasGroundedLastFrame;
    private bool wasJumping;
    private float landTimer;
    private bool isLanding;
    private float lastHorizontalInput;
    private bool wasAnticipating;
    private bool isInJumpState;
    
    void Start(){
        if(playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
        if(animator == null) animator = GetComponent<Animator>();
        if(playerTransform == null) playerTransform = transform;
        
        wasGroundedLastFrame = playerMovement.IsGrounded;
        wasAnticipating = false;
        isInJumpState = false;
    }
    
    void Update(){
        if(playerMovement == null || animator == null) return;
        
        HandleAnimations();
        HandlePlayerFlip();
        wasGroundedLastFrame = playerMovement.IsGrounded;
    }
    
    void HandleAnimations(){
        bool isGrounded = playerMovement.IsGrounded;
        float horizontalInput = GetHorizontalInput();
        bool isMoving = Mathf.Abs(horizontalInput) > 0.1f;
        float verticalVelocity = playerMovement.GetVelocity.y;
        bool isAnticipating = playerMovement.IsAnticipating;
        
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsMoving", isMoving);
        
        if(!isInJumpState){
            animator.SetBool("ShouldJump", false);
            animator.SetBool("ShouldFall", false);
        }
        
        if(isAnticipating){
            if(!wasAnticipating){
                ChangeAnimationState(anticipationState);
                wasAnticipating = true;
            }
            return;
        }
        else{
            if(wasAnticipating){
                wasAnticipating = false;
                animator.SetBool("ShouldJump", true);
            }
        }
        
        if(isLanding){
            landTimer -= Time.deltaTime;
            if(landTimer <= 0){
                isLanding = false;
            }
            return;
        }
        
        if(!wasGroundedLastFrame && isGrounded && !isLanding){
            isLanding = true;
            landTimer = landAnimationLength;
            ChangeAnimationState(landState);
            isInJumpState = false;
            animator.SetBool("ShouldFall", false);
            return;
        }
        
        if(!isGrounded){
            if(verticalVelocity > 0.1f){
                if(!wasJumping && !isInJumpState){
                    wasJumping = true;
                    isInJumpState = true;
                }
            }
            else if(verticalVelocity <= 0.1f){
                if(isInJumpState){
                    AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                    float normalizedTime = stateInfo.normalizedTime % 1f;
                    
                    if(normalizedTime >= jumpToFallThreshold){
                        animator.SetBool("ShouldFall", true);
                        wasJumping = false;
                    }
                }
                else if(currentState != fallState){
                    ChangeAnimationState(fallState);
                    wasJumping = false;
                }
            }
        }
        else{
            wasJumping = false;
            isInJumpState = false;
            animator.SetBool("ShouldFall", false);
            
            if(currentState != idleState && currentState != runState && !isLanding){
                if(isMoving) ChangeAnimationState(runState);
                else ChangeAnimationState(idleState);
            }
        }
    }
    
    void HandlePlayerFlip(){
        float horizontalInput = GetHorizontalInput();
        
        if(Mathf.Abs(horizontalInput) > 0.1f) lastHorizontalInput = horizontalInput;
        float facingDirection = (Mathf.Abs(horizontalInput) > 0.1f) ? horizontalInput : lastHorizontalInput;
    }
    
    float GetHorizontalInput(){
        float input = 0f;
        if(Keyboard.current[Key.A].isPressed || Keyboard.current[Key.LeftArrow].isPressed) input = -1f;
        if(Keyboard.current[Key.D].isPressed || Keyboard.current[Key.RightArrow].isPressed) input = 1f;
        return input;
    }
    
    void ChangeAnimationState(string newState){
        if(currentState == newState) return;
        
        if(currentState == anticipationState && (newState == idleState || newState == runState)){
            return;
        }
        
        if((currentState == jumpState || currentState == fallState) && (newState == idleState || newState == runState)){
            if(!playerMovement.IsGrounded) return;
        }
        
        animator.Play(newState);
        currentState = newState;
    }
    
    #region PUBLIC METHODS
    public void ForceIdle(){
        if(!isLanding && currentState != anticipationState) 
            ChangeAnimationState(idleState);
    }
    
    public void ForceRun(){
        if(!isLanding && currentState != anticipationState) 
            ChangeAnimationState(runState);
    }
    
    public void ForceAnticipation(){
        if(!isLanding){
            ChangeAnimationState(anticipationState);
            wasAnticipating = true;
        }
    }
    
    public void ForceJump(){
        if(!isLanding){
            ChangeAnimationState(jumpState);
            wasJumping = true;
            wasAnticipating = false;
            isInJumpState = true;
        }
    }
    
    public void ForceFall(){
        if(!isLanding){
            ChangeAnimationState(fallState);
            wasJumping = false;
            wasAnticipating = false;
            isInJumpState = false;
        }
    }
    
    public string GetCurrentAnimationState() => currentState;
    public bool IsPlayingLandAnimation() => isLanding;
    public bool IsPlayingAnticipation() => wasAnticipating;
    #endregion
}
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
    
    private string currentState;
    private bool wasGroundedLastFrame;
    private bool wasJumping;
    private float landTimer;
    private bool isLanding;
    private float lastHorizontalInput;
    private bool wasAnticipating;
    
    void Start(){
        if(playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
        if(animator == null) animator = GetComponent<Animator>();
        if(playerTransform == null) playerTransform = transform;
        
        wasGroundedLastFrame = playerMovement.IsGrounded;
        wasAnticipating = false;
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
        
        if(isAnticipating){
            if(!wasAnticipating){
                ChangeAnimationState(anticipationState);
                wasAnticipating = true;
            }
            return;
        }
        else{
            wasAnticipating = false;
        }
        
        if(isLanding){
            landTimer -= Time.deltaTime;
            if(landTimer <= 0){
                isLanding = false;
                if(isMoving) ChangeAnimationState(runState);
                else ChangeAnimationState(idleState);
            }
            return;
        }
        
        if(!wasGroundedLastFrame && isGrounded && !isLanding){
            isLanding = true;
            landTimer = landAnimationLength;
            ChangeAnimationState(landState);
            return;
        }
        
        if(!isGrounded){
            if(verticalVelocity > 0.1f){
                if(!wasJumping){
                    ChangeAnimationState(jumpState);
                    wasJumping = true;
                }
            }
            else if(verticalVelocity <= 0.1f){
                if(wasJumping || currentState != fallState){
                    ChangeAnimationState(fallState);
                    wasJumping = false;
                }
            }
        }
        else{
            wasJumping = false;
            
            if(isMoving) ChangeAnimationState(runState);
            else ChangeAnimationState(idleState);
        }
    }
    
    void HandlePlayerFlip(){
        float horizontalInput = GetHorizontalInput();
        
        if(Mathf.Abs(horizontalInput) > 0.1f) lastHorizontalInput = horizontalInput;
        float facingDirection = (Mathf.Abs(horizontalInput) > 0.1f) ? horizontalInput : lastHorizontalInput;
        
        // if(facingDirection < 0) playerTransform.localScale = new Vector3(-1, 1, 1);
        // else if(facingDirection > 0) playerTransform.localScale = new Vector3(1, 1, 1);
    }
    
    float GetHorizontalInput(){
        float input = 0f;
        if(Keyboard.current[Key.A].isPressed || Keyboard.current[Key.LeftArrow].isPressed) input = -1f;
        if(Keyboard.current[Key.D].isPressed || Keyboard.current[Key.RightArrow].isPressed) input = 1f;
        return input;
    }
    
    void ChangeAnimationState(string newState){
        if(currentState == newState) return;
        animator.Play(newState);
        currentState = newState;
    }
    
    #region PUBLIC METHODS
    public void ForceIdle(){
        if(!isLanding) ChangeAnimationState(idleState);
    }
    
    public void ForceRun(){
        if(!isLanding) ChangeAnimationState(runState);
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
        }
    }
    
    public void ForceFall(){
        if(!isLanding){
            ChangeAnimationState(fallState);
            wasJumping = false;
            wasAnticipating = false;
        }
    }
    
    public string GetCurrentAnimationState() => currentState;
    public bool IsPlayingLandAnimation() => isLanding;
    public bool IsPlayingAnticipation() => wasAnticipating;
    #endregion
}
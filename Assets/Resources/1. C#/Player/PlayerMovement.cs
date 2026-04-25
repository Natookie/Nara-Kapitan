using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("GROUND MOVEMENT")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float acceleration = 15f;
    [SerializeField] private float deceleration = 20f;
    [SerializeField] private float turnSpeed = 25f;
    
    [Header("AIR MOVEMENT")]
    [SerializeField] private float airControl = 0.5f;
    [SerializeField] private float airAcceleration = 8f;
    
    private float horizontalInput;
    private Vector2 currentVelocity;
    
    [Header("JUMP")]
    [SerializeField] private float minJumpForce = 8f;
    [SerializeField] private float maxJumpForce = 20f;
    [SerializeField] private float maxAnticipationTime = 0.3f;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.1f;
    
    private bool isAnticipating;
    private float anticipationHoldTime;
    private bool isJumpHeld;
    private float coyoteTimer;
    private float jumpBufferTimer;
    
    [Header("DIVE")]
    [SerializeField] private float diveSpeed = 30f;
    [SerializeField] private float diveCancelJump = 8f;
    [SerializeField] private float minDownwardVelocityForDive = -2f;
    [SerializeField] private float groundCheckDistanceForDive = 1f;
    
    private bool isDiving;
    private bool canDive = true;
    
    [Header("DASH")]
    [SerializeField] private float dashForce = 25f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 0.5f;
    [SerializeField] private int maxAirDashes = 1;
    
    private bool isDashing;
    private bool canDash = true;
    private int airDashCount = 0;
    private float dashTimer;
    private float dashCooldownTimer;
    private Vector2 dashDirection;
    
    [Header("WALL SLIDE/JUMP")]
    [SerializeField] private float wallSlideSpeed = 3f;
    [SerializeField] private float wallJumpForce = 15f;
    [SerializeField] private float wallJumpHorizontalForce = 10f;
    [SerializeField] private float wallCheckDistance = 0.6f;
    
    private bool isWallSliding;
    private bool isWallLeft;
    private bool isWallRight;
    private Vector2 wallJumpDirection;
    
    [Header("STAMINA")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float wallSlideStaminaDrain = 20f;
    [SerializeField] private float dashStaminaCost = 30f;
    [SerializeField] private float staminaRegenRate = 15f;
    [SerializeField] private float staminaRegenDelay = 5f;
    
    private float currentStamina;
    private float staminaRegenTimer;
    private bool isUsingStamina;
    
    [Header("PHYSICS")]
    [SerializeField] private float groundDrag = 10f;
    [SerializeField] private float airDrag = 2f;
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float lowJumpMultiplier = 2f;
    [SerializeField] private LayerMask groundLayer;
    
    [Header("REFERENCES")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Camera mainCamera;
    
    private bool isGrounded;
    
    void Start(){
        currentStamina = maxStamina;
        if(mainCamera == null) mainCamera = Camera.main;
    }
    
    void Update(){
        CheckCollisions();
        HandleTimers();
        HandleInput();
        
        rb.linearDamping = (isGrounded) ? groundDrag : airDrag;
        UpdateSpriteFlip();
    }

    void FixedUpdate(){
        HandleStamina();
        
        if(isDashing) HandleDash();
        else if(isDiving) HandleDive();
        else{
            if(isWallSliding) HandleWallSlide();
            else HandleMovement();

            HandleJump();
            HandleGravity();
        }
    }
    
    void CheckCollisions(){
        Bounds spriteBounds = sr.bounds;
        
        Vector2 groundCheckPos = new Vector2(transform.position.x, spriteBounds.min.y);
        float groundCheckRadius = 0.2f;
        isGrounded = Physics2D.OverlapCircle(groundCheckPos, groundCheckRadius, groundLayer);
        
        Vector2 leftCheckPos = new Vector2(spriteBounds.min.x, transform.position.y);
        Vector2 rightCheckPos = new Vector2(spriteBounds.max.x, transform.position.y);
        
        isWallLeft = Physics2D.Raycast(leftCheckPos, Vector2.left, wallCheckDistance, groundLayer);
        isWallRight = Physics2D.Raycast(rightCheckPos, Vector2.right, wallCheckDistance, groundLayer);
        
        isWallSliding = !isGrounded && (isWallLeft || isWallRight) && rb.linearVelocity.y < 0;
        
        if(isGrounded && isDiving){
            isDiving = false;
            canDive = true;
        }
        if(isWallSliding) canDive = false;
        if(!isGrounded && !isWallSliding && !canDive) canDive = true;
        if(isGrounded) canDive = true;
        
        DebugDrawDiveRay();
        DebugDrawWallRays();
    }
    
    void DebugDrawDiveRay(){
        Bounds spriteBounds = sr.bounds;
        Vector2 groundCheckPos = new Vector2(transform.position.x, spriteBounds.min.y);
        bool groundNearby = Physics2D.Raycast(groundCheckPos, Vector2.down, groundCheckDistanceForDive, groundLayer);
        if(groundNearby) canDive = false;
        Debug.DrawRay(groundCheckPos, Vector2.down * groundCheckDistanceForDive, groundNearby ? Color.red : Color.green);
    }
    
    void DebugDrawWallRays(){
        Bounds spriteBounds = sr.bounds;
        Vector2 leftCheckPos = new Vector2(spriteBounds.min.x, transform.position.y);
        Vector2 rightCheckPos = new Vector2(spriteBounds.max.x, transform.position.y);
        Debug.DrawRay(leftCheckPos, Vector2.left * wallCheckDistance, isWallLeft ? Color.blue : Color.gray);
        Debug.DrawRay(rightCheckPos, Vector2.right * wallCheckDistance, isWallRight ? Color.blue : Color.gray);
    }
    
    void OnDrawGizmos(){
        if(sr != null){
            Bounds spriteBounds = sr.bounds;
            Vector2 groundCheckPos = new Vector2(transform.position.x, spriteBounds.min.y);
            float groundCheckRadius = 0.2f;
            
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheckPos, groundCheckRadius);
        }
    }
    
    void HandleTimers(){
        if(!isGrounded && coyoteTimer > 0) coyoteTimer -= Time.deltaTime;
        if(jumpBufferTimer > 0) jumpBufferTimer -= Time.deltaTime;
        if(dashTimer > 0) dashTimer -= Time.deltaTime;
        if(dashCooldownTimer > 0) dashCooldownTimer -= Time.deltaTime;
        
        if(isGrounded){
            coyoteTimer = coyoteTime;
            airDashCount = 0;
            canDash = true;
        }
        
        if(dashTimer <= 0 && isDashing) EndDash();
        if(staminaRegenTimer > 0) staminaRegenTimer -= Time.deltaTime;
    }
    
    #region INPUT HANDLING
    void HandleInput(){
        horizontalInput = 0f;
        if(Keyboard.current[Key.A].isPressed || Keyboard.current[Key.LeftArrow].isPressed) horizontalInput = -1f;
        if(Keyboard.current[Key.D].isPressed || Keyboard.current[Key.RightArrow].isPressed) horizontalInput = 1f;
        
        HandleJumpInput();
        HandleDiveInput();
        HandleDashInput();
    }
    
    void HandleJumpInput(){
        if(Keyboard.current[Key.Space].wasPressedThisFrame){
            if(isDiving){
                CancelDive();
                return;
            }
            
            if(isWallSliding){
                WallJump();
                return;
            }
            
            if((isGrounded || coyoteTimer > 0) && !isWallSliding && !isDiving && !isAnticipating){
                isAnticipating = true;
                anticipationHoldTime = 0f;
                return;
            }
            else if(!isGrounded && !isWallSliding && !isDiving){
                jumpBufferTimer = jumpBufferTime;
            }
        }
        
        if(isAnticipating){
            if(Keyboard.current[Key.Space].isPressed){
                anticipationHoldTime += Time.deltaTime;
                
                if(anticipationHoldTime >= maxAnticipationTime){
                    anticipationHoldTime = maxAnticipationTime;
                    PerformJump();
                    isAnticipating = false;
                }
            }
            else if(Keyboard.current[Key.Space].wasReleasedThisFrame){
                PerformJump();
                isAnticipating = false;
            }
        }
    }
    
    void PerformJump(){
        float holdRatio = anticipationHoldTime / maxAnticipationTime;
        float jumpForce = Mathf.Lerp(minJumpForce, maxJumpForce, holdRatio);
        
        Vector2 currentVel = rb.linearVelocity;
        currentVel.y = jumpForce;
        rb.linearVelocity = currentVel;
        jumpBufferTimer = 0;
        coyoteTimer = 0;
        canDive = true;
        
        anticipationHoldTime = 0;
    }
    
    void HandleDiveInput(){
        if(Keyboard.current[Key.S].wasPressedThisFrame){
            if(isDiving){
                CancelDive();
                return;
            }
            
            if(!isGrounded && !isJumpHeld && !isDiving && !isWallSliding && canDive && rb.linearVelocity.y < minDownwardVelocityForDive){
                Bounds spriteBounds = sr.bounds;
                Vector2 groundCheckPos = new Vector2(transform.position.x, spriteBounds.min.y);
                bool groundNearby = Physics2D.Raycast(groundCheckPos, Vector2.down, groundCheckDistanceForDive, groundLayer);
                
                if(!groundNearby){
                    StartDive();
                }
            }
        }
    }
    
    void HandleDashInput(){
        if(Mouse.current.rightButton.wasPressedThisFrame && canDash && !isDashing && dashCooldownTimer <= 0){
            TryDash();
        }
    }
    #endregion
    
    #region MOVEMENT LOGIC
    void HandleMovement(){
        float targetSpeed = horizontalInput * moveSpeed;
        currentVelocity = rb.linearVelocity;
        
        float accelerationRate = isGrounded ? acceleration : airAcceleration;
        float decelerationRate = isGrounded ? deceleration : airAcceleration;
        
        if(Mathf.Abs(horizontalInput) > 0.1f){
            float speedDiff = targetSpeed - currentVelocity.x;
            float movement = speedDiff * accelerationRate * Time.fixedDeltaTime;
            currentVelocity.x += movement;
            
            if(Mathf.Sign(horizontalInput) != Mathf.Sign(currentVelocity.x)){
                currentVelocity.x += speedDiff * turnSpeed * Time.fixedDeltaTime;
            }
        }else{
            currentVelocity.x = Mathf.MoveTowards(currentVelocity.x, 0, decelerationRate * Time.fixedDeltaTime);
        }
        
        if(!isGrounded){
            currentVelocity.x = Mathf.Lerp(currentVelocity.x, targetSpeed, airControl * Time.fixedDeltaTime);
        }
        
        rb.linearVelocity = currentVelocity;
    }
    #endregion
    
    #region JUMP LOGIC
    void HandleJump(){
        bool canJump = (isGrounded || coyoteTimer > 0) && !isWallSliding;
        
        if(jumpBufferTimer > 0 && canJump && !isDiving && !isAnticipating){
            isAnticipating = true;
            anticipationHoldTime = 0f;
            jumpBufferTimer = 0;
        }
    }
    
    void EndJump() => isJumpHeld = false;
    #endregion
    
    #region DIVE LOGIC
    void StartDive(){
        isDiving = true;
        canDive = false;
        rb.linearVelocity = Vector2.down * diveSpeed;
    }
    
    void HandleDive() => rb.linearVelocity = Vector2.down * diveSpeed;
    
    void CancelDive(){
        isDiving = false;
        Vector2 currentVelocity = rb.linearVelocity;
        currentVelocity.y = diveCancelJump;
        rb.linearVelocity = currentVelocity;
    }
    #endregion
    
    #region DASH LOGIC
    void TryDash(){
        if(currentStamina < dashStaminaCost) return;
        
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector3 worldMousePos = mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, mainCamera.nearClipPlane));
        
        dashDirection = (worldMousePos - transform.position).normalized;
        
        isDashing = true;
        canDash = false;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0;
        
        UseStamina(dashStaminaCost);
        
        if(!isGrounded){
            airDashCount++;
            if(airDashCount >= maxAirDashes) canDash = false;
        }
    }
    
    void HandleDash() => rb.linearVelocity = dashDirection * dashForce;
    
    void EndDash(){
        isDashing = false;
        rb.gravityScale = 1;
        
        if(isGrounded){
            canDash = true;
            airDashCount = 0;
        }
        else if(airDashCount < maxAirDashes){
            canDash = true;
        }
        
        rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.5f, rb.linearVelocity.y * 0.5f);
    }
    #endregion
    
    #region WALL LOGIC
    void HandleWallSlide(){
        if(isWallSliding){
            float slideVelocity = Mathf.Max(rb.linearVelocity.y, -wallSlideSpeed);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, slideVelocity);
            
            if(currentStamina > 0){
                UseStamina(wallSlideStaminaDrain * Time.fixedDeltaTime);
            }else{
                isWallSliding = false;
            }
        }
    }
    
    void WallJump(){
        Vector2 wallNormal = isWallLeft ? Vector2.right : Vector2.left;
        wallJumpDirection = new Vector2(wallNormal.x * wallJumpHorizontalForce, maxJumpForce);
        
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(wallJumpDirection, ForceMode2D.Impulse);
        
        isWallSliding = false;
        jumpBufferTimer = 0;
        canDive = true;
    }
    #endregion
    
    #region STAMINA LOGIC
    void HandleStamina(){
        isUsingStamina = isWallSliding || isDashing;
        
        if(isUsingStamina) staminaRegenTimer = staminaRegenDelay;
        else if(staminaRegenTimer <= 0){
            currentStamina += staminaRegenRate * Time.fixedDeltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
        }
    }
    
    void UseStamina(float amount){
        currentStamina -= amount;
        currentStamina = Mathf.Max(0, currentStamina);
        staminaRegenTimer = staminaRegenDelay;
    }
    #endregion
    
    #region PHYSICS HELPERS
    void HandleGravity(){
        if(isDiving) return;
        
        Vector2 velocity = rb.linearVelocity;
        
        if(velocity.y < 0) velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        else if(velocity.y > 0) velocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
        
        rb.linearVelocity = velocity;
    }
    
    void UpdateSpriteFlip(){
        if(horizontalInput > 0) sr.flipX = false;
        else if(horizontalInput < 0) sr.flipX = true;
    }
    #endregion
    
    #region PUBLIC METHODS
    public void SetMoveSpeed(float newSpeed) => moveSpeed = newSpeed;
    public void SetJumpForce(float newMinForce, float newMaxForce){
        minJumpForce = newMinForce;
        maxJumpForce = newMaxForce;
    }
    public bool IsGrounded => isGrounded;
    public bool IsWallSliding => isWallSliding;
    public bool IsDashing => isDashing;
    public bool IsDiving => isDiving;
    public bool IsAnticipating => isAnticipating;
    public float GetAnticipationRatio() => anticipationHoldTime / maxAnticipationTime;
    public bool CanDive => canDive;
    public Vector2 GetVelocity => rb.linearVelocity;
    public float GetStaminaPercent() => currentStamina / maxStamina;
    public bool CanDash => canDash;
    public int AirDashCount => airDashCount;
    #endregion
}
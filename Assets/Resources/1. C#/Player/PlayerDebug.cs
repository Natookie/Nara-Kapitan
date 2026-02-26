using UnityEngine;
using UnityEngine.InputSystem;
using Nova;

public class PlayerDebug : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIBlock2D debugPanel;
    [SerializeField] private TextBlock debugText;
    [SerializeField] private PlayerMovement playerMovement;
    
    [Header("Settings")]
    [SerializeField] private bool showDetailedInfo = true;
    [SerializeField] private bool showCollisionInfo = true;
    [SerializeField] private bool showAbilityInfo = true;
    [SerializeField] private bool showTimerInfo = true;
    [SerializeField] private bool showStaminaInfo = true;
    [SerializeField] private bool showVelocityInfo = true;
    
    private Rigidbody2D rb;
    
    void Start(){
        if(playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
        if(playerMovement != null) rb = playerMovement.GetComponent<Rigidbody2D>();
        if(debugPanel != null) debugPanel.gameObject.SetActive(false);
    }
    
    void Update(){
        if(Keyboard.current.zKey.wasPressedThisFrame && debugPanel != null) debugPanel.gameObject.SetActive(!debugPanel.gameObject.activeSelf);
        if(debugPanel != null && debugPanel.gameObject.activeSelf && debugText != null) UpdateDebugText();
    }
    
    void UpdateDebugText(){
        string debugInfo = "";
        
        debugInfo += $"<color=#FFAA00>Time Scale: {Time.timeScale:F2}</color>\n\n";
        if(showCollisionInfo){
            debugInfo += "<b><color=#00AAFF>COLLISIONS</color></b>\n";
            debugInfo += $"Grounded: {GetColoredBool(playerMovement.IsGrounded)}\n";
            debugInfo += $"Wall Sliding: {GetColoredBool(playerMovement.IsWallSliding)}\n";
            debugInfo += $"Wall Left: {GetColoredBool(GetPrivateField<bool>("isWallLeft"))}\n";
            debugInfo += $"Wall Right: {GetColoredBool(GetPrivateField<bool>("isWallRight"))}\n\n";
        }
        
        if(showAbilityInfo){
            debugInfo += "<b><color=#AA00FF>ABILITIES</color></b>\n";
            debugInfo += $"Dashing: {GetColoredBool(playerMovement.IsDashing)}\n";
            debugInfo += $"Diving: {GetColoredBool(playerMovement.IsDiving)}\n";

            debugInfo += $"Can Dash: {GetColoredBool(GetPrivateField<bool>("canDash"))}\n";
            debugInfo += $"Can Dive: {GetColoredBool(GetPrivateField<bool>("canDive"))}\n";

            debugInfo += $"Air Dash Count: {GetPrivateField<int>("airDashCount")} / {GetPrivateField<int>("maxAirDashes")}\n";
            debugInfo += $"Has Jumped This Air Time: {GetColoredBool(GetPrivateField<bool>("hasJumpedThisAirTime"))}\n";
            debugInfo += $"Jump Held: {GetColoredBool(GetPrivateField<bool>("isJumpHeld"))}\n\n";
        }
        
        if(showTimerInfo){
            debugInfo += "<b><color=#FF5500>TIMERS</color></b>\n";
            debugInfo += $"Coyote Timer: {GetPrivateField<float>("coyoteTimer"):F2}\n";
            debugInfo += $"Jump Buffer Timer: {GetPrivateField<float>("jumpBufferTimer"):F2}\n";
            debugInfo += $"Jump Hold Timer: {GetPrivateField<float>("jumpHoldTimer"):F2}\n";
            debugInfo += $"Dash Timer: {GetPrivateField<float>("dashTimer"):F2}\n";
            debugInfo += $"Dash Cooldown Timer: {GetPrivateField<float>("dashCooldownTimer"):F2}\n";
            debugInfo += $"Stamina Regen Timer: {GetPrivateField<float>("staminaRegenTimer"):F2}\n\n";
        }
        
        if(showStaminaInfo){
            float staminaPercent = playerMovement.GetStaminaPercent();
            string staminaColor = (staminaPercent > 0.5f) ? "#00FF00" : (staminaPercent > 0.2f) ? "#FFFF00" : "#FF0000";
            
            debugInfo += "<b><color=#00FFAA>STAMINA</color></b>\n";
            debugInfo += $"Stamina: <color={staminaColor}>{GetPrivateField<float>("currentStamina"):F0}/{GetPrivateField<float>("maxStamina")}</color>\n";
            debugInfo += $"Percent: <color={staminaColor}>{staminaPercent:P0}</color>\n";
            debugInfo += $"Using Stamina: {GetColoredBool(GetPrivateField<bool>("isUsingStamina"))}\n\n";
        }
        
        if(showVelocityInfo && rb != null){
            Vector2 velocity = playerMovement.GetVelocity;
            string xColor = (Mathf.Abs(velocity.x) > 0.1f) ? "#00FFAA" : "#AAAAAA";
            string yColor = (Mathf.Abs(velocity.y) > 0.1f) ? "#00FFAA" : "#AAAAAA";
            
            debugInfo += "<b><color=#FF00AA>VELOCITY</color></b>\n";
            debugInfo += $"Velocity: (<color={xColor}>{velocity.x:F2}</color>, <color={yColor}>{velocity.y:F2}</color>)\n";
            debugInfo += $"Speed: {velocity.magnitude:F2}\n";
            debugInfo += $"Horizontal Input: {GetPrivateField<float>("horizontalInput"):F2}\n\n";
        }
        
        if(showDetailedInfo){
            debugInfo += "<b><color=#888888>DETAILS</color></b>\n";
            debugInfo += $"Gravity Scale: {rb?.gravityScale:F2}\n";
            debugInfo += $"Linear Damping: {rb?.linearDamping:F2}\n";
        }
        
        debugText.Text = debugInfo;
    }
    
    string GetColoredBool(bool value) => (value) ? "<color=#00FF00>TRUE</color>" : "<color=#FF0000>FALSE</color>";
    
    T GetPrivateField<T>(string fieldName){
        if(playerMovement == null) return default;
        
        System.Type type = playerMovement.GetType();
        System.Reflection.FieldInfo field = type.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
        if(field != null) return (T)field.GetValue(playerMovement);
        return default;
    }
    
    public void ToggleDetailedInfo() => showDetailedInfo = !showDetailedInfo;
    public void ToggleCollisionInfo() => showCollisionInfo = !showCollisionInfo;
    public void ToggleAbilityInfo() => showAbilityInfo = !showAbilityInfo;
    public void ToggleTimerInfo() => showTimerInfo = !showTimerInfo;
    public void ToggleStaminaInfo() => showStaminaInfo = !showStaminaInfo;
    public void ToggleVelocityInfo() => showVelocityInfo = !showVelocityInfo;
}
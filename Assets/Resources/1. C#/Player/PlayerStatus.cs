using UnityEngine;
using NaughtyAttributes;

public class PlayerStatus : MonoBehaviour, IDamageable
{
    [Header("STATS")]
    [ReadOnly]public float maxHealth = 100f;
    [MinValue(0)][SerializeField] private float currentHealth;
    
    [Header("DROWNING")]
    [SerializeField] private float drowningDamage = 10f;
    [SerializeField] private float drowningInterval = 1f;

    [Header("REFERENCES")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private BuoyancyPhysics physics;
    
    private bool isDrowning = false;
    private bool isAlive = true;
    
    public bool IsAlive => isAlive;
    
    void Start(){
        if(playerMovement == null) playerMovement = GetComponent<PlayerMovement>();

        currentHealth = maxHealth;
    }
    
    void Update(){
        HandleDrowning();
    }
    
    void HandleDrowning(){
        if(!isAlive) return;
        
        if(physics.InWater){
            if(!isDrowning){
                isDrowning = true;
                InvokeRepeating(nameof(ApplyDrowningDamage), drowningInterval, drowningInterval);
            }
        }
        else{
            if(isDrowning){
                isDrowning = false;
                CancelInvoke(nameof(ApplyDrowningDamage));
            }
        }
    }
    
    void ApplyDrowningDamage(){
        if(isAlive) currentHealth -= drowningDamage;
    }
    
    public void TakeDamage(int damage){
        if(isAlive) currentHealth -= damage;
        if(currentHealth <= 0) Die();
    }
    
    public void Die(){
        if(!isAlive) return;
        
        isAlive = false;
        playerMovement.enabled = false;
        
        if(SceneChanger.Instance == null) Debug.LogError("SceneChanger.Instance is NULL! Make sure SceneChanger script is attached to a GameObject in the scene.");
        else SceneChanger.Instance.ChangeScene("MainMenuScene");
    }
}
using UnityEngine;
using System.Collections;

public class BulletController : MonoBehaviour
{
    [Header("SETTINGS")]
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private LayerMask collisionLayers;
    
    private Rigidbody2D rb;
    private int damage;
    private Coroutine lifetimeCoroutine;
    private PoolingManager poolingManager;
    
    void Awake(){
        rb = GetComponent<Rigidbody2D>();
        poolingManager = FindFirstObjectByType<PoolingManager>();
    }
    
    public void Initialize(Vector2 velocity, int bulletDamage){
        rb.linearVelocity = velocity;
        damage = bulletDamage;
        
        if(lifetimeCoroutine != null) StopCoroutine(lifetimeCoroutine);
        lifetimeCoroutine = StartCoroutine(LifetimeCountdown());
    }
    
    IEnumerator LifetimeCountdown(){
        yield return new WaitForSeconds(lifetime);
        ReturnToPool();
    }
    
    void OnTriggerEnter2D(Collider2D other){
        if(((1 << other.gameObject.layer) & collisionLayers) != 0)
            HandleCollision(other);
    }
    
    void OnCollisionEnter2D(Collision2D collision){
        if(((1 << collision.gameObject.layer) & collisionLayers) != 0)
            HandleCollision(collision.collider);
    }
    
    void HandleCollision(Collider2D other){
        IDamageable damageable = other.GetComponent<IDamageable>();
        if(damageable != null) damageable.TakeDamage(damage);

        ReturnToPool();
    }
    
    void ReturnToPool(){
        if(poolingManager != null) poolingManager.ReturnBullet(gameObject);
        else gameObject.SetActive(false);
        
        if(lifetimeCoroutine != null){
            StopCoroutine(lifetimeCoroutine);
            lifetimeCoroutine = null;
        }
    }
    
    void OnDisable(){
        rb.linearVelocity = Vector2.zero;
        damage = 0;
        
        if(lifetimeCoroutine != null){
            StopCoroutine(lifetimeCoroutine);
            lifetimeCoroutine = null;
        }
    }
}
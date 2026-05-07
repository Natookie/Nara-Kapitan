using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    private Vector2 targetPosition;
    private int damage;
    private float speed;
    private Vector2 direction;
    
    public void Initialize(Vector2 target, int projectileDamage, float projectileSpeed){
        targetPosition = target;
        damage = projectileDamage;
        speed = projectileSpeed;
        
        direction = (targetPosition - (Vector2)transform.position).normalized;
        
        Destroy(gameObject, 5f);
    }
    
    void Update(){
        transform.Translate(direction * speed * Time.deltaTime);
    }
    
    void OnTriggerEnter2D(Collider2D other){
        if(other.CompareTag("Player")){
            IDamageable damageable = other.GetComponent<IDamageable>();
            if(damageable != null) damageable.TakeDamage(damage);
            Destroy(gameObject);
        }
        else if(!other.CompareTag("Enemy")) Destroy(gameObject);
    }
}
using UnityEngine;

public class Enemylogic : MonoBehaviour, IDamageable
{
    private float currentHealth = 100f;
    
    public void TakeDamage(float damage){
        currentHealth -= damage;
        if(currentHealth <= 0) Die();
    }
    
    private void Die(){
        Destroy(gameObject);
    }
}
using UnityEngine;

[CreateAssetMenu(fileName = "ExplosiveBird", menuName = "Enemies/ExplosiveBird")]
public class ExplosiveBirdData : EnemyData
{
    [Header("WANDERING")]
    public float wanderBeforeFleeMin = 2f;
    public float wanderBeforeFleeMax = 5f;
    
    [Header("KAMIKAZE")]
    public float kamikazeSpeed = 18f;
    public float kamikazeDetectionWindow = 0.5f;
    public float explosionRadius = 2.5f;
    public int explosionDamage = 50;
    public GameObject explosionEffect;
    
    void OnEnable(){
        avoidAimingRadius = 0f;
        stopRadius = 0f;
    }
}
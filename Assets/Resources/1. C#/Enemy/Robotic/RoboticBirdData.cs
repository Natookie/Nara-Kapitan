using UnityEngine;
using NaughtyAttributes;

[CreateAssetMenu(fileName = "RoboticBird", menuName = "Enemies/RoboticBird")]
public class RoboticBirdData : EnemyData
{
    [Header("WANDERING")]
    public Vector2 approachDelay = new Vector2(3f, 6f);
    
    [Header("APPROACHING")]
    [MinValue(0f)] public float approachSpeed = 8f;
    [MinValue(0f)] public float stealRadius = 1.5f;
    [MinValue(0f)] public int stealFoodAmount = 5;
    
    [Header("FLEEING")]
    [MinValue(0f)] public float fleeSpeed = 12f;
    
    [Header("ATTACKING")]
    [MinValue(0f)] public float attackDelay = 0.5f;
    public GameObject projectilePrefab;
    [MinValue(0f)] public float projectileSpeed = 15f;
    [MinValue(0f)] public int projectileDamage = 10;
}
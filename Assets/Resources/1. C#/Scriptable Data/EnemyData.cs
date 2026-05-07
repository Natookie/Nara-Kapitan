using UnityEngine;
using NaughtyAttributes;

[CreateAssetMenu(fileName = "New Enemy", menuName = "Enemies/EnemyData")]
public class EnemyData : ScriptableObject
{
    [Header("HEALTH")]
    [MinValue(0f)] public int maxHealth = 100;

    [Header("MOVEMENT")]
    [MinValue(0f)] public float movementSpeed = 5f;
    [MinValue(0f)] public float stopRadius = 3f;
    [MinValue(0f)] public float stopRadiusTolerance = 5f;
    
    [Header("AVOIDANCE")]
    [MinValue(2f)] public float avoidAimingRadius = 2f;
    [MinValue(0f)] public float avoidSpeed = 8f;
    public LayerMask obstacleMask;
    
    [Header("DEATH")]
    [MinValue(0f)] public int scoreValue = 10;

    [Header("VISUALS")]
    [Required] public Sprite enemySprite;
    public Color spriteColor = Color.white;
}
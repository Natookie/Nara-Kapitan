using UnityEngine;
using NaughtyAttributes;

[CreateAssetMenu(fileName = "WoodenBird", menuName = "Enemies/WoodenBird")]
public class WoodenBirdData : EnemyData
{
    [Header("FLEEING")]
    [MinValue(0f)] public float fleeSpeed = 8f;
    [MinValue(0f)] public float fleeDuration = 3f;
}
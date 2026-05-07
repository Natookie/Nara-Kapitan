using UnityEngine;
using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }
    
    [Header("ENEMY PREFABS")]
    [SerializeField] private GameObject woodenBirdPrefab;
    [SerializeField] private GameObject roboticBirdPrefab;
    [SerializeField] private GameObject explosiveBirdPrefab;
    
    [Header("SPAWN AREA")]
    [SerializeField] private Vector2 wanderAreaCenter = Vector2.zero;
    [SerializeField] private Vector2 wanderAreaSize = new Vector2(20f, 15f);
    [SerializeField] private bool showGizmos = true;
    
    [Header("CONFINER")]
    [SerializeField] private BoxCollider2D fleeConfiner;
    [SerializeField] private Color confinerColor = new Color(1f, 0.5f, 0f, 0.3f);
    
    [Header("SPAWN SETTINGS")]
    [SerializeField] private Transform enemyParent;
    [SerializeField] private int maxEnemiesAtOnce = 10;
    [SerializeField] private float minSpawnDelay = 5f;
    [SerializeField] private float maxSpawnDelay = 10f;
    
    private List<GameObject> activeEnemies = new List<GameObject>();
    private bool isRespawning = true;
    
    void Awake(){
        if(Instance != null && Instance != this){
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    void Start(){
        if(fleeConfiner == null) Debug.LogWarning("No flee confiner assigned to EnemyManager");
        //StartCoroutine(SpawnRoutine());
    }

    void Update(){
        if(Keyboard.current.digit1Key.wasPressedThisFrame) SpawnWoodenBird();
        if(Keyboard.current.digit2Key.wasPressedThisFrame) SpawnRoboticBird();
        if(Keyboard.current.digit3Key.wasPressedThisFrame) SpawnExplosiveBird();
    }
    
    IEnumerator SpawnRoutine(){
        while(isRespawning){
            if(activeEnemies.Count < maxEnemiesAtOnce){
                float delay = Random.Range(minSpawnDelay, maxSpawnDelay);
                yield return new WaitForSeconds(delay);
                SpawnRandomEnemy();
            }
            yield return null;
        }
    }
    
    void SpawnRandomEnemy(){
        int randomType = Random.Range(0, 3);
        
        switch(randomType){
            case 0: SpawnWoodenBird(); break;
            case 1: SpawnRoboticBird(); break;
            case 2: SpawnExplosiveBird(); break;
        }
    }
    
    [Button("Spawn Wooden Bird", EButtonEnableMode.Playmode)]
    void SpawnWoodenBird(){
        if(woodenBirdPrefab == null){
            Debug.LogError("Wooden Bird Prefab not assigned!");
            return;
        }
        SpawnEnemy(woodenBirdPrefab);
    }
    
    [Button("Spawn Robotic Bird", EButtonEnableMode.Playmode)]
    void SpawnRoboticBird(){
        if(roboticBirdPrefab == null){
            Debug.LogError("Robotic Bird Prefab not assigned!");
            return;
        }
        SpawnEnemy(roboticBirdPrefab);
    }
    
    [Button("Spawn Explosive Bird", EButtonEnableMode.Playmode)]
    void SpawnExplosiveBird(){
        if(explosiveBirdPrefab == null){
            Debug.LogError("Explosive Bird Prefab not assigned!");
            return;
        }
        SpawnEnemy(explosiveBirdPrefab);
    }
    
    void SpawnEnemy(GameObject prefab){
        Vector3 spawnPosition = GetRandomPositionInWanderArea();
        
        GameObject enemy = Instantiate(prefab, spawnPosition, Quaternion.identity);
        if(enemyParent != null) enemy.transform.SetParent(enemyParent);
        activeEnemies.Add(enemy);
        
        EnemyLogic enemyLogic = enemy.GetComponent<EnemyLogic>();
        if(enemyLogic != null){
            enemyLogic.OnEnemyDestroyed += HandleEnemyDestroyed;
            enemyLogic.SetWanderArea(wanderAreaCenter, wanderAreaSize);
            
            if(fleeConfiner != null) enemyLogic.SetFleeConfiner(fleeConfiner);
        }
    }
    
    Vector3 GetRandomPositionInWanderArea(){
        float randomX = Random.Range(wanderAreaCenter.x - wanderAreaSize.x / 2, wanderAreaCenter.x + wanderAreaSize.x / 2);
        float randomY = Random.Range(wanderAreaCenter.y - wanderAreaSize.y / 2, wanderAreaCenter.y + wanderAreaSize.y / 2);

        return new Vector3(randomX, randomY, 0);
    }
    
    void HandleEnemyDestroyed(GameObject enemy){
        if(activeEnemies.Contains(enemy)) activeEnemies.Remove(enemy);
    }
    
    public void RemoveEnemy(GameObject enemy){
        if(activeEnemies.Contains(enemy)) activeEnemies.Remove(enemy);
    }
    
    public void DespawnAllEnemies(){
        foreach(GameObject enemy in activeEnemies){
            if(enemy != null) Destroy(enemy);
        }
        activeEnemies.Clear();
    }
    
    [Button("Despawn All Enemies", EButtonEnableMode.Editor)]
    void DespawnAllEnemiesButton(){
        DespawnAllEnemies();
    }
    
    public Vector2 GetWanderAreaCenter() => wanderAreaCenter;
    public Vector2 GetWanderAreaSize() => wanderAreaSize;
    
    void OnDrawGizmos(){
        if(!showGizmos) return;
        
        Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
        Vector3 center = new Vector3(wanderAreaCenter.x, wanderAreaCenter.y, 0);
        Vector3 size = new Vector3(wanderAreaSize.x, wanderAreaSize.y, 0.1f);
        Gizmos.DrawCube(center, size);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(center, size);
        
        if(fleeConfiner != null){
            Gizmos.color = confinerColor;
            Bounds bounds = fleeConfiner.bounds;
            Gizmos.DrawCube(bounds.center, bounds.size);
            Gizmos.color = new Color(confinerColor.r, confinerColor.g, confinerColor.b, 1f);
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }
    
    void OnDestroy(){
        if(Instance == this) Instance = null;
    }
}
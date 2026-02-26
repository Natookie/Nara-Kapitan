using UnityEngine;
using System.Collections.Generic;

public class PoolingManager : MonoBehaviour
{
    [Header("POOLING SETTINGS")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private int initialPoolSize = 100;
    [SerializeField] private Transform poolingParent;
    
    private Queue<GameObject> bulletPool = new Queue<GameObject>();
    private List<GameObject> activeBullets = new List<GameObject>();
    
    private void Awake(){
        if(poolingParent == null){
            GameObject parentObj = new GameObject("PooledBullets");
            poolingParent = parentObj.transform;
        }
        
        InitializePool();
    }
    
    private void InitializePool(){
        for(int i = 0; i < initialPoolSize; i++){
            CreateNewBullet();
        }
    }
    
    private void CreateNewBullet(){
        GameObject bullet = Instantiate(bulletPrefab, poolingParent);
        bullet.SetActive(false);
        bulletPool.Enqueue(bullet);
    }
    
    public GameObject GetBullet(){
        if(bulletPool.Count == 0){
            CreateNewBullet();
        }
        
        GameObject bullet = bulletPool.Dequeue();
        bullet.SetActive(true);
        activeBullets.Add(bullet);
        
        return bullet;
    }
    
    public void ReturnBullet(GameObject bullet){
        bullet.SetActive(false);
        bullet.transform.SetParent(poolingParent);
        bulletPool.Enqueue(bullet);
        activeBullets.Remove(bullet);
    }
    
    public void ReturnAllBullets(){
        for(int i = activeBullets.Count - 1; i >= 0; i--){
            if(activeBullets[i] != null){
                ReturnBullet(activeBullets[i]);
            }
        }
        activeBullets.Clear();
    }
    
    public int GetPoolCount() => bulletPool.Count;
    public int GetActiveCount() => activeBullets.Count;
}
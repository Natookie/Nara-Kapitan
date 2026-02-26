using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooting : MonoBehaviour
{
    [Header("TWEAKS")]
    [SerializeField] private GunData currentGun;
    [SerializeField] private Transform firePoint;
    
    [Header("REFERENCES")]
    [SerializeField] private PoolingManager poolingManager;
    
    [Header("STATES")]
    private int currentAmmo;
    private int totalAmmo;
    private float nextFireTime;
    private bool isReloading;
    
    void Start(){
        if(currentGun != null) Reload();
    }
    
    void Update(){
        if(currentGun == null || firePoint == null) return;
        
        HandleShooting();
    }
    
    void HandleShooting(){
        if(isReloading) return;
        
        bool reloadRequested = Keyboard.current[Key.R].wasPressedThisFrame;
        bool isMagazineNotFull = currentAmmo < currentGun.magazineSize;
        
        if(reloadRequested && isMagazineNotFull) StartReload();
        
        bool weaponReady = Time.time >= nextFireTime;
        bool holdingFireButton = Mouse.current.leftButton.isPressed;
        bool pressedFireThisFrame = Mouse.current.leftButton.wasPressedThisFrame;
        bool gunIsAutomatic = currentGun.automatic;
        
        if(!weaponReady) return;
        if(gunIsAutomatic && holdingFireButton) TryShoot();
        else if(!gunIsAutomatic && pressedFireThisFrame) TryShoot();
    }
    
    void TryShoot(){
        if(currentAmmo <= 0){
            StartReload();
            return;
        }
        
        Shoot();
    }
    
    void Shoot(){
        currentAmmo--;
        nextFireTime = Time.time + currentGun.fireRate;
        
        FireBullet();
        
        if(currentAmmo <= 0) StartReload();
    }
    
    void FireBullet(){
        for(int i = 0; i < currentGun.pelletCount; i++){
            GameObject bullet = poolingManager.GetBullet();
            if(bullet == null) return;
            
            Vector3 direction = GetFireDirection();
            
            if(currentGun.spread > 0){
                Vector2 spreadVector = Random.insideUnitCircle * currentGun.spread;
                direction += new Vector3(spreadVector.x, spreadVector.y, 0);
                direction.Normalize();
            }
            
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            
            bullet.transform.position = firePoint.position;
            bullet.transform.rotation = rotation;
            
            BulletController bulletController = bullet.GetComponent<BulletController>();
            if(bulletController != null){
                bulletController.Initialize(direction * currentGun.bulletSpeed, currentGun.damage);
            }
        }
        
        if(currentGun.fireSound != null){
            // AudioSource.PlayClipAtPoint(currentGun.fireSound, firePoint.position);
        }
        
        if(currentGun.muzzleFlashPrefab != null){
            GameObject muzzleFlash = Instantiate(currentGun.muzzleFlashPrefab, firePoint.position, firePoint.rotation, firePoint);
            Destroy(muzzleFlash, 0.5f);
        }
    }
    
    Vector3 GetFireDirection() => (firePoint.position - transform.position).normalized;
    private void StartReload(){
        if(isReloading || currentAmmo >= currentGun.magazineSize || totalAmmo <= 0) return;
        
        isReloading = true;
        Invoke("FinishReload", currentGun.reloadTime);
        
        if(currentGun.reloadSound != null){
            // AudioSource.PlayClipAtPoint(currentGun.reloadSound, transform.position);
        }
    }
    
    void FinishReload(){
        int ammoNeeded = currentGun.magazineSize - currentAmmo;
        int ammoToAdd = Mathf.Min(ammoNeeded, totalAmmo);
        
        currentAmmo += ammoToAdd;
        totalAmmo -= ammoToAdd;
        
        isReloading = false;
    }
    
    void Reload(){
        currentAmmo = currentGun.magazineSize;
        totalAmmo = currentGun.magazineSize * 5;
    }
    public void AddAmmo(int amount) => totalAmmo = Mathf.Max(0, totalAmmo + amount);
    public void SetGun(GunData newGun){
        currentGun = newGun;
        Reload();
    }
    
    public int GetCurrentAmmo() => currentAmmo;
    public int GetTotalAmmo() => totalAmmo;
    public float GetReloadProgress() => isReloading ? (Time.time - (nextFireTime - currentGun.reloadTime)) / currentGun.reloadTime : 0f;
}
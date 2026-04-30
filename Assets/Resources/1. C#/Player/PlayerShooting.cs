using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooting : MonoBehaviour
{
    [Header("GUN SETTINGS")]
    [SerializeField] private GunData currentGun;
    [SerializeField] private Transform firePoint;
    [SerializeField] private Transform leftArm;
    
    [Header("AIM MODE")]
    [SerializeField] private float aimMovementRestriction = 0.3f;
    
    [Header("LINE RENDERER")]
    [SerializeField] private LineRenderer aimLine;
    
    [Header("REFERENCES")]
    [SerializeField] private PoolingManager poolingManager;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Camera mainCamera;
    
    [Header("STATES")]
    public bool isAiming;
    public bool isReloading;

    private int currentAmmo;
    private int reserveAmmo;
    private Vector3 aimDirection;
    private Vector3 mouseWorldPos;
    
    private float originalMoveSpeed;
    
    void Start(){
        if(currentGun != null) Reload();
        if(mainCamera == null) mainCamera = Camera.main;
    }
    
    void Update(){
        if(currentGun == null || firePoint == null) return;
        
        UpdateAimDirection();
        HandleAimMode();
        HandleShooting();
        HandleArmRotation();
        UpdateAimLine();
        DetectMouseSide();
    }
    
    void UpdateAimDirection(){
        Vector3 mousePos = Mouse.current.position.ReadValue();
        mousePos.z = 0;
        mouseWorldPos = mainCamera.ScreenToWorldPoint(mousePos);
        aimDirection = (mouseWorldPos - firePoint.position).normalized;
    }
    
    void HandleAimMode(){
        if(Mouse.current.leftButton.isPressed && !isAiming && !isReloading)
            EnterAimMode();
        
        if(Mouse.current.rightButton.wasReleasedThisFrame && isAiming)
            ExitAimMode();
    }
    
    void EnterAimMode(){
        if(isAiming) return;
        
        isAiming = true;
        
        if(playerMovement != null){
            originalMoveSpeed = playerMovement.GetMoveSpeed();
            playerMovement.SetMoveSpeed(originalMoveSpeed * aimMovementRestriction);
        }
        
        if(aimLine != null) aimLine.enabled = true;
    }
    
    void ExitAimMode(){
        if(!isAiming) return;
        
        isAiming = false;
        
        if(playerMovement != null){
            playerMovement.SetMoveSpeed(originalMoveSpeed);
        }
        
        if(aimLine != null) aimLine.enabled = false;
        aimDirection = Vector3.zero;
    }
    
    void UpdateAimLine(){
        if(!isAiming || aimLine == null) return;
        
        aimLine.SetPosition(0, firePoint.position);
        aimLine.SetPosition(1, firePoint.position + aimDirection * 20f);
    }
    
    void HandleShooting(){
        if(isReloading) return;
        
        if(Keyboard.current[Key.R].wasPressedThisFrame){
            if(currentAmmo < currentGun.magazineSize && reserveAmmo > 0) 
                StartReload();
            return;
        }
        
        if(isAiming && Mouse.current.leftButton.wasReleasedThisFrame){
            TryShoot();
            ExitAimMode();
        }
    }
    
    void TryShoot(){
        if(currentAmmo <= 0){
            if(reserveAmmo > 0) StartReload();
            return;
        }
        
        Shoot();
    }
    
    void Shoot(){
        currentAmmo--;
        
        FireBullet();
        
        if(currentAmmo <= 0 && reserveAmmo > 0){
            StartReload();
        }
    }
    
    void FireBullet(){
        Vector3 shootDirection = aimDirection;
        
        if(shootDirection == Vector3.zero){
            shootDirection = Vector3.right;
        }
        
        for(int i = 0; i < currentGun.pelletCount; i++){
            GameObject bullet = poolingManager.GetBullet();
            if(bullet == null) return;
            
            Vector3 direction = shootDirection;
            
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
        
        if(currentGun.muzzleFlashPrefab != null){
            GameObject muzzleFlash = Instantiate(currentGun.muzzleFlashPrefab, firePoint.position, firePoint.rotation, firePoint);
            Destroy(muzzleFlash, 0.5f);
        }
    }
    
    void HandleArmRotation(){
        if(leftArm == null) return;
        
        Vector3 armToMouse = (mouseWorldPos - leftArm.position).normalized;
        float angle = Mathf.Atan2(armToMouse.y, armToMouse.x) * Mathf.Rad2Deg;
        
        leftArm.rotation = Quaternion.Euler(0, 0, angle);
    }
    
    void DetectMouseSide(){
        if(!isAiming) return;
        
        float mouseSide = mouseWorldPos.x - transform.position.x;
        
        if(mouseSide > 0){
            // Debug.Log("Flip sprite right");
        }
        else if(mouseSide < 0){
            // Debug.Log("Flip sprite left");
        }
    }
    
    void StartReload(){
        if(isReloading) return;
        if(currentAmmo >= currentGun.magazineSize) return;
        if(reserveAmmo <= 0) return;
        
        if(isAiming) ExitAimMode();
        
        isReloading = true;
        Invoke("FinishReload", currentGun.reloadTime);
    }
    
    void FinishReload(){
        int ammoNeeded = currentGun.magazineSize - currentAmmo;
        int ammoToTake = Mathf.Min(ammoNeeded, reserveAmmo);
        
        currentAmmo += ammoToTake;
        reserveAmmo -= ammoToTake;
        
        isReloading = false;
    }
    
    void Reload(){
        currentAmmo = currentGun.magazineSize;
        reserveAmmo = currentGun.maxReserveAmmo;
    }
    
    public void AddAmmo(int amount){
        reserveAmmo = Mathf.Min(reserveAmmo + amount, currentGun.maxReserveAmmo);
    }
    
    public void SetGun(GunData newGun){
        currentGun = newGun;
        Reload();
    }
    
    public GunData GetCurrentGun() => currentGun;
    public int GetCurrentAmmo() => currentAmmo;
    public int GetReserveAmmo() => reserveAmmo;
    public int GetTotalAmmo() => currentAmmo + reserveAmmo;
    public bool IsAiming => isAiming;
}
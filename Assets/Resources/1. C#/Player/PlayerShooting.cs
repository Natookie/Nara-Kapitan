using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooting : MonoBehaviour
{
    public static PlayerShooting Instance {get; private set;}

    [Header("GUN SETTINGS")]
    [SerializeField] private GunData currentGun;
    [SerializeField] private Transform firePoint;
    [SerializeField] private Transform leftArm;
    [SerializeField] private Transform character;
    
    [Header("AIM MODE")]
    [SerializeField] private float aimMovementRestriction = 0.3f;
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
    
    private int lastFacingDirection = 1;
    private float originalMoveSpeed;
    
    void Awake(){
        if(Instance != null && Instance != this){
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start(){
        if(currentGun != null) Reload();
        if(mainCamera == null) mainCamera = Camera.main;
        if(aimLine != null) aimLine.enabled = false;
    }
    
    void Update(){
        if(currentGun == null || firePoint == null) return;
        
        UpdateAimDirection();
        HandleAimMode();
        HandleShooting();
        HandleArmRotation();
        UpdateAimLine();
        UpdateSpriteFlip();
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
        
        if(playerMovement != null) playerMovement.SetMoveSpeed(originalMoveSpeed);
        if(aimLine != null) aimLine.enabled = false;
        aimDirection = Vector3.zero;
    }
    
    void UpdateAimLine(){
        if(!isAiming || aimLine == null) return;
        
        aimLine.SetPosition(0, firePoint.position);
        aimLine.SetPosition(1, firePoint.position + aimDirection * 20f);
        Debug.DrawRay(firePoint.position, aimDirection * 5f, Color.red);
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
        if(currentAmmo <= 0) return;
        Shoot();
    }
    
    void Shoot(){
        currentAmmo--;
        FireBullet();
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
            if(bulletController != null)
                bulletController.Initialize(direction * currentGun.bulletSpeed, currentGun.damage);
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
    
    void UpdateSpriteFlip(){
        if(character == null) return;
        
        Vector3 rotation = character.localEulerAngles;
        bool shouldFlip = false;
        
        if(isAiming){
            float mouseSide = mouseWorldPos.x - transform.position.x;
            shouldFlip = mouseSide < 0;
        }
        else{
            float input = playerMovement.GetHorizontalInput();
            
            if(Mathf.Abs(input) > 0.1f){
                shouldFlip = input < -0.1f;
                lastFacingDirection = shouldFlip ? -1 : 1;
            }else shouldFlip = lastFacingDirection == -1;
        }
        
        if(shouldFlip) rotation.y = 180f;
        else rotation.y = 0f;
        character.localEulerAngles = rotation;
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
    public Transform GetFirePoint() => firePoint;
    public int GetCurrentAmmo() => currentAmmo;
    public int GetReserveAmmo() => reserveAmmo;
    public int GetTotalAmmo() => currentAmmo + reserveAmmo;
    public bool IsAiming => isAiming;
}
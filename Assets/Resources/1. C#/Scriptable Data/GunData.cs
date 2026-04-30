using UnityEngine;

[CreateAssetMenu(fileName = "NewGun", menuName = "Guns/Gun Data")]
public class GunData : ScriptableObject
{
    [Header("BASIC")]
    public string gunName = "Gun";
    public float damage = 10f;
    public float fireRate = 0.2f;
    public int magazineSize = 30;
    public int maxReserveAmmo = 90;
    public float reloadTime = 1.5f;
    
    [Header("ADVANCED")]
    public float bulletSpeed = 20f;
    public float spread = 0.1f;
    public int pelletCount = 1;
    public bool automatic = true;
    
    [Header("VISUALS")]
    public GameObject bulletPrefab;
    public AudioClip fireSound;
    public AudioClip reloadSound;
    public GameObject muzzleFlashPrefab;
}
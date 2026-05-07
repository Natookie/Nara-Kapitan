using Nova;
using UnityEngine;

public class BulletUI : MonoBehaviour
{
    [Header("UI REFERENCES")]
    [SerializeField] private TextBlock bulletCountText;

    [Header("UI REFERENCES")]
    [SerializeField] private PlayerShooting ps;

    void Start(){
        UpdateBulletCount();
    }

    void Update(){
        UpdateBulletCount();
    }

    void UpdateBulletCount(){
        if(ps != null && bulletCountText != null){
            bulletCountText.Text = 
            $"{ps.GetCurrentAmmo()} / {ps.GetCurrentGun().magazineSize} ({ps.GetReserveAmmo()})";
        }
    }
}
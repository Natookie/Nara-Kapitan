using UnityEngine;
using UnityEngine.InputSystem;
using NaughtyAttributes;

[RequireComponent(typeof(LineRenderer))]
public class CamFoll : MonoBehaviour
{
    [Header("ORBIT")]
    [SerializeField] private float orbitRadius = 2f;
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private float minDistanceFromPlayer = 0.5f;
    [SerializeField] private float maxDistanceFromPlayer = 3f;
    
    [Header("LINE RENDERER")]
    public bool showLine = false;
    [EnableIf("showLine")][SerializeField] private float lineLength = 0f;
    [EnableIf("showLine")][SerializeField] private LineRenderer lineRenderer;

    [Header("INPUT")]
    [SerializeField] private float cursorInfluenceRadius = 100f;

    [Header("REFERENCES")]
    [Required][SerializeField] private Transform playerTransform;
    [Required][SerializeField] private Camera thisCam;
    
    private Vector3 offset;

    void LateUpdate(){
        if(playerTransform == null || thisCam == null) return;
        
        CalculateOffsetFromCursor();
        UpdateCameraPosition();
        if(showLine) UpdateLineRenderer();
    }

    void CalculateOffsetFromCursor(){
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Vector3 mouseScreenPosition = new Vector3(mousePosition.x, mousePosition.y, 0);
        Vector3 playerScreenPosition = thisCam.WorldToScreenPoint(playerTransform.position);
        
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Vector2 cursorDirection = (Vector2)mouseScreenPosition - screenCenter;
        
        float cursorDistance = cursorDirection.magnitude;
        float normalizedDistance = Mathf.Clamp01(cursorDistance / cursorInfluenceRadius);
        
        Vector2 normalizedDirection = cursorDirection.normalized;
        
        float currentRadius = Mathf.Lerp(minDistanceFromPlayer, maxDistanceFromPlayer, normalizedDistance);
        
        offset = new Vector3(normalizedDirection.x, normalizedDirection.y, 0) * orbitRadius;
    }

    void UpdateCameraPosition(){
        Vector3 targetPosition = playerTransform.position + offset;
        targetPosition.z = 0f;
        
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
    }

    void UpdateLineRenderer(){
        if(lineRenderer != null && playerTransform != null){
            Vector3 direction = (transform.position - playerTransform.position).normalized;
            Vector3 endPoint = playerTransform.position + direction * lineLength;
            
            lineRenderer.SetPosition(0, playerTransform.position);
            lineRenderer.SetPosition(1, endPoint);
        }
    }
}
using UnityEngine;
using System.Collections.Generic;
using NaughtyAttributes;

public class BoatLogic : MonoBehaviour
{
    [Header("ATTACHMENT SETTINGS")]
    [Tag, SerializeField] private string[] attachableTags;
    
    [Header("PHYSICS SETTINGS")]
    [SerializeField] private float attachedLinearDamping = 2f;
    [SerializeField] private float attachedAngularDamping = 2f;
    [SerializeField] private bool preserveMovementOnBoat = true;
    
    [Header("ROTATION MODE")]
    [Tooltip("How attached objects should rotate")]
    [SerializeField] private AttachmentRotationMode rotationMode = AttachmentRotationMode.ClipToCollision;
    
    [Header("DEBUG")]
    [SerializeField] private bool showDebugInfo = true;
    [ReadOnly, SerializeField] private int currentAttachedCount;
    
    private HashSet<GameObject> attachedObjects = new HashSet<GameObject>();
    private Dictionary<GameObject, Quaternion> localRotations = new Dictionary<GameObject, Quaternion>();
    private Dictionary<GameObject, float> originalLinearDamping = new Dictionary<GameObject, float>();
    private Dictionary<GameObject, float> originalAngularDamping = new Dictionary<GameObject, float>();
    private Dictionary<GameObject, float> originalGravityScale = new Dictionary<GameObject, float>();
    private CompositeCollider2D boatCollider;
    
    public enum AttachmentRotationMode
    {
        /*Object rotates exactly with the boat*/ MatchBoatRotation,
        /*Object aligns to the collision face normal*/ ClipToCollision,
        /*Object keeps its own rotation*/ KeepOwnRotation,
        /*Object rotates to stay upright relative to world*/ StayUpright
    }
    
    void Start(){
        boatCollider = GetComponent<CompositeCollider2D>();
        if(boatCollider == null) boatCollider = GetComponent<Collider2D>() as CompositeCollider2D;
    }
    
    void OnCollisionEnter2D(Collision2D collision){
        if(!IsAttachableTag(collision.gameObject.tag)) return;
        if(attachedObjects.Contains(collision.gameObject)) return;
        
        AttachObject(collision.gameObject);
    }
    
    void OnCollisionExit2D(Collision2D collision){
        if(attachedObjects.Contains(collision.gameObject)) DetachObject(collision.gameObject);
    }
    
    void AttachObject(GameObject obj){
        attachedObjects.Add(obj);
        localRotations[obj] = Quaternion.Inverse(transform.rotation) * obj.transform.rotation;
        
        Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
        if(rb != null){
            if(preserveMovementOnBoat){
                originalLinearDamping[obj] = rb.linearDamping;
                originalAngularDamping[obj] = rb.angularDamping;
                originalGravityScale[obj] = rb.gravityScale;
                
                rb.linearDamping = attachedLinearDamping;
                rb.angularDamping = attachedAngularDamping;
                rb.gravityScale = 0;
            }
        }
        
        currentAttachedCount = attachedObjects.Count;
        
        if(showDebugInfo) Debug.Log($"[BoatLogic] Attached: {obj.name}. Total: {currentAttachedCount}");
    }
    
    void DetachObject(GameObject obj){
        attachedObjects.Remove(obj);
        localRotations.Remove(obj);
        
        Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
        if(rb != null && preserveMovementOnBoat){
            if(originalLinearDamping.ContainsKey(obj)){
                rb.linearDamping = originalLinearDamping[obj];
                originalLinearDamping.Remove(obj);
            }
            if(originalAngularDamping.ContainsKey(obj)){
                rb.angularDamping = originalAngularDamping[obj];
                originalAngularDamping.Remove(obj);
            }
            if(originalGravityScale.ContainsKey(obj)){
                rb.gravityScale = originalGravityScale[obj];
                originalGravityScale.Remove(obj);
            }
        }
        
        currentAttachedCount = attachedObjects.Count;
        
        if(showDebugInfo) Debug.Log($"[BoatLogic] Detached: {obj.name}. Total: {currentAttachedCount}");
    }
    
    void LateUpdate(){
        if(attachedObjects.Count == 0) return;
        
        List<GameObject> toRemove = null;
        
        foreach(GameObject obj in attachedObjects){
            if(obj == null){
                if(toRemove == null) toRemove = new List<GameObject>();
                toRemove.Add(obj);
                continue;
            }
            
            UpdateAttachedObjectRotation(obj);
        }
        
        if(toRemove != null) foreach(GameObject obj in toRemove) DetachObject(obj);
    }
    
    void UpdateAttachedObjectRotation(GameObject obj){
        switch(rotationMode){
            case AttachmentRotationMode.MatchBoatRotation:
                obj.transform.rotation = transform.rotation;
                break;
                
            case AttachmentRotationMode.ClipToCollision:
                AlignToCollisionNormal(obj);
                break;
                
            case AttachmentRotationMode.KeepOwnRotation:
                if(localRotations.ContainsKey(obj)) 
                    obj.transform.rotation = transform.rotation * localRotations[obj];
                break;
                
            case AttachmentRotationMode.StayUpright:
                obj.transform.rotation = Quaternion.identity;
                break;
        }
        
        if(showDebugInfo && Time.frameCount % 60 == 0) 
            Debug.DrawLine(transform.position, obj.transform.position, Color.green);
    }
    
    void AlignToCollisionNormal(GameObject obj){
        RaycastHit2D hit = Physics2D.Raycast(obj.transform.position, -obj.transform.up, 2f);
        
        if(hit.collider != null && hit.collider.gameObject == gameObject){
            float angle = Mathf.Atan2(hit.normal.y, hit.normal.x) * Mathf.Rad2Deg;
            obj.transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
        }
        else obj.transform.rotation = transform.rotation;
    }
    
    bool IsAttachableTag(string tag){
        foreach(string attachableTag in attachableTags) 
            if(attachableTag == tag) return true;
        return false;
    }
    
    public void ForceDetachAll(){
        List<GameObject> objects = new List<GameObject>(attachedObjects);
        foreach(GameObject obj in objects) 
            if(obj != null) DetachObject(obj);
    }
    
    public bool IsObjectAttached(GameObject obj) => attachedObjects.Contains(obj);
    public int GetAttachedCount() => attachedObjects.Count;
    
    void OnDrawGizmosSelected(){
        Gizmos.color = Color.yellow;
        Collider2D col = GetComponent<Collider2D>();
        if(col != null){
            Gizmos.DrawWireCube(transform.position, col.bounds.size);
        }
        
        if(attachedObjects != null && showDebugInfo){
            Gizmos.color = Color.green;
            foreach(GameObject obj in attachedObjects){
                if(obj != null){
                    Gizmos.DrawLine(transform.position, obj.transform.position);
                    Gizmos.DrawWireSphere(obj.transform.position, 0.2f);
                }
            }
        }
    }
}
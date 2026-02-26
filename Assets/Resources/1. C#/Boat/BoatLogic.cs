using UnityEngine;
using System.Collections.Generic;

public class BoatLogic : MonoBehaviour
{
    [SerializeField] private string[] attachableTags = { 
        "Player", 
        "Enemy", 
        "Item", 
        "Crate" 
    };
    
    private HashSet<GameObject> attachedObjects = new HashSet<GameObject>();
    
    void OnCollisionEnter2D(Collision2D collision){
        if(System.Array.Exists(attachableTags, tag => tag == collision.gameObject.tag)){
            if(!attachedObjects.Contains(collision.gameObject)){
                attachedObjects.Add(collision.gameObject);
                
                collision.gameObject.transform.SetAsLastSibling();
                Rigidbody2D rb = collision.gameObject.GetComponent<Rigidbody2D>();
                if(rb != null){
                    rb.linearDamping = 2f;
                    rb.angularDamping = 2f;
                }
            }
        }
    }
    
    void OnCollisionExit2D(Collision2D collision){
        if(attachedObjects.Contains(collision.gameObject)){
            attachedObjects.Remove(collision.gameObject);
            
            Rigidbody2D rb = collision.gameObject.GetComponent<Rigidbody2D>();
            if(rb != null){
                rb.linearDamping = 0f;
                rb.angularDamping = 0f;
            }
        }
    }
    
    void LateUpdate(){
        List<GameObject> toRemove = new List<GameObject>();
        
        foreach(GameObject obj in attachedObjects){
            if(obj == null){
                toRemove.Add(obj);
                continue;
            }
            
            obj.transform.rotation = transform.rotation;
        }
        
        foreach(GameObject obj in toRemove) attachedObjects.Remove(obj);
    }
    
    void OnDrawGizmosSelected(){
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, GetComponent<Collider2D>()?.bounds.size ?? Vector3.one);
    }
}
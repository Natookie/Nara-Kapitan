using UnityEngine;
using NaughtyAttributes;
using UnityEditor;
using System.Text;
using System.IO;
using System.Collections.Generic;
using Nova;

public class UIDebug : MonoBehaviour
{
    public string targetPath = "Assets/Resources/1. C#/UI";

    [Button("Rename All Interactable UIBlock2D", EButtonEnableMode.Editor)]
    void RenameAllUIBlock2DObjects(){
        #if UNITY_EDITOR
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        
        int renamedCount = 0;
        int skippedCount = 0;
        
        foreach(GameObject obj in allObjects){
            if(obj == null) continue;
            
            if(obj.name.StartsWith("lui:")){
                skippedCount++;
                continue;
            }
            
            List<string> matchingComponents = new List<string>();
            Component[] components = obj.GetComponents<Component>();
            
            foreach(Component comp in components){
                if(comp == null) continue;
                
                if(comp is MonoBehaviour mb){
                    string scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(mb));
                    
                    if(!string.IsNullOrEmpty(scriptPath) && scriptPath.Contains(targetPath)){
                        string scriptName = Path.GetFileName(scriptPath);
                        if(scriptName != "UIDebug.cs") matchingComponents.Add(comp.GetType().Name);
                    }
                }
            }
            
            if(matchingComponents.Count > 0){
                string newName = $"lui:{obj.name}";
                Undo.RecordObject(obj, "Rename UI Object");
                obj.name = newName;
                renamedCount++;
            }
        }
        
        Debug.Log($"Renamed: {renamedCount}, Skipped (already had lui:): {skippedCount}");
        #endif
    }
    
    [Button("Reset UI Object Names", EButtonEnableMode.Editor)]
    void ResetUINames(){
        #if UNITY_EDITOR
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        int resetCount = 0;
        
        foreach(GameObject obj in allObjects){
            if(obj == null) continue;
            
            string name = obj.name;
            
            while(name.StartsWith("lui:")) name = name.Substring(4);
            if(name != obj.name){
                Undo.RecordObject(obj, "Reset UI Object Name");
                obj.name = name;
                resetCount++;
            }
        }
        
        Debug.Log($"Reset {resetCount} UIBlock2D objects");
        #endif
    }
    
    [Button("Log All UIBlock2D", EButtonEnableMode.Editor)]
    void LogAllUIBlock2DObjects(){
        #if UNITY_EDITOR
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        int foundCount = 0;
        
        foreach(GameObject obj in allObjects){
            if(obj == null) continue;
            
            UIBlock2D uiBlock = obj.GetComponent<UIBlock2D>();
            if(uiBlock == null) continue;
            
            List<string> matchingComponents = new List<string>();
            Component[] components = obj.GetComponents<Component>();
            
            foreach(Component comp in components){
                if(comp == null) continue;
                
                if(comp is MonoBehaviour mb){
                    string scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(mb));
                    
                    if(!string.IsNullOrEmpty(scriptPath) && scriptPath.Contains(targetPath)){
                        string scriptName = Path.GetFileName(scriptPath);
                        if(scriptName != "UIDebug.cs") matchingComponents.Add(comp.GetType().Name);
                    }
                }
            }
            
            if(matchingComponents.Count > 0){
                foundCount++;
                Debug.Log($"Object: {obj.name}, Components: {string.Join(", ", matchingComponents)}");
            }
        }
        
        Debug.Log($"Total UIBlock2D objects found: {foundCount}");
        #endif
    }
    
    #if UNITY_EDITOR
    string GetGameObjectPath(GameObject obj){
        string path = obj.name;
        Transform current = obj.transform;
        
        while(current.parent != null){
            current = current.parent;
            path = current.name + "/" + path;
        }
        
        return path;
    }
    #endif
}
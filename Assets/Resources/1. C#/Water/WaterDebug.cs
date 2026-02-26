// using UnityEngine;

// public class WaterDebug : MonoBehaviour
// {
//     public WaterVisuals waterVisuals;
//     public StormyOcean ocean;
    
//     void OnGUI(){
//         if(waterVisuals == null || ocean == null) return;
        
//         GUILayout.BeginArea(new Rect(10, 10, 300, 400));
//         GUILayout.Label("=== WATER DEBUG ===");
        
//         GUILayout.Label($"Storm Intensity: {ocean.GetStormIntensity():F2}");
//         GUILayout.Label($"Max Wave Height: {ocean.maxWaveHeight:F2}");
        
//         GUILayout.Label($"Foam Intensity: {waterVisuals.foamIntensity:F2}");
//         GUILayout.Label($"Foam Spread: {waterVisuals.foamSpread:F2}");
        
//         if(GUILayout.Button("MAX FOAM TEST")){
//             waterVisuals.foamIntensity = 2f;
//             waterVisuals.foamSpread = 1f;
//             ocean.stormIntensity = 1f;
//             ocean.maxWaveHeight = 2f;
//             Debug.Log("Max foam test applied!");
//         }
        
//         if(GUILayout.Button("RESET")){
//             waterVisuals.foamIntensity = 1f;
//             waterVisuals.foamSpread = 0.3f;
//             ocean.stormIntensity = 0.3f;
//             ocean.maxWaveHeight = 0.8f;
//         }
        
//         if(GUILayout.Button("CHECK MESH UVs")){
//             Mesh mesh = waterVisuals.GetComponent<MeshFilter>().mesh;
//             Vector2[] uvs = mesh.uv;
//             if(uvs != null && uvs.Length > 0){
//                 Debug.Log($"Mesh has {uvs.Length} UVs");
//                 Debug.Log($"First UV: {uvs[0]}");
//                 Debug.Log($"Middle UV: {uvs[uvs.Length/2]}");
//                 Debug.Log($"Last UV: {uvs[uvs.Length-1]}");
//             }
//         }
        
//         if(GUILayout.Button("CHECK SHADER VALUES")){
//             Material mat = waterVisuals.GetComponent<MeshRenderer>().material;
//             if(mat != null){
//                 Debug.Log($"Shader: {mat.shader.name}");
//                 Debug.Log($"StormIntensity: {mat.GetFloat("_StormIntensity")}");
//                 Debug.Log($"FoamIntensity: {mat.GetFloat("_FoamIntensity")}");
//                 Debug.Log($"FoamSpread: {mat.GetFloat("_FoamSpread")}");
//             }
//         }
        
//         GUILayout.EndArea();
//     }
// }
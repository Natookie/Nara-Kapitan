using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    public static SceneChanger Instance {get; private set;} 

    public void ChangeScene(string sceneName) => SceneManager.LoadScene(sceneName);
    public void RestartCurrentScene() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    public void LoadNextScene(){
        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
        SceneManager.LoadScene(nextIndex);
    }

    void Awake(){
        if(Instance != null && Instance != this){
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
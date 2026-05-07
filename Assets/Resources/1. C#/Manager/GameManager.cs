using UnityEngine;
using NaughtyAttributes;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    [SerializeField] private int score = 0;

    public static GameManager Instance {get; private set;}
    public bool isPaused = false;

    void Awake(){
        if(Instance != null && Instance != this){
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update(){
        HandleInput();
    }

    void HandleInput(){
        if(Keyboard.current.escapeKey.wasPressedThisFrame){
            isPaused ^= true;
            if(isPaused) Time.timeScale = 0f;
            else Time.timeScale = 1f;
        }
    }

    public int GetScore() => score;
    public void AddScore(int amount) => score += amount;
}

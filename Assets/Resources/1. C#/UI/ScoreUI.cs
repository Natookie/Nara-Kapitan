using UnityEngine;
using Nova;

public class ScoreUI : MonoBehaviour
{
    [SerializeField] private TextBlock scoreText;
    private int currentScore = 0;

    void Start(){
        currentScore = GameManager.Instance.GetScore();
    }

    void Update(){
        if(currentScore != GameManager.Instance.GetScore()){
            currentScore = GameManager.Instance.GetScore();
            scoreText.Text = $"Score: {currentScore}";
        }
    }
}
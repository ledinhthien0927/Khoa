using UnityEngine;

public class UIEffectController : MonoBehaviour
{
    public ParticleSystem effect;

    private bool gameStarted = false;
    private bool gameEnded = false;

    void Start()
    {
        effect.Stop();
    }

    public void OnGameStart()
    {
        if (gameEnded) return;

        gameStarted = true;
        effect.Play();
    }

    public void OnGameWin()
    {
        EndGame();
    }

    public void OnGameLose()
    {
        EndGame();
    }

    void EndGame()
    {
        if (!gameStarted) return;

        gameEnded = true;
        effect.Stop();
    }
}

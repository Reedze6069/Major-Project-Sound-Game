using UnityEngine;
using UnityEngine.SceneManagement;

public class GameFlowManager : MonoBehaviour
{
    public GameObject startScreen;
    public GameObject deathScreen;

    void Start()
    {
        ShowStartScreen();
    }

    public void StartGame()
    {
        if (startScreen != null)
            startScreen.SetActive(false);

        if (deathScreen != null)
            deathScreen.SetActive(false);

        Time.timeScale = 1f;
    }

    public void ShowStartScreen()
    {
        if (startScreen != null)
            startScreen.SetActive(true);

        if (deathScreen != null)
            deathScreen.SetActive(false);

        Time.timeScale = 0f;
    }

    public void ShowDeathScreen()
    {
        if (startScreen != null)
            startScreen.SetActive(false);

        if (deathScreen != null)
            deathScreen.SetActive(true);

        Time.timeScale = 0f;
    }

    public void PlayAgain()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}

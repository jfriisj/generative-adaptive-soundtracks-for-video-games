using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    public void StartAdaptive()
    {
        SceneManager.LoadScene("Adaptive Scene");
    }

    public void StartStatic()
    {
        SceneManager.LoadScene("Static Scene");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
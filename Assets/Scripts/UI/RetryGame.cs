using UnityEngine;
using UnityEngine.SceneManagement;

public class RetryGame : MonoBehaviour
{
    [SerializeField] private GameObject audioManager;

    public void Retry()
    {
        var a = audioManager.GetComponent<AudioManager>();
        a.FMODResetParams();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseGame : MonoBehaviour
{
    [SerializeField] private GameObject audioManager;
    [SerializeField] private GameObject PauseUI;
    [SerializeField] private GameObject Player;
    [SerializeField] private GameObject Camera;
    private PostDB db;
    private bool pausedGame;

    private void Update()
    {
        db = GetComponent<PostDB>();
        if (Input.GetButtonDown("Cancel") && !pausedGame)
            Pause();
        else if (Input.GetButtonDown("Cancel") && pausedGame) UnPause();
    }

    public void QuitGame()
    {
        db.addScore();
        Application.Quit();
    }

    public void Pause()
    {
        pausedGame = true;
        PauseUI.SetActive(true);
        Time.timeScale = 0f;
    }

    public void UnPause()
    {
        pausedGame = false;
        PauseUI?.SetActive(false);
        Time.timeScale = 1f;
    }

    public void Unstuck()
    {
        var a = audioManager.GetComponent<AudioManager>();
        a.ResetBiomeInfluences();
        Player.transform.position = new Vector3(0, 0, Player.transform.position.z);
        Camera.transform.position = new Vector3(0, 0, Camera.transform.position.z);
        UnPause();
    }

    public void MainMenu()
    {
        var a = audioManager.GetComponent<AudioManager>();
        a.StopBGM();
        db.addScore();
        Time.timeScale = 1f;
        SceneManager.LoadScene("Menu");
    }
}
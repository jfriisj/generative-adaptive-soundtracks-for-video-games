using System;
using System.Collections;
using MySqlConnector;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class PostDB : MonoBehaviour
{
    private MySqlConnection conn;
    private string connstr;
    private readonly string database = "spedsite_net_db";
    private readonly string host = "mysql18.unoeuro.com";
    private readonly string password = "rg6GFy2Rwmk4pd5hBAeD";
    private int sceneVer;
    private int secondsInGame;
    private readonly string URL = "http://spedsite.net/insert.php";

    private readonly string user = "spedsite_net";

    // Start is called before the first frame update
    private void Start()
    {
        connstr = "server=" + host + ";user=" + user + ";password=" + password + ";database=" + database;
        //connstr = "server=localhost;user=root;database=bcldb;port=3306;password=";
        conn = new MySqlConnection(connstr);

        if (SceneManager.GetActiveScene().name == "Adaptive Scene")
            sceneVer = 0;
        else
            sceneVer = 1;

        StartCoroutine(timeCounter());
    }

    public void addScore()
    {
        try
        {
            conn.Open();
            var timeMin = (secondsInGame / 60).ToString();
            var timeSec = (secondsInGame % 60).ToString();

            if (timeMin.Length == 1) timeMin = "0" + timeMin;
            if (timeSec.Length == 1) timeSec = "0" + timeSec;

            var totalTime = "00:" + timeMin + ":" + timeSec;

            var sql = "INSERT INTO bcldb (time, game_version) VALUES (@time, @game)";
            var cmd = new MySqlCommand(sql, conn);

            cmd.Parameters.AddWithValue("@time", totalTime);
            cmd.Parameters.AddWithValue("@game", sceneVer);
            cmd.ExecuteNonQuery();
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }

        conn.Close();
        //postScore();
    }

    private IEnumerator postScore()
    {
        var post_url = URL + "time=" + secondsInGame + "&game=" + sceneVer;
        var post = UnityWebRequest.PostWwwForm(post_url, "");

        yield return post.SendWebRequest();

        if (post.error != null) Debug.Log(post.error);
        Debug.Log(post.downloadHandler.text);
    }

    private IEnumerator timeCounter()
    {
        while (true)
        {
            secondsInGame++;
            yield return new WaitForSeconds(1);
        }
    }
}
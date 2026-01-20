using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public GameObject HUD;
    public GameObject[] Player;
    public GameObject EscPanel;
    int GameState = 0;

    private GameObject GamePlayer;
    public GameObject SpawnPoint;

    void Start()
    {
        if (GameManager.Instance.GamePlayer == null)
        {
            GamePlayer = Instantiate(Player[GameManager.Instance.SelectedCharacterIndex]);
        }
        else
        {
            GamePlayer = GameManager.Instance.GamePlayer;
        }
        GamePlayer.transform.position = SpawnPoint.transform.position;
        GamePlayer.SetActive(true);

        GameManager.Instance.GamePlayer = GamePlayer;

        HUD.SetActive(true);
        HUD.GetComponent<HUD>().BindPlayer(GamePlayer);
        EscPanel.SetActive(false);

        GameState = 0;
        Time.timeScale = 1f;


        var gameData = GameManager.Instance.LoadGame(GameManager.Instance.SelectedCharacterIndex);
        if (GameManager.Instance.ShouldLoad && gameData != null)
        {
            GamePlayer.transform.position = new Vector3(gameData.posX, gameData.posY, gameData.posZ);
            GamePlayer.GetComponent<Player>().Score = gameData.score;
            GameManager.Instance.ShouldLoad = false;
        }
        else
        {
            GamePlayer.GetComponent<Player>().Score = GameManager.Instance.TempScore;
        }


        GameManager.Instance.CurrentMap = SceneManager.GetActiveScene().name;
    }

    public void OnEsc()
    {
        // Game Stopped
        if (GameState == 1)
        {
            Time.timeScale = 1f;
            EscPanel.SetActive(false);
            GameState = 0;
        }
        else
        {
            Time.timeScale = 0f;
            EscPanel.SetActive(true);
            GameState = 1;
        }
    }

    public void SaveAndQuit()
    {
        GameManager.Instance.SaveGame();
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
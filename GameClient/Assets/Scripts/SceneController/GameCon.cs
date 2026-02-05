using Protos;
using System.IO;
using TMPro;
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

    public TMP_InputField InputField;

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
        GamePlayer.GetComponent<Player>().UserName = GameManager.Instance.Session.GetUsername();

        GameManager.Instance.GamePlayer = GamePlayer;
        GamePlayer.GetComponent<Player>().EnableInput();

        HUD.SetActive(true);
        HUD.GetComponent<HUD>().BindPlayer(GamePlayer);
        EscPanel.SetActive(false);

        GameState = 0;
        Time.timeScale = 1f;

        Debug.Log(GamePlayer.GetComponent<Player>().Score);

        if (GameManager.Instance.ShouldLoad)
        {
            var gameData = GameManager.Instance.LoadGame(GameManager.Instance.SelectedCharacterIndex);
            if (gameData != null)
            {

                GamePlayer.transform.position = new Vector3(gameData.posX, gameData.posY, gameData.posZ);
                GamePlayer.GetComponent<Player>().Score = gameData.score;
                Debug.Log(GamePlayer.GetComponent<Player>().Score);

                GameManager.Instance.ShouldLoad = false;
            }
        }
        else
        {
            GamePlayer.GetComponent<Player>().Score = GameManager.Instance.TempScore;
        }

        GameManager.Instance.CurrentMap = SceneManager.GetActiveScene().name;
    }

    public void OnEsc()
    {
        if (InputField.isFocused)
            return;

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
        GameManager.Instance.Session.ExitGame();
        Time.timeScale = 1f;
        if (!GameManager.Instance.Session.IsMulti)
        {
            GameManager.Instance.SaveGame();
        }
        SceneManager.LoadScene("MainMenu");
    }

    public void OnChat()
    {
        if (InputField.isFocused)
            return;
        Debug.Log("Enter");
        GameManager.Instance.GamePlayer.GetComponent<Player>().DisableInput();

        InputField.ActivateInputField();
    }

    public GameObject SpawnPlayer(SyncMessage message)
    {
        var player = Instantiate(Player[message.PrefabId]);
        player.transform.position = new Vector2(message.PositionX, message.PositionY);
        player.SetActive(true);

        return player;
    }
}
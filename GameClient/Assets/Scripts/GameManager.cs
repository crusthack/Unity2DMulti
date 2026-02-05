using Protos;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public GameObject[] PlayerPrefabs;
    public int SelectedCharacterIndex = 0;
    public GameObject GamePlayer;

    public string CurrentMap;
    public int playerCount;

    private SaveManager saveManager;
    public bool ShouldLoad = false;
    public float TempScore;

    public NetworkManager NetworkManager;
    public GameSession Session;

    public GameNetworkCon NetworkCon;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Physics2D.queriesHitTriggers = false;

        saveManager = new SaveManager();
        Session = new GameSession();
    }

    public void SaveGame()
    {
        if (GamePlayer != null)
        {
            Player playerComponent = GamePlayer.GetComponent<Player>();
            saveManager.Save(
                SelectedCharacterIndex,
                CurrentMap,
                playerComponent.Score,
                GamePlayer.transform
            );
        }
    }

    public GameData LoadGame(int index)
    {
        if (saveManager.Load(index, out GameData data))
        {
            return data;
        }

        return new GameData();
    }

    public void GoToMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}

public class GameSession
{
    bool IsLoggedIn = false;
    string Username = "";
    public bool IsMulti = false;
    public bool IsHost = false;

    public bool IsLogin()
    {
        return IsLoggedIn;
    }

    public void ExitGame()
    {
        IsMulti = false;
        IsHost = false;
    }

    public void LoginSuccess(string username)
    {
        IsLoggedIn = true;
        Username = username;
    }

    public void Logout()
    {
        IsLoggedIn = false;
        Username = null;
    }

    public string GetUsername()
    {
        return Username;
    }
}



[System.Serializable]
public class GameData
{
    public int CharacterIndex = 0;
    public string MapName = "Map_A";
    public float score = 0;
    public float posX = 0;
    public float posY = 0;
    public float posZ = 0;
}

public class SaveManager
{
    string GetSavePath(int characterIndex)
    {
        return Path.Combine(
            Application.persistentDataPath,
            $"save_{characterIndex}.json"
        );
    }

    public void Save(int characterIndex, string mapName, float score, Transform player)
    {
        GameData data = new GameData
        {
            CharacterIndex = characterIndex,
            MapName = mapName,
            score = score,
            posX = player.position.x,
            posY = player.position.y,
            posZ = player.position.z
        };

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(GetSavePath(characterIndex), json);

        Debug.Log($"Saved: {GetSavePath(characterIndex)}");
    }

    public bool Load(int characterIndex, out GameData data)
    {
        string path = GetSavePath(characterIndex);

        if (!File.Exists(path))
        {
            data = null;
            return false;
        }

        string json = File.ReadAllText(path);
        data = JsonUtility.FromJson<GameData>(json);
        return true;
    }
}

using TMPro;
using UnityEngine;

public class CharacterSelectMenu : MonoBehaviour
{
    public TMP_Text CharacterInfo0;
    public TMP_Text CharacterInfo1;

    void Start()
    {
        var saveInfo = GameManager.Instance.LoadGame(0);
        CharacterInfo0.text = $"Map: {saveInfo.MapName}\n" +
            $"Score: {saveInfo.score}\n";

        var saveInfo1 = GameManager.Instance.LoadGame(1);
        CharacterInfo1.text = $"Map: {saveInfo1.MapName}\n" +
            $"Score: {saveInfo1.score}\n";
    }
    public void OnSelectCharacter(int index)
    {
        GameManager.Instance.SelectedCharacterIndex = index;
        Debug.Log($"Character {index} selected.");
    }

    public void OnGameStart()
    {
        GameManager.Instance.ShouldLoad = true;
        var mapName = GameManager.Instance.LoadGame(
            GameManager.Instance.SelectedCharacterIndex).MapName;
        if (string.IsNullOrEmpty(mapName))
        {
            mapName = "Map_A";
        }
        UnityEngine.SceneManagement.SceneManager.LoadScene($"{mapName}");
    }
}

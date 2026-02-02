using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public GameObject LoginPanel;
    public TMP_Text Username;

    public void LoadSinglePlay()
    {
        SceneManager.LoadScene("CharacterSelect");
    }

    public void OnClickLogin()
    {
        LoginPanel.SetActive(true);
    }

    public void OnClickMultiPlay(int state)
    {
        if (!GameManager.Instance.Session.IsLogin())
        {
            OnClickLogin();
            return;
        }
        switch (state)
        {
            // dedicated server
            case 0:
                break;

            // local host server
            case 1:
                SceneManager.LoadScene("MultiJoin");
                break;
        }
    }

    public void Logout()
    {
        if (GameManager.Instance.Session.IsLogin())
        {
            GameManager.Instance.Session.Logout();
            GameManager.Instance.NetworkManager.Disconnect();
            Username.text = null;
        }
    }
}

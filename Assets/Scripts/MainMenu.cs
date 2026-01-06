using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    bool IsLogined = false;

    public GameObject LoginPanel;
    void Start()
    {
        IsLogined = false;
    }

    void Update()
    {

    }

    public void LoadSinglePlay()
    {
        SceneManager.LoadScene("CharacterSelect");
    }

    public void OnClickLogin()
    {
        if (IsLogined)
        {
            Logout();
        }
        else
        {
            LoginPanel.SetActive(true);
        }
    }

    public void OnClickMultiPlay(int state)
    {
        if(!IsLogined)
        {
            OnClickLogin();
            return;
        }
        switch(state)
        {
            // dedicated server
            case 0: 
                break;

            // local host server
            case 1:
                break;
        }
    }

    private void Logout()
    {
        IsLogined = false;
    }
}

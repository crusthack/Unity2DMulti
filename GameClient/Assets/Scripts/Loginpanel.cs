using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows;

public class Loginpanel : MonoBehaviour
{
    public TMP_InputField IdInput;
    public TMP_Text LoginStatus;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void OnClickLogin()
    {
        if(GameManager.Instance.Session.IsLogin())
        {
            return;
        }
        string name = IdInput.text;
        LoginStatus.text = "Trying login...";

        // 유효성 검사
        if (string.IsNullOrEmpty(name))
        {
            LoginStatus.text = "Please Input Your ID";
            return;
        }

        GameManager.Instance.NetworkManager.Login(name);
    }

    public void OpenWebPage()
    {
        Application.OpenURL("https://www.naver.com");
    }

    public void OnClickClose()
    {
        gameObject.SetActive(false);
    }
}

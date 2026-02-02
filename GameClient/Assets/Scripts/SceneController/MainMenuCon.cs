using NetworkController.Message;
using Protos;
using TMPro;
using UnityEngine;

public class MainMenuHandler : MonoBehaviour
{
    public TMP_Text LoginStatus;
    public TMP_Text Username;
    void Awake()
    {
        GameManager.Instance.NetworkManager.OnMessageRecv += HandleMessage;

        Username.text = GameManager.Instance.Session.GetUsername();
    }

    private void OnDestroy()
    {
        GameManager.Instance.NetworkManager.OnMessageRecv -= HandleMessage;
    }

    void HandleMessage(ProtobufMessage message)
    {
        switch ((ProtobufMessage.OpCode)message.Header.OpCode)
        {
            case ProtobufMessage.OpCode.System:
                HandleSystem(message);
                break;
        }
    }

    void HandleSystem(ProtobufMessage message)
    {
        var system = message.Payload as SystemMessage;
        switch (system.PayloadCase)
        {
            case SystemMessage.PayloadOneofCase.LoginResponse:
                if (system.LoginResponse.Success)
                {
                    LoginSuccess(system.LoginResponse);
                }
                else
                {
                    LoginFail(system.LoginResponse);
                }
                break;
        }
    }

    void LoginSuccess(LoginResponse message)
    {
        GameManager.Instance.Session.LoginSuccess(message.UserName);
        Username.text = message.UserName;
        LoginStatus.text = message.Message;
    }

    void LoginFail(LoginResponse message)
    {
        Username.text = null;
        LoginStatus.text = message.Message;
    }
}
    
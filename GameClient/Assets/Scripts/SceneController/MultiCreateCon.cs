using NetworkController.Message;
using Protos;
using System.Reflection.Emit;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MultiCreateCon : MonoBehaviour
{
    public TMP_InputField RoomName;

    private void Awake()
    {
        GameManager.Instance.NetworkManager.OnMessageRecv += HandleMessage;
    }

    private void OnDestroy()
    {
        GameManager.Instance.NetworkManager.OnMessageRecv -= HandleMessage;
    }
    public void OnCreateClick()
    {
        if (RoomName.text.Length < 1) return;
        if (!GameManager.Instance.Session.IsLogin()) return;

        var roomMsg = new RoomMessage
        {
            CreateRoom = new CreateRoom
            {
                RoomName = RoomName.text
            }
        };
        var msg = new ProtobufMessage(roomMsg, ProtobufMessage.OpCode.Room);
        GameManager.Instance.NetworkManager.SendMessage(msg);
    }

    void HandleMessage(ProtobufMessage message)
    {
        if ((ProtobufMessage.OpCode)message.Header.OpCode == ProtobufMessage.OpCode.Room)
        {
            var roomMsg = message.Payload as RoomMessage;
            if (roomMsg.PayloadCase == RoomMessage.PayloadOneofCase.CreateRoom)
            {
                Debug.Log(roomMsg.CreateRoom.RoomName);
                GameManager.Instance.Session.SetMulti(true);
                SceneManager.LoadScene("CharacterSelect");
            }
        }
    }

    public void OnBack()
    {
        SceneManager.LoadScene("MultiJoin");
    }
}

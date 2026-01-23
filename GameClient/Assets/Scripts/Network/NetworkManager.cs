using UnityEngine;
using NetworkController;
using NetworkController.Message;
using System.Net;
using Protos;

public class NetworkManager : MonoBehaviour
{
    NetworkController<ProtobufMessage> Netcon;

    private void OnEnable()
    {
        Netcon = new NetworkController<ProtobufMessage>();
    }

    public void Login(string userName)
    {
        if (!Netcon.IsConnected())
        {
            Netcon.Connect(IPAddress.Parse("127.0.0.1"), 5000);
        }

        Netcon.SendMessage(new ProtobufMessage(
            new SystemMessage
            {
                LoginRequest = new Protos.LoginRequest
                {
                    UserName = userName
                }
            },
            ProtobufMessage.OpCode.System)
        );
    }

    private void OnDestroy()
    {
        if (Netcon.IsConnected())
        {
            Netcon.Disconnect();
        }
    }
}

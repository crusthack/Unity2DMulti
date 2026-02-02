using NetworkController;
using NetworkController.Message;
using Protos;
using System;
using System.Net;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    private NetworkController<ProtobufMessage> Netcon;

    public int HeartbeatInterval = 1000; // milliseconds
    private float lastHeartbeatTime;

    public event Action<ProtobufMessage> OnMessageRecv;

    private void OnEnable()
    {
        Netcon = new NetworkController<ProtobufMessage>();
        lastHeartbeatTime = Time.time;
        Netcon.OnDisconnect += Disconnect;
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
                LoginRequest = new LoginRequest
                {
                    UserName = userName
                }
            },
            ProtobufMessage.OpCode.System)
        );
    }

    private void Update()
    {
        if (Netcon.IsConnected())
        {
            if (Netcon.IsMessageAvailable())
            {
                var message = Netcon.GetMessage();
                if (message == null)
                {
                    return;
                }
                OnMessageRecv.Invoke(message);
            }
        }

        HandleHeartbeat();
    }

    private void HandleHeartbeat()
    {
        if (HeartbeatInterval <= 0)
            return;

        if (!Netcon.IsConnected())
            return;

        float intervalSeconds = HeartbeatInterval / 1000f;

        if (Time.time - lastHeartbeatTime >= intervalSeconds)
        {
            SendHeartbeat();
            lastHeartbeatTime = Time.time;
        }
    }

    private void SendHeartbeat()
    {
        SendMessage(new ProtobufMessage(
            new SystemMessage
            {
                Heartbeat = new Heartbeat
                {
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                }
            },
            ProtobufMessage.OpCode.System)
        );
    }

    private void OnDestroy()
    {
        if (Netcon != null && Netcon.IsConnected())
        {
            Netcon.Disconnect();
        }
    }

    public void SendChat(string text)
    {
        var chat = new ChattingMessage
        {
            Message = text
        };
        var message = new ProtobufMessage(chat, ProtobufMessage.OpCode.Chatting);

        SendMessage(message);
    }

    public void SendMessage(ProtobufMessage message)
    {
        if (!Netcon.IsConnected())
        {
            return;
        }

        Netcon.SendMessage(message);
    }

    public void Disconnect()
    {
        if (Netcon.IsConnected())
        {
            Netcon.Disconnect();
        }
    }

    public void Disconnect(SocketContext<ProtobufMessage> context)
    {
        Debug.Log("Server disconnected");
        Disconnect();
    }
}

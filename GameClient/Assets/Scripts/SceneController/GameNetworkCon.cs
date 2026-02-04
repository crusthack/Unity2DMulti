using UnityEngine;
using System.Collections.Generic;
using NetworkController.Message;
using Protos;
using TMPro;

public class GameNetworkCon : MonoBehaviour
{
    public GameObject scoreboard;
    public List<GameObject> scores;

    int playerID = 0; // hostUser = 0
    Dictionary<int, GameObject> Players = new();
    int NextSessionID = 1;

    public float SyncInterval = 1;
    private float lastSyncTime = 0;

    public SceneController sceneController;


    void Awake()
    {
        if (!GameManager.Instance.Session.IsMulti)
        {
            return;
        }
        GameManager.Instance.NetworkManager.OnMessageRecv += HandleMessage;
    }

    private void OnDestroy()
    {
        if (!GameManager.Instance.Session.IsMulti)
        {
            return;
        }
        GameManager.Instance.NetworkManager.OnMessageRecv -= HandleMessage;
    }

    void Start()
    {
        if (!GameManager.Instance.Session.IsMulti)
        {
            return;
        }

        scoreboard.SetActive(true);
        if (GameManager.Instance.Session.IsHost)
        {
            Debug.Log("This is host");
        }
        else
        {
            Debug.Log("This is guest");
        }
    }

    void Update()
    {
        if (!GameManager.Instance.Session.IsMulti)
        {
            return;
        }

        UpdateScoreboard();
        SendSyncMessage();
        CheckVisibility();
    }

    void CheckVisibility()
    {
        foreach(var player in Players.Values)
        {
            if(player.GetComponent<Player>().CurrentMap == GameManager.Instance.GamePlayer.GetComponent<Player>().CurrentMap)
            {
                player.SetActive(true);
            }
            else
            {
                player.SetActive(false);
            }
        }
    }

    void UpdateScoreboard()
    {
        List<Player> players = new List<Player>();
        foreach(var p in Players.Values)
        {
            players.Add(p.GetComponent<Player>());
        }
        players.Add(GameManager.Instance.GamePlayer.GetComponent<Player>());

        players.Sort((a, b) =>
        {
            return (int)(a.Score - b.Score);
        });

        foreach(var s in scores)
        {
            s.SetActive(false);
        }

        int index = 0;
        foreach (var p in players)
        {
            scores[index].transform.GetChild(0).GetComponent<TMP_Text>().text = p.UserName + ": " + p.Score;
            scores[index].SetActive(true);
            index++;
            if(index == scores.Count)
            {
                break;
            }
        }
    }

    void SendSyncMessage()
    {
        if (Time.time - lastSyncTime < SyncInterval)
        {
            return;
        }
        lastSyncTime = Time.time;

        // 자신 게임의 상태를 송신
        var s = GameManager.Instance.GamePlayer.GetComponent<Player>().GetSyncInfo();
        s.PlayerId = playerID;
        var g = new GameMessage
        {
            DoBroadcast = GameManager.Instance.Session.IsHost,
            GameSync = s
        };
        var message = new ProtobufMessage(g, ProtobufMessage.OpCode.Game);
        GameManager.Instance.NetworkManager.SendMessage(message);

        // 호스트 유저라면 자기 컴퓨터에 있는 모든 유저들의 정보를 전파
        if (GameManager.Instance.Session.IsHost)
        {
            foreach (var (i, p) in Players)
            {
                var info = p.GetComponent<Player>().GetSyncInfo();
                info.PlayerId = i;

                var gameMessage = new GameMessage
                {
                    DoBroadcast = true,
                    GameSync = info
                };

                var msg = new ProtobufMessage(gameMessage, ProtobufMessage.OpCode.Game);
                GameManager.Instance.NetworkManager.SendMessage(msg);
            }
        }
    }

    void HandleMessage(ProtobufMessage message)
    {
        switch ((ProtobufMessage.OpCode)message.Header.OpCode)
        {
            case ProtobufMessage.OpCode.Game:
                HandleGamemessage(message.Payload as GameMessage);
                break;
        }
    }

    void HandleGamemessage(GameMessage message)
    {
        switch (message.PayloadCase)
        {
            case GameMessage.PayloadOneofCase.GameSync:
                SyncGame(message);
                break;
            case GameMessage.PayloadOneofCase.Rpc:
                HandleRPC(message.Rpc);
                break;
        }
    }

    void SyncGame(GameMessage msg)
    {
        Debug.Log("Received Sync Message");
        var message = msg.GameSync;

        if(message.PlayerId == 0 && GameManager.Instance.Session.IsHost)
        {
            var g = new GameMessage
            {
                SessionID = msg.SessionID,
                DoBroadcast = false,
                Rpc = new RPC
                {
                    PlayerId = NextSessionID,
                    RpcName = "SetPlayerID",
                }
            };

            var m = new ProtobufMessage(g, ProtobufMessage.OpCode.Game);
            GameManager.Instance.NetworkManager.SendMessage(m);
            message.PlayerId = NextSessionID++;
        }

        if (Players.TryGetValue(message.PlayerId, out var p))
        {
            p.GetComponent<Player>().Sync(message);
        }
        else
        {
            var newPlayer = sceneController.SpawnPlayer(message);
            Players.Add(message.PlayerId, newPlayer);
            newPlayer.GetComponent<Player>().Sync(message);
            newPlayer.GetComponent<Player>().UserName = message.UserName;
        }
    }

    void HandleRPC(RPC message)
    {
        if(message.RpcName == "SetPlayerID")
        {
            playerID = message.PlayerId;
        }
    }
}

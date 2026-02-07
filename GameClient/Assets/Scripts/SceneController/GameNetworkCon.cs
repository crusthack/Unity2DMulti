using NetworkController.Message;
using Protos;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameNetworkCon : MonoBehaviour
{
    public GameObject scoreboard;
    public List<GameObject> scores;

    int playerID = -1; // hostUser = 0, unauthorized = -1
    ConcurrentDictionary<int, GameObject> Players = new();    // key = sessionid

    public float SyncInterval = 1;
    private float lastSyncTime = 0;

    public SceneController sceneController;


    void Awake()
    {
        GameManager.Instance.NetworkCon = this;
        GameManager.Instance.NetworkManager.OnMessageRecv += HandleMessage;
    }

    private void OnDestroy()
    {
        GameManager.Instance.NetworkManager.OnMessageRecv -= HandleMessage;
    }

    void Start()
    {
        if (!GameManager.Instance.Session.IsMulti)
            return;

        scoreboard.SetActive(true);

        Debug.Log(GameManager.Instance.Session.IsHost ? "This is host" : "This is guest");
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
        var localPlayer = GameManager.Instance.GamePlayer?.GetComponent<Player>();
        if (localPlayer == null) return;

        foreach (var obj in Players.Values)
        {
            if (obj == null) continue;

            var p = obj.GetComponent<Player>();
            if (p == null) continue;

            bool shouldActive = p.CurrentMap == localPlayer.CurrentMap;
            if (obj.activeSelf != shouldActive)
                obj.SetActive(shouldActive);
        }
    }

    void UpdateScoreboard()
    {
        if (scores == null || scores.Count == 0)
            return;

        List<Player> players = new();

        foreach (var obj in Players.Values)
        {
            if (obj == null) continue;
            var p = obj.GetComponent<Player>();
            if (p != null)
                players.Add(p);
        }

        var localPlayer = GameManager.Instance.GamePlayer?.GetComponent<Player>();
        if (localPlayer != null)
            players.Add(localPlayer);

        players.Sort((a, b) => b.Score.CompareTo(a.Score));

        foreach (var s in scores)
            if (s != null) s.SetActive(false);

        for (int i = 0; i < players.Count && i < scores.Count; i++)
        {
            if (scores[i] == null) continue;

            scores[i].transform.GetChild(0)
                .GetComponent<TMP_Text>().text =
                $"{players[i].UserName}: {players[i].Score}";

            scores[i].SetActive(true);
        }
    }

    void SendSyncMessage()
    {
        if (Time.time - lastSyncTime < SyncInterval)
            return;

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
                Debug.Log(gameMessage.ToString());
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
                if (GameManager.Instance.Session.IsHost)
                {
                    HostPlayerSyncGame(message);
                }
                else
                {
                    SyncGame(message);
                }
                break;
            case GameMessage.PayloadOneofCase.Rpc:
                HandleRPC(message.Rpc);
                break;
        }
    }

    // 호스트 플레이어 전용 
    void HostPlayerSyncGame(GameMessage msg)
    {
        var message = msg.GameSync;

        if (Players.TryGetValue(msg.SessionID, out var p))
        {
        }
        else
        {
            var newPlayer = sceneController.SpawnPlayer(message);
            newPlayer.GetComponent<Player>().UserName = message.UserName;

            newPlayer.GetComponent<Player>().Sync(message);
            Players.TryAdd(msg.SessionID, newPlayer);
            Debug.Log("New player: " + message.UserName + "join to game");
            InitClient(msg);
        }
    }

    int InitClient(GameMessage msg)
    {
        var g = new GameMessage
        {
            SessionID = msg.SessionID,
            DoBroadcast = false,
            Rpc = new RPC
            {
                PlayerId = msg.SessionID,
                RpcName = "SetPlayerID",
            }
        };

        Debug.Log(msg.SessionID + " New Client try To Join   " + g.Rpc.PlayerId);
        Debug.Log($"Send Set Playr ID RPC to {g.SessionID}");
        var m = new ProtobufMessage(g, ProtobufMessage.OpCode.Game);
        GameManager.Instance.NetworkManager.SendMessage(m);
        return g.Rpc.PlayerId;
    }

    void SyncGame(GameMessage msg)
    {
        var message = msg.GameSync;

        // 아직 호스트 유저로부터 번호 부여 안 받은 상태
        if (playerID == -1)
        {
            return;
        }

        if(playerID == message.PlayerId)
        {
            GameManager.Instance.GamePlayer.GetComponent<Player>().Sync(message);
            return;
        }

        if (Players.TryGetValue(message.PlayerId, out var p))
        {
            p.GetComponent<Player>().Sync(message);
        }
        else
        {
            var newPlayer = sceneController.SpawnPlayer(message);
            newPlayer.GetComponent<Player>().Sync(message);
            newPlayer.GetComponent<Player>().UserName = message.UserName + "(" + message.PlayerId.ToString() + ")";
            Players.TryAdd(message.PlayerId, newPlayer);
            Debug.Log("New player: " + message.UserName + "join to game");
        }
    }

    void HandleRPC(RPC message)
    {
        if (message.PlayerId == playerID)
        {
            return;
        }

        switch (message.RpcName)
        {
            case "SetPlayerID":
                {
                    playerID = message.PlayerId;
                    GameManager.Instance.GamePlayer.GetComponent<Player>().UserName += $"({message.PlayerId})";
                }
                Debug.Log("Set Player ID: " + playerID);
                break;
            case "Move":
                ProcessRPCMove(message);
                break;
            case "Attack":
                ProcessRPCAttack(message);
                break;
        }
    }

    public void RPC_Move(InputValue value)
    {
        if (!GameManager.Instance.Session.IsMulti)
        {
            return;
        }

        var x = value.Get<Vector2>().x;
        var y = value.Get<Vector2>().y;

        var g = new GameMessage
        {
            Rpc = new RPC
            {
                PlayerId = playerID,
                RpcName = "Move",
                Values = { x.ToString(), y.ToString() }
            }
        };

        if (GameManager.Instance.Session.IsHost)
        {
            g.DoBroadcast = true;
        }

        var m = new ProtobufMessage(g, ProtobufMessage.OpCode.Game);
        GameManager.Instance.NetworkManager.SendMessage(m);
    }

    void ProcessRPCMove(RPC message)
    {
        if (!Players.TryGetValue(message.PlayerId, out var p))
            return;

        if (!float.TryParse(message.Values[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x))
            return;

        if (!float.TryParse(message.Values[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
            return;

        p.GetComponent<Player>().MovDir = new Vector2(x, y);

        if (GameManager.Instance.Session.IsHost)
        {
            GameManager.Instance.NetworkManager.SendMessage(
                new ProtobufMessage(new GameMessage
                {
                    DoBroadcast = true,
                    Rpc = message
                }, ProtobufMessage.OpCode.Game));
        }
    }

    void ProcessRPCAttack(RPC message)
    {
        if (!Players.TryGetValue(message.PlayerId, out var p))
            return;

        p.GetComponent<Player>().Attack();

        if (GameManager.Instance.Session.IsHost)
        {
            GameManager.Instance.NetworkManager.SendMessage(
                new ProtobufMessage(new GameMessage
                {
                    DoBroadcast = true,
                    Rpc = message
                }, ProtobufMessage.OpCode.Game));
        }
    }

    public void RPC_Attack()
    {
        if (!GameManager.Instance.Session.IsMulti)
            return;

        var g = new GameMessage
        {
            Rpc = new RPC
            {
                PlayerId = playerID,
                RpcName = "Attack",
            },
            DoBroadcast = GameManager.Instance.Session.IsHost
        };

        GameManager.Instance.NetworkManager.SendMessage(
            new ProtobufMessage(g, ProtobufMessage.OpCode.Game));
    }
}
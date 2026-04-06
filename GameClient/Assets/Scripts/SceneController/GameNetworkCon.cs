using NetworkController.Message;
using Protos;
using System;
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
    ConcurrentDictionary<int, GameObject> Players = new();    // key = sessionID;

    public float SyncInterval = 0.1f;   // n sec
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
        sceneController.AddNotice(GameManager.Instance.Session.IsHost ? "This is host" : "This is guest");
        if (!GameManager.Instance.Session.IsHost)
        {
            SendJoinMessage();
        }
    }


    void Update()
    {
        if (!GameManager.Instance.Session.IsMulti)
        {
            return;
        }

        // 이거 비효율적임. 매우
        UpdateScoreboard();
    }

    private void FixedUpdate()
    {
        SendSyncMessage();
    }

    void SendSyncMessage(bool sync = false)
    {
        if (Time.fixedTime - lastSyncTime < SyncInterval && !sync) return;
        lastSyncTime = Time.fixedTime;
        sceneController.AddNotice("[" + DateTime.UtcNow + "] Send Sync");

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

        if (GameManager.Instance.Session.IsHost)
        {
            // 다른 유저들 상태 전파
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

    void SyncGame(GameMessage msg)
    {
        var message = msg.GameSync;

        if (playerID == message.PlayerId)
        {
            return;
        }

        if (Players.TryGetValue(message.PlayerId, out var p))
        {
            p.GetComponent<Player>().Sync(message);
            sceneController.AddNotice("Player: " + message.UserName + " synced");
        }
        else
        {
            var newPlayer = sceneController.SpawnPlayer(message);
            newPlayer.GetComponent<Player>().Sync(message);
            newPlayer.GetComponent<Player>().UserName = message.UserName + "(" + message.PlayerId.ToString() + ")";
            Players.TryAdd(message.PlayerId, newPlayer);
            Debug.Log("New player: " + message.UserName + " created");
            sceneController.AddNotice("New player: " + message.UserName + " created");
        }
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

        int t = Mathf.Min(players.Count, scores.Count);

        for (int i = 0; i < t; i++)
        {
            if (scores[i] == null) continue;

            scores[i].transform.GetChild(0)
                .GetComponent<TMP_Text>().text =
                $"{players[i].UserName}: {players[i].Score}";

            scores[i].SetActive(true);
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
                HandleRPC(message);
                break;
        }
    }

    void HandleRPC(GameMessage message)
    {
        var rpc = message.Rpc;

        switch (rpc.RpcName)
        {
            case "SetPlayerID":
                {
                    playerID = rpc.PlayerId;
                    GameManager.Instance.GamePlayer.GetComponent<Player>().UserName += $"({rpc.PlayerId})";
                }
                Debug.Log("Set Player ID: " + playerID);
                break;
            case "Move":
                ProcessRPCMove(message);
                break;
            case "Attack":
                ProcessRPCAttack(rpc);
                break;
            case "Join":
                ProcessJoin(message);
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
                Values = { x.ToString(), y.ToString() },
            },
            DoBroadcast = GameManager.Instance.Session.IsHost,
        };

        if (GameManager.Instance.Session.IsHost)
            sceneController.AddNotice("Called RPC_Move");

        var m = new ProtobufMessage(g, ProtobufMessage.OpCode.Game);
        GameManager.Instance.NetworkManager.SendMessage(m);
    }

    void ProcessRPCMove(GameMessage message)
    {
        var rpc = message.Rpc;
        if (!Players.TryGetValue(rpc.PlayerId, out var p))
            return;

        if (!float.TryParse(rpc.Values[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x))
            return;

        if (!float.TryParse(rpc.Values[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
            return;

        p.GetComponent<Player>().MovDir = new Vector2(x, y);

        // 호스트 유저면 rpc 전파
        if (GameManager.Instance.Session.IsHost)
        {
            GameManager.Instance.NetworkManager.SendMessage(
                new ProtobufMessage(new GameMessage
                {
                    DoBroadcast = true,
                    Rpc = rpc
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


    void SendJoinMessage()
    {
        var g = new GameMessage
        {
            Rpc = new RPC
            {
                PlayerId = playerID,
                RpcName = "Join",
                Values = { GameManager.Instance.Session.GetUsername(), $"{GameManager.Instance.SelectedCharacterIndex}" },
            }
        };
        sceneController.AddNotice("Join Message sent");

        GameManager.Instance.NetworkManager.SendMessage(
            new ProtobufMessage(g, ProtobufMessage.OpCode.Game));
    }

    void ProcessJoin(GameMessage message)
    {
        var rpc = message.Rpc;

        if (rpc.PlayerId == -1)
        {
            if (!Int32.TryParse(rpc.Values[1], out var prefabId))
            {
                return;
            }

            var newPlayer = sceneController.SpawnPlayer(prefabId, 0, 0);
            newPlayer.GetComponent<Player>().UserName = rpc.Values[0];

            Players.TryAdd(message.SessionID, newPlayer);

            var m = "New player: " + $"{message.SessionID}" + " join to game";
            Debug.Log(m);
            sceneController.AddNotice(m);
            RPC_InitPlayer(message.SessionID);
            if(GameManager.Instance.Session.IsHost)
            {
                SendSyncMessage(true);
            }
        }
    }

    int RPC_InitPlayer(int playerID)
    {
        var g = new GameMessage
        {
            SessionID = playerID,
            DoBroadcast = false,
            Rpc = new RPC
            {
                PlayerId = playerID,
                RpcName = "SetPlayerID",
            }
        };

        Debug.Log(playerID + " New Client try To Join   " + g.Rpc.PlayerId);
        Debug.Log($"Send Set Playr ID RPC to {g.SessionID}");
        var m = new ProtobufMessage(g, ProtobufMessage.OpCode.Game);
        GameManager.Instance.NetworkManager.SendMessage(m);
        return g.Rpc.PlayerId;
    }
}
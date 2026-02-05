using NetworkController.Message;
using NUnit.Framework;
using Protos;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;

public class MultiMenuCon : MonoBehaviour
{
    public TMP_Text Username;
    public Transform Rooms;
    public GameObject RoomPrefab;
    List<GameObject> RoomList = new();

    public TMP_Text RoomName;
    public TMP_Text HostName;
    public GameObject UserInfoPrefab;
    public Transform Players;
    List<GameObject> PlayerList = new();

    RoomButton SelectedRoom = null;

    void Awake()
    {
        GameManager.Instance.NetworkManager.OnMessageRecv += HandleMesssage;
    }

    private void OnDestroy()
    {
        GameManager.Instance.NetworkManager.OnMessageRecv -= HandleMesssage;
    }

    private void Update()
    {
    }

    void Start()
    {
        Username.text = GameManager.Instance.Session.GetUsername();
        GetRoomList();
    }
    public void MainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void CreateGame()
    {
        SceneManager.LoadScene("MultiCreate");
    }

    public void OnReload()
    {
        foreach (GameObject room in RoomList)
        {
            Destroy(room);
        }
        RoomList.Clear();

        GetRoomList();
    }

    public void ShowRoomInfo(string roomName, string hostName)
    {
        foreach (var p in PlayerList)
        {
            Destroy(p);
        }
        PlayerList.Clear();


        foreach (GameObject o in RoomList)
        {
            var room = o.GetComponent<RoomButton>();
            if (room.RoomNameText.text == roomName && room.HostName.text == hostName)
            {
                RoomName.text = room.RoomNameText.text;
                HostName.text = room.HostName.text;
                foreach (var player in room.Players)
                {
                    var p = Instantiate(UserInfoPrefab, Players);
                    p.GetComponent<PlayerInfoUI>().SetName(player);
                    PlayerList.Add(p);
                }

                SelectedRoom = room;
            }
        }
    }

    public void OnJoinRoom()
    {
        if (SelectedRoom == null)
        {
            return;
        }

        Debug.Log("Try To join " + SelectedRoom.RoomNameText.text);
        var r = new RoomMessage
        {
            JoinRoom = new JoinRoom
            {
                RoomName = SelectedRoom.RoomNameText.text,
                OwnerName = SelectedRoom.HostName.text
            }
        };

        var message = new ProtobufMessage(r, ProtobufMessage.OpCode.Room);

        GameManager.Instance.NetworkManager.SendMessage(message);
    }

    public void GetRoomList()
    {
        if (!GameManager.Instance.Session.IsLogin())
        {
            return;
        }

        var roomMsg = new RoomMessage
        {
            RoomList = new RoomList()
        };

        var msg = new ProtobufMessage(roomMsg, ProtobufMessage.OpCode.Room);
        GameManager.Instance.NetworkManager.SendMessage(msg);
        Debug.Log("Request Room List");
    }

    void HandleMesssage(ProtobufMessage message)
    {
        switch ((ProtobufMessage.OpCode)message.Header.OpCode)
        {
            case ProtobufMessage.OpCode.Room:
                HandleRoomMessage(message.Payload as RoomMessage);
                break;
        }
    }

    void HandleRoomMessage(RoomMessage message)
    {
        switch (message.PayloadCase)
        {
            case RoomMessage.PayloadOneofCase.RoomList:
                foreach (var room in message.RoomList.Rooms)
                {
                    string roomName = room.RoomName;
                    string ownerName = room.OwnerName;

                    var players = room.Players.Clone().ToList();

                    var newRoom = Instantiate(RoomPrefab, Rooms);
                    var r = newRoom.GetComponent<RoomButton>();
                    r.Bind(roomName, ownerName, players);

                    var button = newRoom.GetComponent<Button>();
                    button.onClick.AddListener(() =>
                    {
                        ShowRoomInfo(roomName, ownerName);
                    });

                    RoomList.Add(newRoom);
                }
                break;
            case RoomMessage.PayloadOneofCase.JoinRoom:
                Debug.Log("join to " + message.JoinRoom.OwnerName + "'s " + message.JoinRoom.RoomName);
                GameManager.Instance.Session.IsMulti = true;
                SceneManager.LoadScene("CharacterSelect");
                break;
        }
    }
}

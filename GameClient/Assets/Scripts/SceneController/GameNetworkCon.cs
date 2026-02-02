using JetBrains.Annotations;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.UI;
using NetworkController.Message;

public class GameNetworkCon : MonoBehaviour
{
    public GameObject scoreboard;
    public List<GameObject> scores;

    public Object a;

    void Awake()
    {
        GameManager.Instance.NetworkManager.OnMessageRecv += HandleMessage;
    }

    private void OnDestroy()
    {
        GameManager.Instance.NetworkManager.OnMessageRecv -= HandleMessage;
    }

    void Start()
    {
        scoreboard.SetActive(GameManager.Instance.Session.IsMulti());
    }

    void Update()
    {
        if(!GameManager.Instance.Session.IsMulti())
        {
            return;
        }

        UpdateScoreboard();
    }

    void UpdateScoreboard()
    {

    }

    void HandleMessage(ProtobufMessage message)
    {

    }
}

using TMPro;
using UnityEngine;
using System.Collections.Generic;

public class RoomButton : MonoBehaviour
{
    public TMP_Text RoomNameText;
    public TMP_Text HostName;
    public TMP_Text PlayerCountText;
    public List<string> Players;

    public void Bind(string roomName, string hostName, List<string> players)
    {
        RoomNameText.text = roomName;
        HostName.text = hostName;
        PlayerCountText.text = players.Count.ToString();

        Players = players;
    }
}

using GameServer.Service;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameServer
{
    internal class ClientSession
    {
        public UInt32 SessionID;
        public string UserName = "";
        public bool IsAuthenticated => !string.IsNullOrEmpty(UserName);
        public Int64 LastActiveTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        public GameRoom? PlayingRoom = null;

        public ClientSession(UInt32 sessionID)
        {
            SessionID = sessionID;
        }
    }
}

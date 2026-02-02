using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace GameServer.Service
{
    internal class GameService
    {
        Server Owner;


        public GameService(Server owner)
        {
            Owner = owner;
        }
    }
}

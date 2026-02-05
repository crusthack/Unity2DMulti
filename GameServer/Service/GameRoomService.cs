using NetworkController.Message;
using Protos;
using System;
using System.Collections.Concurrent;

namespace GameServer.Service
{
    internal class GameRoomService
    {
        Server Owner;

        ConcurrentDictionary<string, GameRoom> Rooms = new(); // <hostName, room>

        public GameRoomService(Server owner)
        {
            Owner = owner;
            Owner.OnDisconnect += ExitRoom;

            for (int i = 0; i < 13; ++i)
            {
                var dummyHost = new ClientSession((uint)i + 1200000);
                dummyHost.UserName = "host #" + i.ToString();
                var room = new GameRoom($"test{i}", dummyHost);

                Rooms.TryAdd($"host #{i}", room);

                for (int j = 0; j < i; ++j)
                {
                    var dummy = new ClientSession((uint)j + 1000000);
                    dummy.UserName = j.ToString();
                    room.JoinRoom(dummy);
                }
            }
        }

        public void CreateGameRoom(ClientSession session, Protos.CreateRoom message)
        {
            if (!session.IsAuthenticated)
            {
                return;
            }
            Console.WriteLine($"{session.UserName} request create room, name: {message.RoomName}");

            if (Rooms.TryGetValue(session.UserName, out var _) || session.PlayingRoom != null)
            {
                // deny
                return;
            }

            var gameRoom = new GameRoom(message.RoomName, session);

            Rooms.TryAdd(session.UserName, gameRoom);

            var roomMsg = new RoomMessage
            {
                CreateRoom = message
            };
            var pmsg = new ProtobufMessage(roomMsg, ProtobufMessage.OpCode.Room);

            session.PlayingRoom = gameRoom;

            Owner.SendMessage(session, pmsg);
        }

        public void JoinRoom(ClientSession session, Protos.JoinRoom message)
        {
            //if (message.RoomName.StartsWith("test"))
            //{
            //    return;
            //}

            if (session.PlayingRoom != null)
            {
                Console.WriteLine(session.UserName + " session already in game");
                return;
            }

            Console.WriteLine(session.UserName + " try to join " + message.OwnerName + "'s room");
            if (Rooms.TryGetValue(message.OwnerName, out var room))
            {
                room.JoinRoom(session);

                var r = new RoomMessage
                {
                    JoinRoom = message
                };
                session.PlayingRoom = room;

                var m = new ProtobufMessage(r, ProtobufMessage.OpCode.Room);
                Owner.SendMessage(session, m);
            }
        }

        public void GetRoomList(ClientSession session, Protos.RoomList message)
        {
            var roomList = new RoomList();
            foreach (var room in Rooms)
            {
                var info = room.Value;
                var roomInfo = new RoomInfo
                {
                    RoomName = info.RoomName,
                    OwnerName = info.Hostuser.UserName
                };
                foreach (var player in info.Players)
                {
                    roomInfo.Players.Add(player.UserName);
                }
                roomList.Rooms.Add(roomInfo);
            }

            var rmsg = new RoomMessage
            {
                RoomList = roomList
            };

            var msg = new ProtobufMessage(rmsg, ProtobufMessage.OpCode.Room);

            Owner.SendMessage(session, msg);
        }

        void ExitRoom(ClientSession session)
        {
            if(session.PlayingRoom == null)
            {
                return;
            }

            if (session.PlayingRoom.Hostuser == session)
            {
                // 접속해있는 다른 유저들 퇴장 처리
                if (Rooms.TryRemove(session.UserName, out var room))
                {
                    Console.WriteLine($"{session.UserName}'s room removed");
                }
            }

            else
            {
                session.PlayingRoom.Exit(session);
            }
        }
    }

    class GameRoom
    {
        public string RoomName;
        public ClientSession Hostuser;
        public List<ClientSession> Players = new();

        public GameRoom(string roomName, ClientSession host)
        {
            RoomName = roomName;
            Hostuser = host;
        }

        public void JoinRoom(ClientSession session)
        {
            Players.Add(session);
        }

        public void Exit(ClientSession session)
        {
            if(Players.Remove(session))
            {
                //var message = new ProtobufMessage(
                //    new GameMessage
                //    {
                //        Rpc = new 
                //    })

                Console.WriteLine($"{session.UserName}: leave game room {RoomName}");
            }
        }
    }
}

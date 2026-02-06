using Google.Protobuf;
using NetworkController.Message;
using Protos;

namespace GameServer.Service
{
    internal class GameService
    {
        Server Owner;

        public GameService(Server owner)
        {
            Owner = owner;
        }

        public void ProcessSyncMessage(ClientSession session, GameMessage message)
        {
            var gameRoom = session.PlayingRoom;
            if (gameRoom == null)
            {
                return;
            }

            if (gameRoom.Hostuser == session)
            {
                ProcessHostsync(session, message, gameRoom);
            }
            else
            {
                ProcessClientsync(session, message, gameRoom);
            }
        }

        // 호스트 유저
        void ProcessHostsync(ClientSession session, GameMessage message, GameRoom room)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"from {session.SessionID}: {message}");
            Console.ForegroundColor = ConsoleColor.White;

            if (message.DoBroadcast)
            {
                foreach(var player in room.Players)
                {
                    Owner.SendMessage(player, new ProtobufMessage(message, ProtobufMessage.OpCode.Game));
                }
            }
            else
            {
                if (Owner.TryGetSession((uint)message.SessionID, out var s))
                {
                    Owner.SendMessage(s, new ProtobufMessage(message, ProtobufMessage.OpCode.Game));
                }
            }
        }

        // 참가 클라이언트 
        void ProcessClientsync(ClientSession session, GameMessage message, GameRoom room)
        {
            message.SessionID = (int)session.SessionID;
            message.UserName = session.UserName;
            Owner.SendMessage(room.Hostuser, new ProtobufMessage(message, ProtobufMessage.OpCode.Game));
        }


        // rpc 메시지
        public void ProcessRpcMessage(ClientSession session, GameMessage rpcMessage)
        {
            var gameRoom = session.PlayingRoom;
            if (gameRoom == null)
            {
                return;
            }

            if (gameRoom.Hostuser == session)
            {
                ProcessHostRpc(session, rpcMessage, gameRoom);
            }
            else
            {
                ProcessClientRpc(session, rpcMessage, gameRoom);
            }
        }

        void ProcessHostRpc(ClientSession session, GameMessage message, GameRoom room)
        {
            if (message.DoBroadcast)
            {
                foreach (var player in room.Players)
                {
                    Owner.SendMessage(player, new ProtobufMessage(message, ProtobufMessage.OpCode.Game));
                }
            }
            else
            {
                if (Owner.TryGetSession((uint)message.SessionID, out var s))
                {
                    Owner.SendMessage(s, new ProtobufMessage(message, ProtobufMessage.OpCode.Game));
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Send RPC message, from: {session.UserName}. to: {s.UserName}. RPC: {message.Rpc.RpcName}. {message.Rpc.PlayerId}");
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }
        }

        // 참가 클라이언트 
        void ProcessClientRpc(ClientSession session, GameMessage message, GameRoom room)
        {
            message.SessionID = (int)session.SessionID;
            message.UserName = session.UserName;
            Owner.SendMessage(room.Hostuser, new ProtobufMessage(message, ProtobufMessage.OpCode.Game));
        }
    }
}

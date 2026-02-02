using NetworkController.Message;
using Protos;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameServer.MessageProcessor
{
    internal class RoomMessageHandler
    {
        Server Server;
        public RoomMessageHandler(Server server)
        {
            Server = server;
        }
        public void HandleMessage(ClientSession session, ProtobufMessage message)
        {
            if (message.Payload is not RoomMessage msg)
            {
                Console.WriteLine("Invalid ChattingMessage payload");
                return;
            }

            switch (msg!.PayloadCase)
            {
                case RoomMessage.PayloadOneofCase.RoomList:
                    Server.GameRoomService.GetRoomList(session, msg.RoomList);
                    break;
                case RoomMessage.PayloadOneofCase.CreateRoom:
                    Server.GameRoomService.CreateGameRoom(session, msg.CreateRoom);
                    break;
                case RoomMessage.PayloadOneofCase.JoinRoom:
                    Server.GameRoomService.JoinRoom(session, msg.JoinRoom);
                    break;
            }
        }
    }
}

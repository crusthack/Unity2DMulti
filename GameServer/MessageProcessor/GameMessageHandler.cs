using NetworkController.Message;
using Protos;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameServer.MessageProcessor
{
    internal class GameMessageHandler
    {
        Server Server;
        public GameMessageHandler(Server server) 
        {
            Server = server;
        }
        public void HandleMessage(ClientSession session, ProtobufMessage message)
        {
            if (message.Payload is not GameMessage msg)
            {
                Console.WriteLine("Invalid ChattingMessage payload");
                return;
            }

            switch (msg!.PayloadCase)
            {
                case GameMessage.PayloadOneofCase.GameSync:
                    break;

                case GameMessage.PayloadOneofCase.Rpc:
                    break;
            }
        }
    }
}

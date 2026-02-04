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
            if(!session.IsAuthenticated)
            {
                return;
            }
            if (message.Payload is not GameMessage msg)
            {
                Console.WriteLine("Invalid ChattingMessage payload");
                return;
            }

            switch (msg!.PayloadCase)
            {
                case GameMessage.PayloadOneofCase.GameSync:
                    Server.GameService.ProcessSyncMessage(session, msg);
                    break;

                case GameMessage.PayloadOneofCase.Rpc:
                    Server.GameService.ProcessRpcMessage(session, msg);

                    break;
            }
        }
    }
}

using NetworkController.Message;
using Protos;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameServer.MessageProcessor
{
    internal class SystemMessageHandler
    {
        Server Server;

        public SystemMessageHandler(Server server)
        {
            Server = server;
        }

        public void HandlerMessage(ClientSession session, ProtobufMessage message)
        {
            if (message.Payload is not SystemMessage msg)
            {
                Console.WriteLine("Invalid ChattingMessage payload");
                return;
            }

            switch (msg!.PayloadCase)
            {
                case SystemMessage.PayloadOneofCase.LoginRequest:
                    HandleLogin(session, msg.LoginRequest);
                    break;

                case SystemMessage.PayloadOneofCase.Heartbeat:
                    HandleHeartbeat(session, msg.Heartbeat);
                    break;
            }
        }


        void HandleLogin(ClientSession session, LoginRequest loginRequest)
        {
            Server.LoginService.Login(session, loginRequest);
        }

        void HandleHeartbeat(ClientSession session, Heartbeat heatbeat)
        {
            Server.LoginService.Heartbeat(session, heatbeat);
        }
    }
}

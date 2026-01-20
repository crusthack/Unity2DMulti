using NetworkController.Message;
using Protos;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameServer.MessageProcessor
{
    internal class MessageProcessor
    {
        Server Server;
        SystemMessageHandler SystemHandler;
        ChattingMessageHandler ChattingHandler;
        public MessageProcessor(Server server) 
        {
            Server = server;
            SystemHandler = new SystemMessageHandler(server);
            ChattingHandler = new ChattingMessageHandler(server);
        }

        public void HandleMessage(ClientSession session, ProtobufMessage message)
        {
            if (message != null)
            {
                session.LastActiveTime = message.Header.Timestamp;
                switch ((ProtobufMessage.OpCode)message.Header.OpCode)
                {
                    case ProtobufMessage.OpCode.Chatting:
                        ChattingHandler.HandleMessage(session, message);
                        break;
                    case ProtobufMessage.OpCode.System:
                        SystemHandler.HandlerMessage(session, message);
                        break;
                }
            }
        }
    }
}

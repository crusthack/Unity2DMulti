using NetworkController.Message;
using Protos;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameServer.MessageProcessor
{
    internal class ChattingMessageHandler
    {
        Server Owner;
        public ChattingMessageHandler(Server owner)
        {
            Owner = owner;
        }

        public void HandleMessage(ClientSession session, ProtobufMessage message)
        {
            if (message.Payload is not ChattingMessage msg)
            {
                Console.WriteLine("Invalid ChattingMessage payload");
                return;
            }
            if(session.IsAuthenticated == false)
            {
                Console.WriteLine($"Unauthenticated session {session.SessionID} tried to send a chat message");
                return;
            }

            Console.WriteLine(
                $"[{DateTimeOffset.FromUnixTimeMilliseconds(message.Header.Timestamp)
                    .LocalDateTime
                    .ToString("yyyy-MM-dd HH:mm:ss.fff")}] " +
                $"Id={session.SessionID}, Message={msg.Message}"
            );

            Owner.ChattingService.ProcessChatting(session, msg);
        }
    }
}

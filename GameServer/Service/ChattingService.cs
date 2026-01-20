using NetworkController.Message;
using Protos;

namespace GameServer.Service
{
    internal class ChattingService
    {
        Server Owner;
        public ChattingService(Server owner)
        {
            Owner = owner;
        }

        public void ProcessChatting(ClientSession session, ChattingMessage message)
        {
            var targets = Owner.LoginService.GetLoggedInSessionsSnapshot();
            var chat = new ChattingMessage
            {
                Username = session.UserName,
                Message = message.Message
            };
            var msg = new ProtobufMessage(chat, ProtobufMessage.OpCode.Chatting);

            foreach (var target in targets)
            {
                Owner.SendMessage(target, msg);
            }
        }
    }
}

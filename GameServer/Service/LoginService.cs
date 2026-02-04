using NetworkController.Message;
using Protos;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace GameServer.Service
{
    internal class LoginService
    {
        Server Server;
        ConcurrentDictionary<string, ClientSession> LoginedClients = new();

        public LoginService(Server server)
        {
            Server = server;
            Server.OnDisconnect += (session) =>
            {
                Logout(session);
            };
        }

        public bool Login(ClientSession session, LoginRequest requestMessage)
        {
            var username = requestMessage.UserName;

            if (LoginedClients.ContainsKey(username))
            {
                return false;
            }
            if(session.IsAuthenticated)
            {
                return false;
            }
            session.UserName = username;

            Console.WriteLine($"User {username} logged in.");
            var b = LoginedClients.TryAdd(username, session);

            if(b)
            {
                var message = new ProtobufMessage(
                    new SystemMessage
                    {
                        LoginResponse = new LoginResponse
                        {
                            Success = true,
                            UserName = username,
                            Message = "Login Success"
                        }
                    }, ProtobufMessage.OpCode.System);
                Server.SendMessage(session, message);
            }

            return b;
        }

        public void Logout(ClientSession session)
        {
            if (!session.IsAuthenticated)
            {
                return;
            }
            LoginedClients.TryRemove(session.UserName, out var _);
            Console.WriteLine($"User {session.UserName} logged out.");
        }

        public IReadOnlyCollection<ClientSession> GetLoggedInSessionsSnapshot()
        {
            return LoginedClients.Values.ToArray();
        }

        public void Heartbeat(ClientSession session, Heartbeat requestMessage)
        {
            session.LastActiveTime = requestMessage.Timestamp;
        }
    }
}

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
            session.UserName = username;

            Console.WriteLine($"User {username} logged in.");

            return LoginedClients.TryAdd(username, session);
        }

        public void Logout(ClientSession session)
        {
            if (!session.IsAuthenticated)
            {
                return;
            }
            LoginedClients.TryRemove(session.UserName, out var _);
            Console.WriteLine($"User {session.UserName} logged out.");
            session.UserName = string.Empty;
        }

        public IReadOnlyCollection<ClientSession> GetLoggedInSessionsSnapshot()
        {
            return LoginedClients.Values.ToArray();
        }
    }
}

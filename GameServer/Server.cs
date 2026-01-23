using NetworkController.Message;
using NetworkController;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using GameServer.MessageProcessor;
using GameServer.Service;
using Protos;

namespace GameServer
{
    internal class Server
    {
        NetworkController<ProtobufMessage> Netcon;
        Task? ServerTask;
        Task HeartbeatTask;
        int CheckInterval = 5000; // 5sec
        bool IsRunning = false;
        CancellationTokenSource token = new();

        MessageProcessor.MessageProcessor Processor;
        ConcurrentDictionary<UInt32, ClientSession> Clients = new();

        public event Action<ClientSession>? OnConnect;
        public event Action<ClientSession>? OnDisconnect;

        public LoginService LoginService;
        public ChattingService ChattingService;

        public Server()
        {
            Netcon = new NetworkController<ProtobufMessage>();
            Netcon.OnConnect += (context) =>
            {
                OnConnected(context);
            };

            Netcon.OnDisconnect += (context) =>
            {
                OnDisconnected(context);
            };
            Processor = new(this);

            LoginService = new(this);
            ChattingService = new(this);

            HeartbeatTask = new Task(async () =>
            {
                Console.WriteLine("Checking heartbeat");
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(CheckInterval);
                    CheckHeartbeat();
                }
                Console.WriteLine("Heartbeat task terminated");
            });
        }

        public void Start()
        {
            Netcon.OpenServer(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 5000));
            Console.WriteLine($"Server start running, IP: 127.0.0.1, Port: 5000");
            IsRunning = true;
            token = new();
            HeartbeatTask = new Task(async () =>
            {
                Console.WriteLine("Checking heartbeat");
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(CheckInterval);
                    CheckHeartbeat();
                }
                Console.WriteLine("Heartbeat task terminated");
            });
            HeartbeatTask.RunSynchronously();
            ServerTask = Task.Run(() =>
            {
                Console.WriteLine("Server start receiving message");
                try
                {
                    while (!token.Token.IsCancellationRequested)
                    {
                        var message = Netcon.GetMessage(out var context, token.Token);
                        if (TryGetSession(context.SessionID, out var session))
                        {
                            if (session == null)
                            {
                                continue;
                            }
                            Processor.HandleMessage(session, message);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Server task terminaterd");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ServerTask error: {ex}");
                }
                Console.WriteLine("Server work terminated");
            });
        }

        public async Task Stop()
        {
            if (!IsRunning)
            {
                return;
            }

            Netcon.CloseServer();
            IsRunning = false;
            token.Cancel();
            await ServerTask!;
        }

        void OnConnected(SocketContext<ProtobufMessage> context)
        {
            if (Clients.TryGetValue(context.SessionID, out var session))
            {
                Console.WriteLine("Server.OnConnected: ???");
                context.Disconnect();
                return;
            }


            var newSession = new ClientSession(context.SessionID);
            if (!Clients.TryAdd(newSession.SessionID, newSession))
            {
                Console.WriteLine("Server.OnConnected: !!!");
            }

            Console.WriteLine($"New client connected. SessionID:{context.SessionID}, EndPoint: {context.RemoteAddress}:{context.RemotePort}");

            OnConnect?.Invoke(newSession);
        }

        void OnDisconnected(SocketContext<ProtobufMessage> context)
        {
            if (!Clients.TryGetValue(context.SessionID, out var session))
            {
                Console.WriteLine("Server.OnDisconnected: ???");
                context.Disconnect();
                return;
            }

            OnDisconnect?.Invoke(session);

            Clients.TryRemove(context.SessionID, out var sesion);

            Console.WriteLine($"Client disconnected. SessionID:{context.SessionID}, EndPoint: {context.RemoteAddress}:{context.RemotePort}");
        }

        void CheckHeartbeat()
        {
            var copy = Clients.Values.ToArray();
            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            foreach (var c in copy)
            {
                if (c.LastActiveTime + CheckInterval < currentTime)
                {
                    Console.WriteLine($"SessionID: {c.SessionID}{(c.IsAuthenticated ? " " + c.UserName : "")} has no response long time. Disconnect");
                    Netcon.Disconnect(c.SessionID);
                }
            }
        }

        public bool TryGetSession(UInt32 sessionID, out ClientSession? session)
        {
            var ret = Clients.TryGetValue(sessionID, out session);
            return ret;
        }

        public void SendMessage(ClientSession session, ProtobufMessage message)
        {
            Netcon.SendMessageTo(session.SessionID, message);
        }
    }
}

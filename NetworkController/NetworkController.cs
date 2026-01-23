using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using NetworkController.Message;

namespace NetworkController
{
    public class NetworkController<T> where T : BaseMessage
    {
        enum NetworkState { None, Client, Server }
        NetworkState State = NetworkState.None;
        Socket _Socket;
        Int32 BufferSize = 1024 * 10;

        BlockingCollection<Tuple<SocketContext<T>, T>> ReceiveMessageQueue = new BlockingCollection<Tuple<SocketContext<T>, T>>();

        SocketContext<T> ClientContext = null;
        ConcurrentStack<SocketContext<T>> ContextQueue = new ConcurrentStack<SocketContext<T>>();
        ConcurrentDictionary<UInt32, SocketContext<T>> ConnectedContext = new ConcurrentDictionary<UInt32, SocketContext<T>>();

        public event Action<SocketContext<T>> OnConnect;
        public event Action<SocketContext<T>> OnDisconnect;

        UInt32 NextSessionID = 0;

        public NetworkController()
        {
            _Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public NetworkController(Socket socket)
        {
            _Socket = socket;
        }

        public NetworkController(Socket socket, int bufferSize)
        {
            _Socket = socket;
            BufferSize = bufferSize;
        }

        public void SetReceiveBufferSize(int size)
        {
            BufferSize = size;
        }

        public void Connect(IPAddress ip, UInt16 port)
        {
            if (State != NetworkState.None)
            {
                throw new Exception("Connect(): NetworkController is already running.");
            }
            State = NetworkState.Client;
            _Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _Socket.Connect(new IPEndPoint(ip, port));

            ClientContext = GetSocketContext();

            ClientContext.Reset(NextSessionID++, _Socket, ip, port);

            ClientContext.StartReceive();
        }

        public bool IsConnected()
        {
            if (State != NetworkState.Client)
            {
                return false;
            }

            return true;
        }

        SocketContext<T> GetSocketContext()
        {
            if (ContextQueue.TryPop(out var ctx))
            {
                return ctx;
            }

            var c = new SocketContext<T>(BufferSize);
            c.OnReceiveMessage += (sender, message) =>
            {
                ReceiveMessageQueue.Add(new Tuple<SocketContext<T>, T>(sender, message));
            };

            c.OnDisconnect += (sender) =>
            {
                SocketDisconnected(sender);
            };

            return c;
        }

        public void Disconnect()
        {
            if (State != NetworkState.Client)
            {
                throw new Exception("Disconnect(): NetworkController is not connected as a client.");
            }
            if (ClientContext == null)
            {
                throw new Exception("Disconnect(): No client context available.");
            }
            ClientContext.Disconnect();
            State = NetworkState.None;
        }

        public void Disconnect(UInt32 sessionID)
        {
            if (State != NetworkState.Server)
            {
                throw new Exception("Disconnect(sessionID): NetworkController is not running as a server.");
            }

            if (ConnectedContext.TryGetValue(sessionID, out var context))
            {
                context.Disconnect();
            }
            else
            {
                throw new Exception($"Disconnect(sessionID): SessionID {sessionID} not found.");
            }
        }

        public void OpenServer(IPEndPoint endPoint)
        {
            _Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _Socket.Bind(endPoint);
            _Socket.Listen(100);

            var args = new SocketAsyncEventArgs();
            args.Completed += (s, e) =>
            {
                CompletedAccept(s, e);
            };

            State = NetworkState.Server;

            StartAccept(args);
        }

        public void CloseServer()
        {
            if (!State.Equals(NetworkState.Server))
            {
                throw new Exception("CloseServer(): NetworkController is not running as a server.");
            }
            _Socket.Close();

            foreach (var context in ConnectedContext.Values)
            {
                context.Disconnect();
            }

            State = NetworkState.None;
        }

        public T GetMessage(CancellationToken cancellationToken = default(CancellationToken))
        {
            return ReceiveMessageQueue.Take(cancellationToken).Item2;
        }

        public T GetMessage(out SocketContext<T> context, CancellationToken cancellationToken = default(CancellationToken))
        {
            var tuple = ReceiveMessageQueue.Take(cancellationToken);
            context = tuple.Item1;
            return tuple.Item2;
        }

        public bool IsMessageAvailable()
        {
            return !ReceiveMessageQueue.IsCompleted && ReceiveMessageQueue.Count > 0;
        }

        public void SendMessage(T message)
        {
            if (!State.Equals(NetworkState.Client))
            {
                throw new Exception("SendMessage: NetworkController is not connected as a client.");
            }

            if (ClientContext == null)
            {
                throw new Exception("SendMessage: No client context available.");
            }
            ClientContext.SendMessage(message);
        }

        public void SendMessageTo(UInt32 sessionID, T message)
        {
            if (!State.Equals(NetworkState.Server))
            {
                throw new Exception("SendMessageTo: NetworkController is not running as a server.");
            }
            if (ConnectedContext.TryGetValue(sessionID, out var context))
            {
                context.SendMessage(message);
            }
            else
            {
                throw new Exception($"SendMessageTo: SessionID {sessionID} not found.");
            }
        }

        void StartAccept(SocketAsyncEventArgs e)
        {
            e.AcceptSocket = null;
            if (!_Socket.AcceptAsync(e))
            {
                CompletedAccept(this, e);
            }
        }

        void CompletedAccept(object sender, SocketAsyncEventArgs e)
        {
            var clientSocket = e.AcceptSocket;
            if (clientSocket == null)
            {
                return;
            }
            var remoteEndPoint = clientSocket.RemoteEndPoint as IPEndPoint;
            if (remoteEndPoint == null)
            {
                return;
            }
            var context = GetSocketContext();
            while (ConnectedContext.TryGetValue(NextSessionID, out var existingContext))
            {
                NextSessionID++;
            }
            ConnectedContext[NextSessionID] = context;
            context.Reset(NextSessionID++, clientSocket, remoteEndPoint.Address, (UInt16)remoteEndPoint.Port);
            context.StartReceive();

            if (OnConnect != null) OnConnect(context);

            StartAccept(e);
        }

        void SocketDisconnected(SocketContext<T> context)
        {
            ConnectedContext.TryRemove(context.SessionID, out var _);
            OnDisconnect?.Invoke(context);
            ContextQueue.Push(context);

            if (State == NetworkState.Client)
            {
                Disconnect();
            }
        }
    }
}
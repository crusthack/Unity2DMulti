using NetworkController.Message;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NetworkController
{
    public class SocketContext<T> where T : BaseMessage, IMessageParser<T>
    {
        Socket? Socket = null;
        SocketAsyncEventArgs SendArgs;
        SocketAsyncEventArgs RecvArgs;

        public UInt32 SessionID;
        public IPAddress? RemoteAddress = null;
        public UInt16 RemotePort = 0;

        byte[] RecvBuf;
        int RecvBufOffset = 0;
        int RecvBufDataSize = 0;

        byte[] SendBuf;
        int SendBufDataSize = 0;
        bool IsSending;
        bool IsConnected = false;
        ConcurrentQueue<T> SendMessageQueue = new();
        T? MessageForSend;

        public event Action<SocketContext<T>, T>? OnReceiveMessage;
        public event Action<SocketContext<T>>? OnDisconnect;

        public SocketContext(Int32 bufferSize)
        {
            RecvBuf = new byte[bufferSize];
            SendBuf = new byte[bufferSize];
            SendArgs = new();
            RecvArgs = new();

            SendArgs.UserToken = this;
            SendArgs.Completed += (s, e) =>
            {
                this.CompletedSend(e.BytesTransferred);
            };
            RecvArgs.UserToken = this;
            RecvArgs.Completed += (s, e) =>
            {
                this.CompletedReceive(e.BytesTransferred);
            };

            SendArgs.SetBuffer(SendBuf, 0, 0);
            RecvArgs.SetBuffer(RecvBuf, 0, RecvBuf.Length);
        }

        public void Reset(UInt32 sessionID, Socket socket, IPAddress ip, UInt16 port)
        {
            if (Interlocked.CompareExchange(ref IsConnected, true, false) == true)
            {
                throw new Exception("Already Connected");
            }
            SessionID = sessionID;
            Socket = socket;
            RemoteAddress = ip;
            RemotePort = port;

            RecvBufOffset = 0;
            RecvBufDataSize = 0;
            SendBufDataSize = 0;
        }

        public void Disconnect()
        {
            if (Interlocked.CompareExchange(ref IsConnected, false, true) == false)
            {
                return;
            }
            Socket!.Shutdown(SocketShutdown.Both);
            Socket!.Close();
            OnDisconnect?.Invoke(this);
        }

        public void StartReceive()
        {
            RecvArgs.SetBuffer(RecvBuf, RecvBufOffset, RecvBuf.Length - (RecvBufOffset + RecvBufDataSize));

            if (!Socket!.ReceiveAsync(RecvArgs))
            {
                CompletedReceive(RecvArgs.BytesTransferred);
            }
        }

        public void CompletedReceive(int bytesTransferred)
        {
            while (true)
            {
                if (bytesTransferred <= 0)
                {
                    Disconnect();
                    return;
                }

                RecvBufDataSize += bytesTransferred;

                while (true)
                {
                    int parsed = T.Parse(RecvBuf, RecvBufDataSize, out T? message);

                    if (parsed < 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Wrong data received...");
                        Console.ForegroundColor = ConsoleColor.White;

                        Disconnect();
                        return;
                    }

                    if (message == null)
                    {
                        break;
                    }

                    OnReceiveMessage?.Invoke(this, message);

                    RecvBufOffset += parsed;
                    RecvBufDataSize -= parsed;
                }

                // 수신한 데이터가 모두 파싱되어 비어있으면 offset 자동으로 0으로 옮겨줌 
                if (RecvBufDataSize == 0)
                {
                    RecvBufOffset = 0;
                }
                // 버퍼에 메시지 하나를 수신할 공간이 부족하면 현재 유효 데이터를 앞으로 옮기고 오프셋 이동
                else if (RecvBuf.Length - (RecvBufOffset + RecvBufDataSize) < T.GetMaxSize())
                {
                    Console.WriteLine($"{RecvBuf.Length}, {RecvBufOffset}, {RecvBufDataSize}");
                    Buffer.BlockCopy(RecvBuf, RecvBufOffset, RecvBuf, 0, RecvBufDataSize);
                    RecvBufOffset = RecvBufDataSize;
                }

                RecvArgs.SetBuffer(
                    RecvBufOffset + RecvBufDataSize,
                    RecvBuf.Length - (RecvBufOffset + RecvBufDataSize)
                );

                if (Socket!.ReceiveAsync(RecvArgs))
                    return;

                bytesTransferred = RecvArgs.BytesTransferred;
            }
        }

        public void SendMessage(T message)
        {
            SendMessageQueue.Enqueue(message);

            if (Interlocked.CompareExchange(ref IsSending, true, false) == false)
            {
                TrySend();
            }
        }

        void TrySend()
        {
            while (true)
            {
                if (MessageForSend != null && MessageForSend.GetSize() < SendBuf.Length - SendBufDataSize)
                {
                    MessageForSend.Serialize(SendBuf.AsSpan(SendBufDataSize));
                    SendBufDataSize += MessageForSend.GetSize();
                    MessageForSend = null;
                }
                if (MessageForSend == null)
                {
                    while (SendMessageQueue.TryDequeue(out var msg))
                    {
                        int size = msg.GetSize();
                        if (size > SendBuf.Length - SendBufDataSize)
                        {
                            MessageForSend = msg;
                            break;
                        }

                        msg.Serialize(SendBuf.AsSpan(SendBufDataSize));
                        SendBufDataSize += size;
                    }
                }

                if (SendBufDataSize == 0)
                {
                    Interlocked.Exchange(ref IsSending, false);

                    if (!SendMessageQueue.IsEmpty && Interlocked.CompareExchange(ref IsSending, true, false) == false)
                    {
                        continue;
                    }
                    return;
                }

                SendArgs.SetBuffer(SendBuf, 0, SendBufDataSize);

                bool pending = Socket!.SendAsync(SendArgs);
                if (pending)
                {

                    return;
                }

                // 송신이 즉시 완료되면 계속 루프 돌면서 송신 
                SendBufDataSize -= SendArgs.BytesTransferred;

                if (SendBufDataSize > 0)
                {
                    Buffer.BlockCopy(SendBuf, SendArgs.BytesTransferred, SendBuf, 0, SendBufDataSize);
                }
            }
        }

        public void CompletedSend(int bytesTransferred)
        {
            SendBufDataSize -= bytesTransferred;

            if (SendBufDataSize > 0)
            {
                Buffer.BlockCopy(SendBuf, bytesTransferred, SendBuf, 0, SendBufDataSize);
            }

            TrySend();
        }
    }
}

using NetworkController;
using NetworkController.Message;
using System.Net;

namespace GameClientConsole
{
    internal class Program
    {

        static void Main(string[] args)
        {
            Console.InputEncoding = System.Text.Encoding.UTF8;
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            NetworkController<ProtobufMessage> Netcon = new NetworkController<ProtobufMessage>();
            Netcon.OnDisconnect += (context) =>
            {
                Console.WriteLine($"Server disconnected");
            };

            var chat = new Protos.ChattingMessage
            {
                Message = "hello, world!!"
            };

            _ = Task.Run(() =>
            {
                while (true)
                {
                    var message = Netcon.GetMessage();
                    if (message.Payload is Protos.ChattingMessage chatMessage)
                    {
                        Console.WriteLine($"[{chatMessage.Username}]: {chatMessage.Message}");
                    }
                    else if (message.Payload is Protos.SystemMessage)
                    {

                    }
                }
            });

            bool sendHeartbeat = false;
            bool printHeartbeat = false;
            _ = Task.Run(() =>
            {
            while (true)
            {
                Thread.Sleep(1000);
                if (sendHeartbeat)
                {

                        try
                        {
                            Netcon.SendMessage(new ProtobufMessage(new Protos.SystemMessage
                            {
                                Heartbeat = new Protos.Heartbeat
                                {
                                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                                }
                            }
                            , ProtobufMessage.OpCode.System));
                        }
                        catch (Exception ex)
                        {
                            sendHeartbeat = false;
                        }

                        if (printHeartbeat)
                        {
                            Console.WriteLine("Heartbeat message sent.");
                        }
                    }
                }
            });

            PrintCommand();
            while (true)
            {
                var input = Console.ReadLine();
                chat.Message = String.IsNullOrEmpty(input) ? "blank" : input;

                var message = new ProtobufMessage(chat, ProtobufMessage.OpCode.Chatting);
                switch (input)
                {
                    case "test":
                        for (int i = 0; i < 1000; ++i)
                        {
                            Netcon.SendMessage(message);
                            continue;
                        }
                        break;
                    case "quit":
                        sendHeartbeat = false;
                        Netcon.Disconnect();
                        break;
                    case "connect":
                        Netcon.Connect(IPAddress.Parse("127.0.0.1"), 5000);
                        sendHeartbeat = true;
                        break;
                    case "system":
                        Netcon.SendMessage(new ProtobufMessage(new Protos.SystemMessage
                        {
                            Heartbeat = new Protos.Heartbeat
                            {
                                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                            }
                        }
                        , ProtobufMessage.OpCode.System));
                        break;

                    case "login":
                        Console.Write("Enter username: ");
                        var username = Console.ReadLine();
                        Netcon.SendMessage(new ProtobufMessage(new Protos.SystemMessage
                        {
                            LoginRequest = new Protos.LoginRequest
                            {
                                UserName = string.IsNullOrEmpty(username) ? "Guest" : username
                            }
                        },
                        ProtobufMessage.OpCode.System));
                        break;
                    case "l":
                        printHeartbeat = !printHeartbeat;
                        break;
                    case "h":
                        if (sendHeartbeat)
                        {
                            Console.WriteLine("Stop sending heartbeat.");
                            sendHeartbeat = false;
                        }
                        else
                        {
                            Console.WriteLine("Start sending heartbeat.");
                            sendHeartbeat = true;
                        }
                        break;

                    default:
                        if (Netcon.IsConnected())
                        {
                            Netcon.SendMessage(message);
                        }
                        else
                        {
                            PrintCommand();
                        }
                        break;
                }
            }
        }

        static void PrintCommand()
        {
            Console.WriteLine("Client Command List: connect, system, test, login, h, quit");
        }
    }
}

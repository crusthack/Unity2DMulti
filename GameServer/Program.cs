using Google.Protobuf;
using NetworkController;
using NetworkController.Message;
using Protos;
using System;
using System.Net;

namespace GameServer
{
    internal class Program
    {

        static async Task Main(string[] args)
        {
            Console.InputEncoding = System.Text.Encoding.UTF8;
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Server server = new();
            bool running = true;

            PrintCommand();
            while (running)
            {
                var input = Console.ReadLine();
                if (string.IsNullOrEmpty(input))
                {
                    continue;
                }
                input.ToLower();

                switch (input)
                {
                    case "start":
                        server.Start();
                        break;
                    case "stop":
                        await server.Stop();
                        break;
                    case "quit":
                        running = false;
                        break;
                    default:
                        PrintCommand();
                        break;
                }
            }

            await server.Stop();
        }

        static void PrintCommand()
        {
            Console.WriteLine("Server Command List: start, stop, quit");
        }
    }
}

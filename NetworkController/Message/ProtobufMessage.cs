using System;
using System.Collections.Generic;
using Google.Protobuf;
using Protos;

namespace NetworkController.Message
{
    public class ProtobufMessage : BaseMessage
    {
        public enum OpCode : Int32
        {
            System = 1,
            Chatting = 2,
        }

        public ProtobufMessageHeader Header;
        public Google.Protobuf.IMessage Payload;

        public ProtobufMessage(Google.Protobuf.IMessage payload, OpCode opCode)
        {
            Header = new ProtobufMessageHeader(payload.CalculateSize(), (Int32)opCode);
            Payload = payload;
            if (GetSize() > GetMaxSize())
            {
                throw new ArgumentException("Payload size exceeds maximum allowed size.");
            }
        }
        public override Int32 GetSize()
        {
            return ProtobufMessageHeader.HeaderSize + Payload.CalculateSize();
        }
        public new static Int32 GetMaxSize()
        {
            return 1024;
        }

        public override byte[] Serialize()
        {
            var headerSize = ProtobufMessageHeader.HeaderSize;
            var payloadSize = Payload.CalculateSize();
            byte[] buffer = new byte[headerSize + payloadSize];

            Header.Serialize(buffer, 0);
            var payloadBytes = Payload.ToByteArray();
            Buffer.BlockCopy(payloadBytes, 0, buffer, headerSize, payloadSize);

            return buffer;
        }

        public Int32 Serialize(byte[] buffer, int offset)
        {
            var total = GetSize();
            if (buffer.Length < offset + total) throw new ArgumentException("Buffer too small", nameof(buffer));

            Header.Serialize(buffer, offset);
            var payloadBytes = Payload.ToByteArray();
            Buffer.BlockCopy(payloadBytes, 0, buffer, offset + ProtobufMessageHeader.HeaderSize, payloadBytes.Length);

            return total;
        }

        public static int Parse(byte[] data, int size, out ProtobufMessage msg)
        {
            var offset = ProtobufMessageHeader.Parse(data, size, out var header);
            if (offset == -1)
            {
                msg = null;
                return -1;
            }
            if (offset == 0 || header == null)
            {
                msg = null;
                return 0;
            }

            var messageHeader = header as ProtobufMessageHeader;
            if (size < ProtobufMessageHeader.HeaderSize + messageHeader.PayloadSize)
            {
                msg = null;
                return 0;
            }

            var payloadBytes = new byte[messageHeader.PayloadSize];
            Buffer.BlockCopy(data, ProtobufMessageHeader.HeaderSize, payloadBytes, 0, messageHeader.PayloadSize);
            var payload = ProtobufParserRegistry.Parse(messageHeader.OpCode, payloadBytes);
            msg = new ProtobufMessage(payload, (OpCode)messageHeader.OpCode);

            return ProtobufMessageHeader.HeaderSize + messageHeader.PayloadSize;
        }
    }

    public class ProtobufMessageHeader : IMessageHeader
    {
        public static Int32 HeaderSize => 20;
        public Int32 PayloadSize;       // 4
        public Int32 OpCode;            // 4
        private Int64 _TimeStamp;       // 8
        public Int64 Timestamp => _TimeStamp;
        static public Int32 CheckKey = 0x2026; // 4

        public ProtobufMessageHeader(Int32 payloadSize, Int32 opCode)
        {
            PayloadSize = payloadSize;
            OpCode = opCode;
            _TimeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
        ProtobufMessageHeader(Int32 p, Int32 o, Int64 t, Int32 c)
        {
            PayloadSize = p;
            OpCode = o;
            _TimeStamp = t;
            CheckKey = c;
        }
        public int Serialize(byte[] buffer, int offset)
        {
            if (buffer.Length < offset + HeaderSize)
                throw new ArgumentException("Buffer too small", nameof(buffer));

            WriteInt32LE(buffer, offset + 0, PayloadSize);
            WriteInt32LE(buffer, offset + 4, OpCode);
            WriteInt64LE(buffer, offset + 8, _TimeStamp);
            WriteInt32LE(buffer, offset + 16, CheckKey);

            return HeaderSize;
        }
        static public int Parse(byte[] buffer, int size, out IMessageHeader header)
        {
            if (size < HeaderSize)
            {
                header = null;
                return 0;
            }

            int payloadSize = ReadInt32LE(buffer, 0);
            int opCode = ReadInt32LE(buffer, 4);
            long timeStamp = ReadInt64LE(buffer, 8);
            int checkKey = ReadInt32LE(buffer, 16);
            header = new ProtobufMessageHeader(payloadSize, opCode, timeStamp, checkKey);
            if (checkKey != CheckKey)
            {
                return -1;
            }

            return HeaderSize;
        }

        static void WriteInt32LE(byte[] buf, int offset, int value)
        {
            unchecked
            {
                buf[offset + 0] = (byte)(value);
                buf[offset + 1] = (byte)(value >> 8);
                buf[offset + 2] = (byte)(value >> 16);
                buf[offset + 3] = (byte)(value >> 24);
            }
        }

        static void WriteInt64LE(byte[] buf, int offset, long value)
        {
            unchecked
            {
                buf[offset + 0] = (byte)(value);
                buf[offset + 1] = (byte)(value >> 8);
                buf[offset + 2] = (byte)(value >> 16);
                buf[offset + 3] = (byte)(value >> 24);
                buf[offset + 4] = (byte)(value >> 32);
                buf[offset + 5] = (byte)(value >> 40);
                buf[offset + 6] = (byte)(value >> 48);
                buf[offset + 7] = (byte)(value >> 56);
            }
        }

        static int ReadInt32LE(byte[] buf, int offset)
        {
            unchecked
            {
                return (buf[offset + 0]) |
                       (buf[offset + 1] << 8) |
                       (buf[offset + 2] << 16) |
                       (buf[offset + 3] << 24);
            }
        }

        static long ReadInt64LE(byte[] buf, int offset)
        {
            unchecked
            {
                uint lo = (uint)ReadInt32LE(buf, offset);
                uint hi = (uint)ReadInt32LE(buf, offset + 4);
                return (long)((ulong)lo | ((ulong)hi << 32));
            }
        }
    }

    static class ProtobufParserRegistry
    {
        static readonly Dictionary<Int32, Func<byte[], IMessage>> Parsers = new Dictionary<int, Func<byte[], IMessage>>() {
            { (Int32)ProtobufMessage.OpCode.System, (b) => SystemMessage.Parser.ParseFrom(b) },
            { (Int32)ProtobufMessage.OpCode.Chatting, (b) => ChattingMessage.Parser.ParseFrom(b) },
        };

        public static IMessage Parse(Int32 opcode, byte[] payload)
        {
            return Parsers[opcode](payload);
        }
    }
}

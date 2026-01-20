using Google.Protobuf;
using System.Buffers.Binary;
using Protos;

namespace NetworkController.Message
{
    public class ProtobufMessage : BaseMessage, IMessageParser<ProtobufMessage>
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
            byte[] buffer = new byte[headerSize + Payload.CalculateSize()];

            var offset = Header.Serialize(buffer);
            Payload.WriteTo(buffer.AsSpan<byte>(offset, payloadSize));

            return buffer;
        }

        public override Int32 Serialize(Span<byte> buffer)
        {
            Header.Serialize(buffer);
            Payload.WriteTo(buffer.Slice(ProtobufMessageHeader.HeaderSize, Payload.CalculateSize()));

            return GetSize();
        }

        public static int Parse(byte[] data, int size, out ProtobufMessage? msg)
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
            if (size < ProtobufMessageHeader.HeaderSize + messageHeader!.PayloadSize)
            {
                msg = null;
                return 0;
            }

            var payload = ProtobufParserRegistry.Parse(
                messageHeader.OpCode, new ReadOnlySpan<byte>(data, ProtobufMessageHeader.HeaderSize, messageHeader.PayloadSize));
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
        public int Serialize(Span<byte> buffer)
        {
            if (buffer.Length < HeaderSize)
                throw new ArgumentException("Buffer too small", nameof(buffer));

            BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(0, 4), PayloadSize);
            BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(4, 4), OpCode);
            BinaryPrimitives.WriteInt64LittleEndian(buffer.Slice(8, 8), _TimeStamp);
            BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(16, 4), CheckKey);

            return HeaderSize;
        }
        static public int Parse(byte[] buffer, int size, out IMessageHeader? header)
        {
            if (size < HeaderSize)
            {
                header = null;
                return 0;
            }

            BinaryPrimitives.TryReadInt32LittleEndian(new ReadOnlySpan<byte>(buffer, 0, 4), out Int32 payloadSize);
            BinaryPrimitives.TryReadInt32LittleEndian(new ReadOnlySpan<byte>(buffer, 4, 4), out Int32 opCode);
            BinaryPrimitives.TryReadInt64LittleEndian(new ReadOnlySpan<byte>(buffer, 8, 8), out Int64 timeStamp);
            BinaryPrimitives.TryReadInt32LittleEndian(new ReadOnlySpan<byte>(buffer, 16, 4), out Int32 checkKey);
            header = new ProtobufMessageHeader(payloadSize, opCode, timeStamp, checkKey);
            if (checkKey != CheckKey)
            {
                return -1;
            }

            return HeaderSize;
        }
    }

    static class ProtobufParserRegistry
    {
        static readonly Dictionary<Int32, MessageParser> Parsers = new()
        {
            { (Int32)ProtobufMessage.OpCode.System, SystemMessage.Parser},
            { (Int32)ProtobufMessage.OpCode.Chatting, ChattingMessage.Parser },
        };

        public static IMessage Parse(Int32 opcode, ReadOnlySpan<byte> payload)
        {
            return Parsers[opcode].ParseFrom(payload.ToArray());
        }
    }
}

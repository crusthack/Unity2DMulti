using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetworkController.Message
{
    public class BaseMessage : IMessageParser<BaseMessage>
    {
        string Payload;

        public BaseMessage(string payload = "")
        {
            Payload = payload;
        }

        public virtual Int32 GetSize()
        {
            return Encoding.UTF8.GetByteCount(Payload);
        }

        public static Int32 GetMaxSize()
        {
            return 1024;
        }

        public virtual byte[] Serialize()
        {
            return Encoding.UTF8.GetBytes(Payload);
        }
        public virtual Int32 Serialize(Span<byte> buffer)
        {
            if(buffer.Length < Encoding.UTF8.GetByteCount(Payload))
            {
                throw new Exception("Buffer size is smaller than payload size.");
            }

            return Encoding.UTF8.GetBytes(Payload, buffer);
        }

        public static int Parse(byte[] data, Int32 size, out BaseMessage? message)
        {
            string m = Encoding.UTF8.GetString(data, 0, size);
            message = new BaseMessage(m);

            return size;
        }
    }

    public interface IMessageParser<T>
    {
        static abstract int Parse(byte[] data, int size, out T? message);

        static abstract Int32 GetMaxSize();
    }

    public interface IMessageHeader
    {
        static Int32 HeaderSize { get; }
        abstract Int32 Serialize(Span<byte> buffer);
        static abstract int Parse(byte[] buffer, int Size, out IMessageHeader? header);
    }
}

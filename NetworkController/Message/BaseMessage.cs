using System;
using System.Text;

namespace NetworkController.Message
{
    public class BaseMessage
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
        // compatibility helper: serialize into byte[] at offset
        public virtual Int32 Serialize(byte[] buffer, int offset)
        {
            var bytes = Encoding.UTF8.GetBytes(Payload);
            if (buffer.Length < offset + bytes.Length)
            {
                throw new Exception("Buffer size is smaller than payload size.");
            }

            Buffer.BlockCopy(bytes, 0, buffer, offset, bytes.Length);
            return bytes.Length;
        }

        public static int Parse(byte[] data, Int32 size, out BaseMessage message)
        {
            string m = Encoding.UTF8.GetString(data, 0, size);
            message = new BaseMessage(m);

            return size;
        }
    }

    public interface IMessageParser<T>
    {
        int Parse(byte[] data, int size, out T message);
        Int32 GetMaxSize();
    }

    public interface IMessageHeader
    {
        Int32 Serialize(byte[] buffer, int offset);
        // parsing is implemented by concrete headers
    }
}

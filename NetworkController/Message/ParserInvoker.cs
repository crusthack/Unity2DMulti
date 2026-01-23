using System;

namespace NetworkController.Message
{
    // Helper to call static Parse/GetMaxSize on message types compiled for newer C#.
    // Uses reflection to find a static Parse(byte[] data, int size, out T message) method and a static GetMaxSize() method.
    static class ParserInvoker<T>
    {
        delegate int ParseDelegate(byte[] data, int size, out T message);
        static readonly ParseDelegate _parse;
        static readonly Func<int> _getMaxSize;

        static ParserInvoker()
        {
            var t = typeof(T);
            var parseMethod = t.GetMethod("Parse", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (parseMethod == null)
            {
                throw new InvalidOperationException($"Type {t.FullName} does not contain a static Parse method.");
            }

            _parse = (ParseDelegate)Delegate.CreateDelegate(typeof(ParseDelegate), parseMethod);

            var gms = t.GetMethod("GetMaxSize", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (gms == null)
            {
                throw new InvalidOperationException($"Type {t.FullName} does not contain a static GetMaxSize method.");
            }

            _getMaxSize = (Func<int>)Delegate.CreateDelegate(typeof(Func<int>), gms);
        }

        public static int Parse(byte[] data, int size, out T message)
        {
            return _parse(data, size, out message);
        }

        public static int GetMaxSize()
        {
            return _getMaxSize();
        }
    }
}

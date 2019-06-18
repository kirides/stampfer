using System.Collections.Concurrent;

namespace DaedalusLanguageServer
{
    public class BufferManager
    {
        private ConcurrentDictionary<string, char[]> _buffers = new ConcurrentDictionary<string, char[]>();

        public void UpdateBuffer(string documentPath, char[] buffer)
        {
            _buffers.AddOrUpdate(documentPath, buffer, (k, v) => buffer);
        }

        public char[] GetBuffer(string documentPath)
        {
            return _buffers.TryGetValue(documentPath, out var buffer) ? buffer : null;
        }
    }
}



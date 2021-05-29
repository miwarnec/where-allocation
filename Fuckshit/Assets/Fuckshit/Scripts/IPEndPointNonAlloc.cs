using System.Net;
using JetBrains.Annotations;

namespace Fuckshit
{
    public class IPEndPointNonAlloc : IPEndPoint
    {
        public IPEndPointNonAlloc(long address, int port) : base(address, port) {}
        public IPEndPointNonAlloc([NotNull] IPAddress address, int port) : base(address, port) {}
    }
}
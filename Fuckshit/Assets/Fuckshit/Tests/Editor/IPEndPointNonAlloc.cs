using System.Net;
using NUnit.Framework;

namespace Fuckshit.Tests
{
    public class IPEndPointNonAllocTests : UdpTest
    {
        // simply try to create one
        [Test]
        public void NewIPEndPointNonAlloc()
        {
            IPEndPointNonAlloc endPoint = new IPEndPointNonAlloc(IPAddress.Any, 1337);
        }

        [Test]
        public void SendToServer()
        {
        }
    }
}

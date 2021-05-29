using System;
using System.Linq;
using System.Net;
using NUnit.Framework;

namespace Fuckshit.Tests
{
    public class IPEndPointNonAllocTests : UdpTest
    {
        byte[] message = {0xAA, 0xBB};

        // simply try to create one
        [Test]
        public void New()
        {
            IPEndPointNonAlloc _ = new IPEndPointNonAlloc(IPAddress.Any, 1337);
        }

        [Test]
        public void ReceiveFrom()
        {
            // send a message to server
            ClientSend(message);

            // poll with IPEndPointNonAlloc
            EndPoint newClientEP = new IPEndPointNonAlloc(IPAddress.Any, 0);
            bool result = ServerPoll(ref newClientEP, out ArraySegment<byte> received);
            Assert.That(result, Is.True);
            Assert.That(received.SequenceEqual(message));
        }
    }
}

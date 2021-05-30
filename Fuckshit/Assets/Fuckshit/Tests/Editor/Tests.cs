using System;
using System.Linq;
using System.Net;
using NUnit.Framework;

namespace Fuckshit.Tests
{
    public class Tests : UdpTest
    {
        byte[] message = {0xAA, 0xBB};
        byte[] message2 = {0xCC, 0xDD};

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

        // send two messages to server
        [Test]
        public void ReceiveFromMultiple()
        {
            // send a message to server
            ClientSend(message);

            // poll with IPEndPointNonAlloc
            EndPoint newClientEP = new IPEndPointNonAlloc(IPAddress.Any, 0);
            bool result = ServerPoll(ref newClientEP, out ArraySegment<byte> received);
            Assert.That(result, Is.True);
            Assert.That(received.SequenceEqual(message));


            // send two messages to server
            ClientSend(message2);

            // poll with IPEndPointNonAlloc
            newClientEP = new IPEndPointNonAlloc(IPAddress.Any, 0);
            result = ServerPoll(ref newClientEP, out received);
            Assert.That(result, Is.True);
            Assert.That(received.SequenceEqual(message2));
        }

        // need to guarantee that ReceiveFrom never overwrites the original
        // cached serialization
        [Test]
        public void ReceiveFromNeverChangesCachedSerialization()
        {
            EndPoint newClientEP = new IPEndPointNonAlloc(IPAddress.Any, 0);

            // get original hash
            int originalHash = ((IPEndPointNonAlloc)newClientEP).serialized.GetHashCode();

            // do it twice, just to be sure
            for (int i = 0; i < 2; ++i)
            {
                // send a message to server
                ClientSend(message);

                // poll with IPEndPointNonAlloc
                bool result = ServerPoll(ref newClientEP, out ArraySegment<byte> _);
                Assert.That(result, Is.True);
            }

            // check hash again
            int hash = ((IPEndPointNonAlloc)newClientEP).serialized.GetHashCode();
            Assert.That(hash, Is.EqualTo(originalHash));
        }
    }
}

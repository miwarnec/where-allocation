using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
            bool result = ServerPoll(out ArraySegment<byte> received);
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
            bool result = ServerPoll(out ArraySegment<byte> received);
            Assert.That(result, Is.True);
            Assert.That(received.SequenceEqual(message));


            // send two messages to server
            ClientSend(message2);

            // poll with IPEndPointNonAlloc
            result = ServerPoll(out received);
            Assert.That(result, Is.True);
            Assert.That(received.SequenceEqual(message2));
        }

        // see IPEndPointNonAlloc.Create:
        // we guarantee to throw an Exception if ReceiveFrom() is ever changed
        // to not pass the expected Serialize() SocketAddress back to Create()
        [Test, Ignore("Unity 2019 LTS mono version still creates a new one each time.")]
        public void CreateCatchesUnknownSocketAddressObject()
        {
            SocketAddress random = new SocketAddress(AddressFamily.InterNetwork);

            Assert.Throws<Exception>(() => {
                reusableReceiveEP.Create(random);
            });
        }

        // see IPEndPointNonAlloc.Create:
        // we always need to return _something_ != null so that ReceiveFrom can
        // set seed_endpoint.
        [Test]
        public void CreateReturnsSelf()
        {
            EndPoint created = reusableReceiveEP.Create(reusableReceiveEP.temp);
            Assert.That(created, Is.EqualTo(reusableReceiveEP));
        }
    }
}

using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using NUnit.Framework;
using Fuckshit.Examples;

namespace Fuckshit.Tests
{
    public class Tests : UdpTest
    {
        byte[] message = {0xAA, 0xBB};
        byte[] message2 = {0xCC, 0xDD};

        [SetUp]
        public void SetUp()
        {
            Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            Shutdown();
        }

        // simply try to create one
        [Test]
        public void New()
        {
            IPEndPointNonAlloc _ = new IPEndPointNonAlloc(IPAddress.Any, 1337);
        }

        [Test]
        public void ClientToServer()
        {
            // send a message to server
            ClientSend(message);

            // poll with IPEndPointNonAlloc
            bool result = ServerPoll(out int _, out ArraySegment<byte> received);
            Assert.That(result, Is.True);
            Assert.That(received.SequenceEqual(message));
        }

        // send two messages to server
        [Test]
        public void ClientToServer_Multiple()
        {
            // send a message to server
            ClientSend(message);

            // poll with IPEndPointNonAlloc
            bool result = ServerPoll(out int _, out ArraySegment<byte> received);
            Assert.That(result, Is.True);
            Assert.That(received.SequenceEqual(message));


            // send two messages to server
            ClientSend(message2);

            // poll with IPEndPointNonAlloc
            result = ServerPoll(out int _, out received);
            Assert.That(result, Is.True);
            Assert.That(received.SequenceEqual(message2));
        }

        // TODO flaky. only works when running by itself. not after the others.
        [Test]
        public void ServerToClient()
        {
            // send a message to server
            ServerSend(message);

            // poll with IPEndPointNonAlloc
            bool result = ClientPoll(out ArraySegment<byte> received);
            Assert.That(result, Is.True);
            Assert.That(received.SequenceEqual(message));
        }

        // see IPEndPointNonAlloc.Create:
        // we guarantee to throw an Exception if ReceiveFrom() is ever changed
        // to not pass the expected Serialize() SocketAddress back to Create()
        [Test, Ignore("Unity 2019 LTS mono version still creates a new one each time.")]
        public void CreateCatchesUnknownSocketAddressObject()
        {
            SocketAddress random = new SocketAddress(AddressFamily.InterNetwork);

            Assert.Throws<Exception>(() => {
                serverReusableReceiveEP.Create(random);
            });
        }

        // see IPEndPointNonAlloc.Create:
        // we always need to return _something_ != null so that ReceiveFrom can
        // set seed_endpoint.
        [Test]
        public void CreateReturnsSelf()
        {
            EndPoint created = serverReusableReceiveEP.Create(serverReusableReceiveEP.temp);
            Assert.That(created, Is.EqualTo(serverReusableReceiveEP));
        }
    }
}

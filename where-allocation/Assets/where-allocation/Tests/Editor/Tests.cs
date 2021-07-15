using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using NUnit.Framework;
using WhereAllocation.Examples;

namespace WhereAllocation.Tests
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
            bool result = ServerPoll(out ArraySegment<byte> received);
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

        [Test, Ignore("Flaky. Only works when running separately. Not with all the others.")]
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

        // make sure that GetHashCode works for our custom class.
        // it's supposed to use temp.GetHashCode.
        [Test]
        public void GetHashCodeTest_UsesTempSocketAddress()
        {
            IPAddress address = IPAddress.Parse("127.0.0.1");
            IPEndPointNonAlloc endPoint = new IPEndPointNonAlloc(address, 1337);

            // Unity mono / netcore hashcodes are slightly different
#if UNITY_2018_3_OR_NEWER
            Assert.That(endPoint.GetHashCode(), Is.EqualTo(939851901));
#else
            Assert.That(endPoint.GetHashCode(), Is.EqualTo(939852415));
#endif
        }

        // make sure that GetHashCode works for our custom class.
        // using .temp.GetHashCode() should work too.
        [Test]
        public void GetHashCodeTest_temp()
        {
            IPAddress address = IPAddress.Parse("127.0.0.1");
            IPEndPointNonAlloc endPoint = new IPEndPointNonAlloc(address, 1337);

            // Unity mono / netcore hashcodes are slightly different
#if UNITY_2018_3_OR_NEWER
            Assert.That(endPoint.temp.GetHashCode(), Is.EqualTo(939851901));
#else
            Assert.That(endPoint.temp.GetHashCode(), Is.EqualTo(939852415));
#endif
        }

        // by default, SocketAddress.GetHashCode() returns 0 after usage in
        // ReceiveFrom_NonAlloc because m_changed is false and so HashCode is
        // never calculated.
        [Test]
        public void GetHashCodeTest_AfterReceiveFrom()
        {
            // send something and then poll once
            ClientSend(message);
            bool polled = ServerPoll(out ArraySegment<byte> _);
            Assert.That(polled, Is.True);

            Assert.That(serverReusableReceiveEP.temp.GetHashCode(), !Is.EqualTo(0));
        }

        // need a way to create a real, valid IPEndPoint from our NonAlloc class
        [Test]
        public void DeepCopyIPEndPoint()
        {
            // create a IPEndPointNonAlloc with unique values
            IPEndPointNonAlloc unique = new IPEndPointNonAlloc(IPAddress.Parse("1.2.3.4"), 42);

            // deep copy
            IPEndPoint deepCopy = unique.DeepCopyIPEndPoint();

            // result should be the same
            Assert.That(deepCopy.ToString(), Is.EqualTo("1.2.3.4:42"));
            Assert.That(unique.Equals(deepCopy), Is.True);

            // check if the endpoint's SocketAddress is as expected
            int beforeHash = unique.temp.GetHashCode();
            int afterHash = deepCopy.Serialize().GetHashCode();
            Assert.That(beforeHash, Is.EqualTo(afterHash));
        }
    }
}

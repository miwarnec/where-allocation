// helper class with udp connection setup

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NUnit.Framework;
using UnityEngine;

namespace Fuckshit.Tests
{
    public abstract class UdpTest
    {
        public int Port = 1337;

        // server
        public Socket serverSocket;
        public IPEndPointNonAlloc serverReusableReceiveEp;
        public IPEndPointNonAlloc reusableSendEP; // true copy of the connected client's EP

        // client
        public Socket clientSocket;
        public IPEndPoint clientRemoteEndPoint;
        public IPEndPointNonAlloc clientReusableReceiveEP;

        [SetUp]
        public void SetUp()
        {
            // create server
            serverReusableReceiveEp = new IPEndPointNonAlloc(IPAddress.Any, 0);
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, Port));

            // create client
            clientRemoteEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), Port);
            clientReusableReceiveEP = new IPEndPointNonAlloc(IPAddress.Any, 0);
            clientSocket = new Socket(clientRemoteEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            clientSocket.Connect(clientRemoteEndPoint);
            Thread.Sleep(100);

            // send hello
            ClientSend(new byte[]{0x12, 0x34});

            // server should have something to poll now
            bool result = ServerPoll(out ArraySegment<byte> _);
            Assert.That(result, Is.True);
        }

        [TearDown]
        public void TearDown()
        {
            serverSocket.Close();
            clientSocket.Close();
        }

        public void ClientSend(byte[] data)
        {
            // send and wait a little bit for it to be delivered
            clientSocket.Send(data, data.Length, SocketFlags.None);
            Thread.Sleep(100);
        }

        public void ServerSend(byte[] data)
        {
            // send and wait a little bit for it to be delivered
            // NOTE: this does not allocate because it doesn't have the
            //       IPEndPoint as last parameter, unlike ServerSend.

            // which EP to use?
            // IPEndPointNonAlloc caches Serializes just fine.
            // just need to use an actual one with the correct SocketAddress etc.
            serverSocket.SendTo_NonAlloc(data, 0, data.Length, SocketFlags.None, reusableSendEP);
            Thread.Sleep(100);
        }

        public bool ClientPoll(out ArraySegment<byte> message)
        {
            byte[] receiveBuffer = new byte[1200];

            if (clientSocket != null && clientSocket.Poll(0, SelectMode.SelectRead))
            {
                int msgLength = clientSocket.ReceiveFrom_NonAlloc(receiveBuffer, clientReusableReceiveEP);
                message = new ArraySegment<byte>(receiveBuffer, 0, msgLength);
                return msgLength > 0;
            }
            message = default;
            return false;
        }

        public bool ServerPoll(out ArraySegment<byte> message)
        {
            byte[] receiveBuffer = new byte[1200];
            if (serverSocket != null && serverSocket.Poll(0, SelectMode.SelectRead))
            {
                // get message
                int msgLength = serverSocket.ReceiveFrom_NonAlloc(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, serverReusableReceiveEp);
                Debug.Log($"ServerPoll from {serverReusableReceiveEp}:  {BitConverter.ToString(receiveBuffer, 0, msgLength)}");

                SocketAddress remoteAddress = serverReusableReceiveEp.temp;

                // new connection?
                if (reusableSendEP == null)
                {
                    // create a copy to remember the client EP for sending to it

                    // allocate a placeholder IPAddress to copy
                    // our SocketAddress into.
                    // -> needs to be the same address family.
                    IPAddress ipAddress;
                    if (remoteAddress.Family == AddressFamily.InterNetworkV6)
                        ipAddress = IPAddress.IPv6Any;
                    else if (remoteAddress.Family == AddressFamily.InterNetwork)
                        ipAddress = IPAddress.Any;
                    else
                        throw new Exception($"Unexpected SocketAddress family: {remoteAddress.Family}");

                    // allocate a playerholder IPEndPoint.
                    // with the needed size form IPAddress.
                    IPEndPoint placeholder = new IPEndPoint(ipAddress, 0);

                    // create an actual copy from RemoteAddress via .Create
                    IPEndPoint actualCopy = (IPEndPoint)placeholder.Create(remoteAddress);

                    // Serialize to create an actual copy of SocketAddress
                    SocketAddress addressCopy = actualCopy.Serialize();

                    // create an empty IPEndPointNonAlloc with correct address family
                    reusableSendEP = new IPEndPointNonAlloc(ipAddress, 0);

                    // set .temp which is returned by Serialize()
                    reusableSendEP.temp = addressCopy;

                    // IMPORTANT: newClientEP doesn't actually have the SocketAddress.
                    //            only it's .temp has the correct SocketAddress.
                }

                message = new ArraySegment<byte>(receiveBuffer, 0, msgLength);
                return msgLength > 0;
            }
            return false;
        }
    }
}
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace WhereAllocation.Examples
{
    public class UdpTest
    {
        public int Port = 1337;

        // server
        public Socket serverSocket;
        public IPEndPointNonAlloc serverReusableReceiveEP; // for reading only
        public IPEndPoint newClientEP; // the actual new client's end point
        public IPEndPointNonAlloc serverReusableSendEP; // true copy of the connected client's EP
        byte[] receiveBuffer;

        // client
        public IPEndPoint clientRemoteEndPoint;
        public Socket clientSocket;
        public IPEndPointNonAlloc clientReusableReceiveEP;

        public void Initialize()
        {
            // create buffer
            receiveBuffer = new byte[1200];

            // create server
            serverReusableReceiveEP = new IPEndPointNonAlloc(IPAddress.Any, 0);
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, Port));
            newClientEP = null;

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
            if (!result)
                Debug.LogError($"NOT CONNECTED!");
        }

        public void Shutdown()
        {
            serverSocket.Close();
            clientSocket.Close();
        }

        public void ClientSend(byte[] data)
        {
            // send and wait a little bit for it to be delivered
            // NOTE: this does not allocate because it doesn't have the
            //       IPEndPoint as last parameter, unlike ServerSend.
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
            serverSocket.SendTo_NonAlloc(data, 0, data.Length, SocketFlags.None, serverReusableSendEP);
            Thread.Sleep(100);
        }

        public bool ClientPoll(out ArraySegment<byte> message)
        {
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
            if (serverSocket != null && serverSocket.Poll(0, SelectMode.SelectRead))
            {
                // alloc
                //int msgLength = serverSocket.ReceiveFrom(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, ref newClientEP);
                //fromHash = newClientEP.GetHashCode();

                // nonalloc
                int msgLength = serverSocket.ReceiveFrom_NonAlloc(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, serverReusableReceiveEP);

                // new connection?
                if (newClientEP == null)
                {
                    // IPEndPointNonAlloc is reused all the time.
                    // we can't store that as the connection's endpoint.
                    // we need a new copy!
                    newClientEP = serverReusableReceiveEP.DeepCopyIPEndPoint();

                    // for allocation free sending, we also need another
                    // IPEndPointNonAlloc...
                    serverReusableSendEP = new IPEndPointNonAlloc(newClientEP.Address, newClientEP.Port);
                }

                // kcp needs the hashcode from the result too.
                // which allocates. so let's test it as well.
                message = new ArraySegment<byte>(receiveBuffer, 0, msgLength);
                return msgLength > 0;
            }
            message = default;
            return false;
        }
    }
}
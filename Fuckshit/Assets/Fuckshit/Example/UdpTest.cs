using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace Fuckshit.Examples
{
    public class UdpTest : MonoBehaviour
    {
        public int Port = 1337;

        // server
        public Socket serverSocket;
        IPEndPointNonAlloc newClientEP = new IPEndPointNonAlloc(IPAddress.Any, 0);
        byte[] receiveBuffer = new byte[1200];

        // client
        public IPEndPoint clientRemoteEndPoint;
        public Socket clientSocket;

        // send per UPDATE (easier to measure in profiler than per FixedUpdate)
        public int SendPerUpdate = 1;
        byte[] message = {0x01, 0x02, 0x03, 0x04};

        void Start()
        {
            // create server
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, Port));

            // create client
            clientRemoteEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), Port);
            clientSocket = new Socket(clientRemoteEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            clientSocket.Connect(clientRemoteEndPoint);
            Thread.Sleep(100);

            // send hello
            ClientSend(new byte[]{0x12, 0x34});

            // server should have something to poll now
            bool result = ServerPoll(out int _, out ArraySegment<byte> _);
            if (!result)
                Debug.LogError($"NOT CONNECTED!");
        }

        public void ClientSend(byte[] data)
        {
            // send and wait a little bit for it to be delivered
            clientSocket.Send(data, data.Length, SocketFlags.None);
            Thread.Sleep(100);
        }

        public bool ServerPoll(out int fromHash, out ArraySegment<byte> message)
        {
            if (serverSocket != null && serverSocket.Poll(0, SelectMode.SelectRead))
            {
                // alloc
                //int msgLength = serverSocket.ReceiveFrom(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, ref newClientEP);
                //fromHash = newClientEP.GetHashCode();

                // nonalloc
                int msgLength = serverSocket.ReceiveFrom_NonAlloc(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, newClientEP);
                // SocketAddress.GetHashCode hashes port + address without
                // allocations:
                // https://github.com/mono/mono/blob/bdd772531d379b4e78593587d15113c37edd4a64/mcs/class/referencesource/System/net/System/Net/SocketAddress.cs#L262
                SocketAddress remoteAddress = newClientEP.temp;
                fromHash = remoteAddress.GetHashCode();

                // kcp needs the hashcode from the result too.
                // which allocates. so let's test it as well.
                message = new ArraySegment<byte>(receiveBuffer, 0, msgLength);
                return msgLength > 0;
            }
            fromHash = 0;
            message = default;
            return false;
        }

        public void Update()
        {
            for (int i = 0; i < SendPerUpdate; ++i)
            {
                ClientSend(message);
                ServerPoll(out int _, out ArraySegment<byte> _);
            }
        }

        /* no GUI to avoid allocations for easier profiling
        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(5, 5, 150, 400));
            GUILayout.Label("Client:");
            if (GUILayout.Button("Connect 127.0.0.1"))
            {
                client.Connect("127.0.0.1", Port, true, 10);
            }
            if (GUILayout.Button("Send 0x01, 0x02 reliable"))
            {
                client.Send(new ArraySegment<byte>(new byte[]{0x01, 0x02}), KcpChannel.Reliable);
            }
            if (GUILayout.Button("Send 0x03, 0x04 unreliable"))
            {
                client.Send(new ArraySegment<byte>(new byte[]{0x03, 0x04}), KcpChannel.Unreliable);
            }
            if (GUILayout.Button("Disconnect"))
            {
                client.Disconnect();
            }
            GUILayout.EndArea();
        }
        */
    }
}
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UnityEngine.Assertions;

namespace Fuckshit.Examples
{
    public class UdpTest : MonoBehaviour
    {
        public int Port = 1337;

        // server
        public Socket serverSocket;
        EndPoint newClientEP = new IPEndPoint(IPAddress.Any, 0);
        byte[] receiveBuffer = new byte[1200];

        // client
        public IPEndPoint clientRemoteEndPoint;
        public Socket clientSocket;

        // send per UPDATE (easier to measure in profiler than per FixedUpdate)
        public int SendPerUpdate = 1;
        byte[] message = new byte[]{0x01, 0x02, 0x03, 0x04};

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
            bool result = ServerPoll();
            if (!result)
                Debug.LogError($"NOT CONNECTED!");
        }

        public void ClientSend(byte[] data)
        {
            // send and wait a little bit for it to be delivered
            clientSocket.Send(data, data.Length, SocketFlags.None);
            Thread.Sleep(100);
        }

        public bool ServerPoll()
        {
            if (serverSocket != null && serverSocket.Poll(0, SelectMode.SelectRead))
            {
                // get message
                int msgLength = serverSocket.ReceiveFrom_NonAlloc(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, out SocketAddress remoteAddress);
                Debug.Log($"ServerPoll from {newClientEP}:  {BitConverter.ToString(receiveBuffer, 0, msgLength)}");
                //message = new ArraySegment<byte>(receiveBuffer, 0, msgLength);
                // convert SocketAddress to EndPoint again, just for tests
                //newClientEP = newClientEP.Create(remoteAddress);
                return true;
            }
            return false;
        }

        public void Update()
        {
            for (int i = 0; i < SendPerUpdate; ++i)
            {
                ClientSend(message);
                ServerPoll();
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
// uses UdpTest class in a component
using System;
using UnityEngine;

namespace Fuckshit.Examples
{
    public class UdpTestComponent : MonoBehaviour
    {
        public UdpTest udp = new UdpTest();

        // send per UPDATE (easier to measure in profiler than per FixedUpdate)
        public int SendPerUpdate = 1;
        byte[] message = {0x01, 0x02, 0x03, 0x04};

        void Start() => udp.Initialize();

        public void Update()
        {
            for (int i = 0; i < SendPerUpdate; ++i)
            {
                udp.ClientSend(message);
                udp.ServerPoll(out int _, out ArraySegment<byte> _);

                udp.ServerSend(message);
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
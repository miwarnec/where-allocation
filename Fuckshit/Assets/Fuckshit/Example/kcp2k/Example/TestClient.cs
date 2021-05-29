using System;
using UnityEngine;

namespace kcp2k.Examples
{
    public class TestClient : MonoBehaviour
    {
        // configuration
        public ushort Port = 7777;

        public int SendPerTick = 100;
        byte[] message = new byte[]{0x01, 0x02, 0x03, 0x04};

        // client
        public KcpClient client = new KcpClient(
            () => {},
            (message) => {}, //Debug.Log($"KCP: OnClientDataReceived({BitConverter.ToString(message.Array, message.Offset, message.Count)})"),
            () => {}
        );

        // MonoBehaviour ///////////////////////////////////////////////////////
        void Awake()
        {
            // logging
            Log.Info = Debug.Log;
            Log.Warning = Debug.LogWarning;
            Log.Error = Debug.LogError;
        }

        void Start()
        {
            // connect client in Start(). server was started in Awake().
            client.Connect("127.0.0.1", Port, true, 10);
        }

        public void LateUpdate() => client.Tick();

        public void FixedUpdate()
        {
            if (client.connected)
            {
                for (int i = 0; i < SendPerTick; ++i)
                {
                    client.Send(new ArraySegment<byte>(message), KcpChannel.Unreliable);
                }
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
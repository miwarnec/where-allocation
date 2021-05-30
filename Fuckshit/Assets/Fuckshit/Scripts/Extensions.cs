using System.Net;
using System.Net.Sockets;

namespace Fuckshit
{
    public static class Extensions
    {
        // always pass the same IPEndPointNonAlloc instead of allocating a new
        // one each time.
        //
        // use IPEndPointNonAlloc.temp to get the latest SocketAdddress written
        // by ReceiveFrom_Internal!
        //
        // IMPORTANT: .temp will be overwritten in next call!
        //            hash or manually copy it if you need to store it, e.g.
        //            when adding a new connection.
        public static int ReceiveFrom_NonAlloc(
            this Socket socket,
            byte[] buffer,
            int offset,
            int size,
            SocketFlags socketFlags,
            IPEndPointNonAlloc remoteEndPoint)
        {
            // call ReceiveFrom with IPEndPointNonAlloc.
            // need to wrap this in ReceiveFrom_NonAlloc because it's not
            // obvious that IPEndPointNonAlloc.Create does NOT create a new
            // IPEndPoint. it saves the result in IPEndPointNonAlloc.temp!
            EndPoint casted = remoteEndPoint;
            int received = socket.ReceiveFrom(buffer, offset, size, socketFlags, ref casted);
            return received;
        }

        // same as above, different parameters
        public static int ReceiveFrom_NonAlloc(this Socket socket, byte[] buffer, IPEndPointNonAlloc remoteEndPoint)
        {
            EndPoint casted = remoteEndPoint;
            int received = socket.ReceiveFrom(buffer, ref casted);
            return received;
        }
    }
}
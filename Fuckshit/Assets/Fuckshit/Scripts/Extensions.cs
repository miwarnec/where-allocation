using System;
using System.Net;
using System.Net.Sockets;

namespace Fuckshit
{
    public static class Extensions
    {
        // TODO pass this as parameter?
        [ThreadStatic] static EndPoint endPointNonAlloc;

        // returns SocketAddress instead of EndPoint.
        // -> can still create an EndPoint from it when first adding / storing
        //    the connection
        // -> if already added, simply identify it with remoteAddress instead.
        //
        // IMPORTANT: remoteAddress will be overwritten in next call!
        //            hash or manually copy it if you need to store it!
        public static int ReceiveFrom_NonAlloc(
            this Socket socket,
            byte[] buffer,
            int offset,
            int size,
            SocketFlags socketFlags,
            out SocketAddress remoteAddress)
        {
            // create IPEndPointNonAlloc helper only once
            if (endPointNonAlloc == null)
                endPointNonAlloc = new IPEndPointNonAlloc(IPAddress.Any, 0);

            // call ReceiveFrom with IPEndPointNonAlloc.
            // need to wrap this in ReceiveFrom_NonAlloc because it's not
            // obvious that IPEndPointNonAlloc.Create does NOT create a new
            // IPEndPoint. it saves the result in IPEndPointNonAlloc.temp!
            int received = socket.ReceiveFrom(buffer, offset, size, socketFlags, ref endPointNonAlloc);
            remoteAddress = ((IPEndPointNonAlloc)endPointNonAlloc).temp;
            return received;
        }
    }
}
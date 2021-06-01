using System;
using System.Net;
using System.Net.Sockets;

namespace WhereAllocation
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

            // SocketAddress.GetHashCode() depends on SocketAddress.m_changed.
            // ReceiveFrom only sets the buffer, it does not seem to set m_changed.
            // we need to reset m_changed for two reasons:
            // * if m_changed is false, GetHashCode() returns the cahced m_hash
            //   which is '0'. that would be a problem.
            //   https://github.com/mono/mono/blob/bdd772531d379b4e78593587d15113c37edd4a64/mcs/class/referencesource/System/net/System/Net/SocketAddress.cs#L262
            // * if we have a cached m_hash, but ReceiveFrom modified the buffer
            //   then the GetHashCode() should change too. so we need to reset
            //   either way.
            //
            // the only way to do that is by _actually_ modifying the buffer:
            // https://github.com/mono/mono/blob/bdd772531d379b4e78593587d15113c37edd4a64/mcs/class/referencesource/System/net/System/Net/SocketAddress.cs#L99
            // so let's do that.
            // -> unchecked in case it's byte.Max
            unchecked
            {
                remoteEndPoint.temp[0] += 1;
                remoteEndPoint.temp[0] -= 1;
            }

            // make sure this worked.
            // at least throw an Exception to make it obvious if the trick does
            // not work anymore, in case ReceiveFrom is ever changed.
            if (remoteEndPoint.temp.GetHashCode() == 0)
                throw new Exception($"SocketAddress GetHashCode() is 0 after ReceiveFrom. Does the m_changed trick not work anymore?");

            return received;
        }

        // same as above, different parameters
        public static int ReceiveFrom_NonAlloc(this Socket socket, byte[] buffer, IPEndPointNonAlloc remoteEndPoint)
        {
            EndPoint casted = remoteEndPoint;
            return socket.ReceiveFrom(buffer, ref casted);
        }

        // SendTo allocates too:
        // https://github.com/mono/mono/blob/f74eed4b09790a0929889ad7fc2cf96c9b6e3757/mcs/class/System/System.Net.Sockets/Socket.cs#L2240
        // -> the allocation is in EndPoint.Serialize()
        // NOTE: technically this function isn't necessary.
        //       could just pass IPEndPointNonAlloc.
        //       still good for strong typing.
        public static int SendTo_NonAlloc(
            this Socket socket,
            byte[] buffer,
            int offset,
            int size,
            SocketFlags socketFlags,
            IPEndPointNonAlloc remoteEndPoint)
        {
            EndPoint casted = remoteEndPoint;
            return socket.SendTo(buffer, offset, size, socketFlags, casted);
        }
    }
}
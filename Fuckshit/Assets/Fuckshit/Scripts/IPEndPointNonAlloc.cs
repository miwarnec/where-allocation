using System;
using System.Net;

namespace Fuckshit
{
    public class IPEndPointNonAlloc : IPEndPoint
    {
        // ReceiveFrom calls remoteEndPoint.Serialize at first:
        // https://github.com/mono/mono/blob/f74eed4b09790a0929889ad7fc2cf96c9b6e3757/mcs/class/System/System.Net.Sockets/Socket.cs#L1733
        // -> this creates a new SocketAddress each time, which allocates.
        // -> instead, serialize only once in constructor
        // IMPORTANT: DO NOT MODIFY
        // -> internal so tests can validate that it's never changed
        internal readonly SocketAddress cache;

        // ReceiveFrom passes the serialized SocketAddress into ReceiveFrom_Internal,
        // which then writes the remote end's SocketAddress into it.
        // -> we need a worker copy to write into, without ever modifying our
        //    original cached SocketAddress above.
        // -> this copy is always equal to the last ReceiveFrom's remote
        //    SocketAddress
        internal readonly SocketAddress temp;

        // IPEndPoint.Serialize allocates a new SocketAddress each time:
        // https://github.com/mono/mono/blob/bdd772531d379b4e78593587d15113c37edd4a64/mcs/class/referencesource/System/net/System/Net/IPEndPoint.cs#L128
        //
        // we can't do it manually because the SocketAddress ctor is internal:
        //   serialized = new SocketAddress(Address, Port);
        //
        // BUT we can still call the base Serialize function:
        // (which does NOT call our overwritten one)
        public IPEndPointNonAlloc(long address, int port) : base(address, port)
        {
            cache = base.Serialize();
            temp = base.Serialize();
        }
        public IPEndPointNonAlloc(IPAddress address, int port) : base(address, port)
        {
            cache = base.Serialize();
            temp = base.Serialize();
        }

        // as explained above, we want to cache the Serialization.
        // but we CAN NOT return the original cache because ReceiveFrom_Internal
        // writes into it.
        // => manually copy our cache to the temporary one.
        // => and return it so ReceiveFrom_Internal can write into it here:
        //    https://github.com/mono/mono/blob/f74eed4b09790a0929889ad7fc2cf96c9b6e3757/mcs/class/System/System.Net.Sockets/Socket.cs#L1739
        public override SocketAddress Serialize()
        {
            // copy all the fields:
            //   internal int m_Size;
            //   internal byte[] m_Buffer;

            // for now, let's only handle the same size
            if (temp.Size == cache.Size)
            {
                // copy buffer from cache to temp
                for (int i = 0; i < cache.Size; ++i)
                    temp[i] = cache[i];

                // return temp (which will be modified)
                return temp;
            }
            // NOTE: different size should not really happen, because .Create()
            //       compares the AddressFamily below?
            // TODO create a new one in those cases?
            // TODO if ReceiveFrom ever modifies size, then cache that one too
            //      and reuse it?
            throw new Exception($"IPEndPointNonAlloc.Serialize: size mismatch. cache.Size={cache.Size} temp.Size={temp.Size}. Can't copy.");
        }

        // ReceiveFrom calls EndPoint.Create(), which allocates:
        // https://github.com/mono/mono/blob/f74eed4b09790a0929889ad7fc2cf96c9b6e3757/mcs/class/System/System.Net.Sockets/Socket.cs#L1761
        // because it creates a new IPEndPoint from the SocketAddress.
        // -> SocketAddress is exactly the one returned by Serialize() above
        // -> which means it's always 'this.temp'
        // -> simply do nothing. return self.
        // => Extensions.ReceiveFromNonAlloc will take the SocketAddress from
        //    'temp'.
        public override EndPoint Create(SocketAddress socketAddress)
        {
            //Debug.LogWarning($"{nameof(IPEndPointNonAlloc)}.Create() hook");

            // original IPEndPoint.Create validates:
            if (socketAddress.Family != AddressFamily)
                throw new ArgumentException($"Unsupported socketAddress.AddressFamily: {socketAddress.Family}. Expected: {AddressFamily}");
            if (socketAddress.Size < 8)
                throw new ArgumentException($"Unsupported socketAddress.Size: {socketAddress.Size}. Expected: <8");

            // do nothing
            return this;
        }
    }
}
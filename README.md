# where-allocation
**_Nearly_** allocation free Mono C# UDP SendTo/ReceiveFrom **NonAlloc**.

![whereallocation_smaller](https://user-images.githubusercontent.com/16416509/120292650-129e3180-c2f7-11eb-8249-e7e1e950ae87.jpg)

Made by [vis2k](https://github.com/vis2k/) & [FakeByte](https://github.com/FakeByte/).

# ReceiveFrom Allocations
Mono C#'s Socket.ReceiveFrom has heavy allocations (338 byte in Unity):

<img width="595" alt="ReceiveFrom_Before" src="https://user-images.githubusercontent.com/16416509/120093573-d24f7f80-c14d-11eb-8afe-573942b71b60.png">

Which is a huge issue for multiplayer games which try to minimize runtime allocations / GC.

It allocates because IPEndPoint **.Create** allocates a new IPEndPoint, and **Serialize()** allocates a new SocketAddress.

Both functions are called in [Mono's Socket.ReceiveFrom](https://github.com/mono/mono/blob/f74eed4b09790a0929889ad7fc2cf96c9b6e3757/mcs/class/System/System.Net.Sockets/Socket.cs#L1761):
```csharp
int ReceiveFrom (Memory<byte> buffer, int offset, int size, SocketFlags socketFlags, ref EndPoint remoteEP, out SocketError errorCode)
{
    SocketAddress sockaddr = remoteEP.Serialize();

    int nativeError;
    int cnt;
    unsafe {
        using (var handle = buffer.Slice (offset, size).Pin ()) {
            cnt = ReceiveFrom_internal (m_Handle, (byte*)handle.Pointer, size, socketFlags, ref sockaddr, out nativeError, is_blocking);
        }
    }

    errorCode = (SocketError) nativeError;
    if (errorCode != SocketError.Success) {
        if (errorCode != SocketError.WouldBlock && errorCode != SocketError.InProgress) {
            is_connected = false;
        } else if (errorCode == SocketError.WouldBlock && is_blocking) { // This might happen when ReceiveTimeout is set
            errorCode = SocketError.TimedOut;
        }

        return 0;
    }

    is_connected = true;
    is_bound = true;

    /* If sockaddr is null then we're a connection oriented protocol and should ignore the
     * remoteEP parameter (see MSDN documentation for Socket.ReceiveFrom(...) ) */
    if (sockaddr != null) {
        /* Stupidly, EndPoint.Create() is an instance method */
        remoteEP = remoteEP.Create (sockaddr);
    }

    seed_endpoint = remoteEP;

    return cnt;
}
```

# How where-allocation avoids the Allocations
**IPEndPointNonAlloc** inherits from IPEndPoint to overwrite **Create()**, **Serialize()** and **GetHashCode()**.
* **Create(SocketAddress)** does not create a new IPEndPoint anymore. It only stores the SocketAddress.
* **Serialize()** does not create a new SocketAddress anymore. It only returns the stored one.
* **GetHashCode()** returns the cached SocketAddress GetHashCode() directly without allocations.

# Benchmarks
Using [Mirror](https://github.com/vis2k/Mirror) with 1000 monsters, Unity 2019 LTS (Deep Profiling), we previously allocated **8.9 KB**:

<img width="702" alt="Mirror - 1k - serveronly - before" src="https://user-images.githubusercontent.com/16416509/120271597-33a65880-c2de-11eb-8f70-3dd8db20f510.png">

With where-allocation, it's reduced to **364 B**:

<img width="700" alt="Mirror - 1k - serveronly - after" src="https://user-images.githubusercontent.com/16416509/120271608-399c3980-c2de-11eb-854a-51333d41b65c.png">

**=> 25x reduction** in allocations/GC!<br/>

# Usage Guide
See the **Example** folder or [kcp2k](https://github.com/vis2k/kcp2k/).

* Use IPEndPointNonAlloc
* Use ReceiveFrom_NonAlloc
* Use SendTo_NonAlloc
* Use IPEndPointNonAlloc.DeepCopyIPEndPoint() to create an actual copy (once per new connection)

Here is how the server polls, from the **Example**:
```csharp
if (serverSocket.Poll(0, SelectMode.SelectRead))
{
    // nonalloc ReceiveFrom
    int msgLength = serverSocket.ReceiveFrom_NonAlloc(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, serverReusableReceiveEP);

    // new connection? then allocate an actual IPEndPoint once to store it.
    if (newClientEP == null)
        newClientEP = serverReusableReceiveEP.DeepCopyIPEndPoint();

    // process the message...
    message = new ArraySegment<byte>(receiveBuffer, 0, msgLength);
}
```

# Tests
where-allocation comes with several unit tests to guarantee stability:

<img width="348" alt="2021-06-01_13-58-31@2x" src="https://user-images.githubusercontent.com/16416509/120273789-89c8cb00-c2e1-11eb-82b8-72a126edf128.png">

# Showcase
where-allocation is used by:
* [kcp2k](https://github.com/vis2k/kcp2k/)
* [Mirror](https://github.com/vis2k/Mirror/)

# Remaining Allocations
In Unity 2019/2020, Socket.ReceiveFrom_Internal still allocates 90 bytes because of the oudated Mono version:

<img width="990" alt="Unity2019 LTS Mono - ReceiveFrom" src="https://user-images.githubusercontent.com/16416509/120100294-a266a300-c172-11eb-9b64-f0c04c8db0a8.png">

Unity 2021.2.0.a18 is [supposed to have the latest Mono](https://forum.unity.com/threads/unity-future-net-development-status.1092205/page-3#post-7164088).

Which should automatically get rid of the last allocation.

# Fuckshit
**_Nearly_** allocation free Mono C# UDP SendTo/ReceiveFrom **NonAlloc**.

# ReceiveFrom Allocations
Mono C#'s Socket.ReceiveFrom has heavy allocations (338 byte in Unity):

<img width="595" alt="ReceiveFrom_Before" src="https://user-images.githubusercontent.com/16416509/120093573-d24f7f80-c14d-11eb-8afe-573942b71b60.png">

Which is a huge issue for multiplayer games which try to minimize runtime allocations / GC.

# Why Socket.ReceiveFrom Allocates
It calls EndPoint.Create(SocketAddress) to return a new EndPoint each time:

https://github.com/mono/mono/blob/f74eed4b09790a0929889ad7fc2cf96c9b6e3757/mcs/class/System/System.Net.Sockets/Socket.cs#L1761

# How Fuckshit avoids the Allocations
**IPEndPointNonAlloc** inherits from IPEndPoint to overwrite **Create()**, **Serialize()** and **GetHashCode()**.
* **Create(SocketAddress)** does not create a new IPEndPoint anymore. It only stores the SocketAddress.
* **Serialize()** does not create a new SocketAddress anymore. It only returns the stored one.
* **GetHashCode()** returns the cached SocketAddress GetHashCode() directly without allocations.

# Benchmarks
Using [Mirror](https://github.com/vis2k/Mirror) with 1000 monsters, Unity 2019 LTS (Deep Profiling), we previously allocated **8.9 KB**:

<img width="702" alt="Mirror - 1k - serveronly - before" src="https://user-images.githubusercontent.com/16416509/120271597-33a65880-c2de-11eb-8f70-3dd8db20f510.png">

With Fuckshit, it's reduced to **364 B**:

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

# Showcase
Fuckshit is used by:
* [kcp2k](https://github.com/vis2k/kcp2k/)
* [Mirror](https://github.com/vis2k/Mirror/)

# Remaining Allocations
In Unity 2019/2020, Socket.ReceiveFrom_Internal still allocates 90 bytes because of the oudated Mono version:

<img width="990" alt="Unity2019 LTS Mono - ReceiveFrom" src="https://user-images.githubusercontent.com/16416509/120100294-a266a300-c172-11eb-9b64-f0c04c8db0a8.png">

Unity 2021.2.0.a18 is [supposed to have the latest Mono](https://forum.unity.com/threads/unity-future-net-development-status.1092205/page-3#post-7164088).

Which should automatically get rid of the last allocation.

# Fuckshit
**_Nearly_** allocation free Mono C# UDP SendTo/ReceiveFrom **NonAlloc**.

# ReceiveFrom Allocations
C#'s Socket.ReceiveFrom has heavy allocations (338 byte in Unity):

<img width="595" alt="ReceiveFrom_Before" src="https://user-images.githubusercontent.com/16416509/120093573-d24f7f80-c14d-11eb-8afe-573942b71b60.png">

Which is a huge issue for multiplayer games which try to minimize runtime allocations / GC.

# Why Socket.ReceiveFrom Allocates
It calls EndPoint.Create(SocketAddress) to return a new EndPoint each time:

https://github.com/mono/mono/blob/f74eed4b09790a0929889ad7fc2cf96c9b6e3757/mcs/class/System/System.Net.Sockets/Socket.cs#L1761

# How Fuckshit avoids the Allocations
IPEndPointNonAlloc inherits from IPEndPoint to overwrite Create(), Serialize() and GetHashCode().
* Create(SocketAddress) does not create a new object anymore. It only stores the SocketAddress.
* Serialize() does not create a new SocketAddress anymore. It only returns the stored one.
* GetHashCode() returns the cached SocketAddress GetHashCode() directly without allocations.

# Benchmarks
Using [Mirror](https://github.com/vis2k/Mirror) with 1000 monsters, we previously allocated 8.9KB:

<img width="889" alt="Mirror - 1k - serveronly - before" src="https://user-images.githubusercontent.com/16416509/120270474-4455cf00-c2dc-11eb-914f-cd99654341bc.png">

With Fuckshit, it's reduced to 364 B:

<img width="881" alt="Mirror - 1k - serveronly - after" src="https://user-images.githubusercontent.com/16416509/120270498-4e77cd80-c2dc-11eb-8c34-3813e5befd47.png">

**=> 25x reduction** in allocations/GC!<br/>
**=> 9x improvement** in performance (see Time/ms)!

# Usage Guide
It's important to understand that ReceiveFrom_NonAlloc takes IPEndPointNonAlloc which:
- only holds a SocketAddress in **.temp**
- does not have its values set like a regular IPEndPoint would
- is reused every time

In other words, allocate a new IPEndPoint only **once** when adding the connection the first time.

## Server Pseudcode:
```csharp
// ReceiveFromNonAlloc with reusable IPEndPointNonAlloc
int received = socket.ReceiveFromNonAlloc(out message, reusable);

// hash (nonalloc)
int connectionId = SocketAddress.GetHashCode();

// allocate IPEndPoint only once when adding connection
if (!connections.Contains(connectionId))
    connectons[connectionId] = new IPEndPoint(SocketAddress);

// process message
connections[connectionId].OnMessage(message)
```

# Showcase
Fuckshit is used by:
* [kcp2k](https://github.com/vis2k/kcp2k/)
* [Mirror](https://github.com/vis2k/Mirror/)

# Remaining Allocations
In Unity 2019/2020, Socket.ReceiveFrom_Internal still allocates 90 bytes because of the oudated Mono version:

<img width="990" alt="Unity2019 LTS Mono - ReceiveFrom" src="https://user-images.githubusercontent.com/16416509/120100294-a266a300-c172-11eb-9b64-f0c04c8db0a8.png">

Unity 2021.2.0.a18 is supposed to have the latest Mono:
https://forum.unity.com/threads/unity-future-net-development-status.1092205/page-3#post-7164088

Which should automatically get rid of the last allocation.

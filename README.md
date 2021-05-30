# Fuckshit
**_Nearly_** allocation free C# Socket.ReceiveFromNonAlloc.

# ReceiveFrom Allocations
C#'s Socket.ReceiveFrom has heavy allocations (338 byte):
<img width="595" alt="ReceiveFrom_Before" src="https://user-images.githubusercontent.com/16416509/120093573-d24f7f80-c14d-11eb-8afe-573942b71b60.png">

Which is a huge issue for multiplayer games.



# Why Socket.ReceiveFrom Allocates
It calls EndPoint.Create(SocketAddress) to return a new EndPoint each time:
https://github.com/mono/mono/blob/f74eed4b09790a0929889ad7fc2cf96c9b6e3757/mcs/class/System/System.Net.Sockets/Socket.cs#L1761

# How Fuckshit avoids the Allocations
IPEndPointNonAlloc inherits from IPEndPoint to overwrite Create() and Serialize().
* Create(SocketAddress) does not create a new object anymore. It only stores the SocketAddress.
* Serialize does not create a new SocketAddress anymore. It only returns the stored one.

As result, we get a **3.75x** reduction in allocations. 

ReceiveFromNonAlloc only allocates 90 byte:
<img width="580" alt="ReceiveFrom_IPEndPointNonAlloc_ReceiveNonAlloc" src="https://user-images.githubusercontent.com/16416509/120093652-3ffbab80-c14e-11eb-93e9-0d350bead4fa.png">

# Usage Guide (Pseudocode)
It's important to understand that ReceiveFrom_NonAlloc:
- returns a SocketAddress, not an EndPoint
- always writes into the **same** SocketAddress object

In other words, you allocate your own IPEndPoint only **once** when adding the connection the first time.
Afterwards, you can use the allocation free SocketAddress.GetHashCode() function to identify which connection the message is from.

Server pseudcode:
```csharp
// ReceiveFromNonAlloc always returns the same 'object'
// with different internal values.
ReceiveFromNonAlloc(out message, out SocketAddress);

// hash (nonalloc)
int connectionId = SocketAddress.GetHashCode();

// allocate IPEndPoint only once when adding connection
if (!connections.Contains(connectionId))
    connectons[connectionId] = new IPEndPoint(SocketAddress);

// process message
connections[connectionId].OnMessage(message)
```

# Showcase
Used by [kcp2k](https://github.com/vis2k/kcp2k/)
Used by [Mirror](https://github.com/vis2k/Mirror/)

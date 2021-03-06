# Uavcan.NET
UAVCAN v0 protocol stack implementation for .NET platform.

## Features
- Flexible and fast serializer for converting between .NET objects and UAVCAN data structures.
- Built-in asynchronous I/O support.
- SLCAN adapters support.
- Extensible GUI Tool for bus management and diagnostics.

## Usage
```C#
using var driver = new UsbTin();
await driver.ConnectAsync("COM1").ConfigureAwait(false);
await driver.OpenCanChannelAsync(125000, UsbTinOpenMode.Active).ConfigureAwait(false);

var typeResolver = new FileSystemUavcanTypeResolver(@"Path to DSDL definitions");

using var engine = new UavcanInstance(typeResolver)
{
    NodeID = 5
};
engine.AddDriver(driver);

var response = await engine.SendServiceRequestAsync(
    destinationNodeId: 1,
    value: new GetNodeInfo_Request())
    .ConfigureAwait(false);
var nodeInfo = Serializer.Deserialize<GetNodeInfo_Response>(response.ContentBytes);
Console.WriteLine(nodeInfo.Name);

var panicType = typeResolver.ResolveType("uavcan.protocol", "Panic");
var panic = new Dictionary<string, object>
{
    ["ReasonText"] = "Sample"
};
engine.SendBroadcastMessage(
    value: panic, 
    valueType: panicType);
```

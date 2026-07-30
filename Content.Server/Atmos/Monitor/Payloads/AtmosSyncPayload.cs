using Content.Shared.DeviceNetwork;

namespace Content.Server.Atmos.Monitor.Payloads;

/// <summary>
/// A general payload that when sent to an atmos device forces it to respond with its data payload.
/// </summary>
public partial record struct AtmosSyncPayload : INetworkPayload;

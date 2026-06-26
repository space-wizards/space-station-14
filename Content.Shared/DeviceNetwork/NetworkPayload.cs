using Content.Shared.DeviceNetwork.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.DeviceNetwork;

[ImplicitDataDefinitionForInheritors]
public partial interface INetworkPayload;

/// <summary>
/// A data class for information passing through a Device Network.
/// </summary>
[ImplicitDataDefinitionForInheritors]
[Serializable, NetSerializable]
public abstract partial class NetworkPayload : INetworkPayload;

/// <summary>
/// A <see cref="NetworkPayload"/> that can be handled by systems that inherit <see cref="DeviceNetworkHandler"/>,
/// which have better functionality and performance.
/// </summary>
[ImplicitDataDefinitionForInheritors]
[Serializable, NetSerializable]
public abstract partial class HandledNetworkPayload : INetworkPayload;

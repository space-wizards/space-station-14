using Robust.Shared.Serialization;

namespace Content.Shared.DeviceNetwork;

public interface INetworkPayload;

/// <summary>
/// A data class for information passing through a Device Network.
/// </summary>
[ImplicitDataDefinitionForInheritors]
[Serializable, NetSerializable]
public abstract partial class NetworkPayload : INetworkPayload;

/// <summary>
/// A <see cref="NetworkPayload"/> that is automatically
/// </summary>
[ImplicitDataDefinitionForInheritors]
[Serializable, NetSerializable]
public abstract partial class HandledNetworkPayload : INetworkPayload;

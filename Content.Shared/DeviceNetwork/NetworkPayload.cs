using Robust.Shared.Serialization;

namespace Content.Shared.DeviceNetwork;

/// <summary>
/// A data class for information passing through a Device Network.
/// </summary>
[ImplicitDataDefinitionForInheritors]
[Serializable, NetSerializable]
public abstract partial class NetworkPayload;

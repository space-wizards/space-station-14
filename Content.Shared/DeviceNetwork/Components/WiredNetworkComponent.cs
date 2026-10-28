using Robust.Shared.GameStates;

namespace Content.Shared.DeviceNetwork.Components;

/// <summary>
/// Allows a device to send and receive device network messages over wired connections.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class WiredNetworkComponent : Component;

using Robust.Shared.GameStates;

namespace Content.Shared.Light.Components;

/// <summary>
///     Component that represents an active emergency light.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ActiveEmergencyLightComponent : Component;

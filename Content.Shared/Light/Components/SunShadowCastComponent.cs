using Robust.Shared.GameStates;

namespace Content.Shared.Light.Components;

/// <summary>
/// Treats this entity as a 1x1 tile and extrapolates its position along the <see cref="SunShadowComponent"/> direction.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SunShadowCastComponent : Component;

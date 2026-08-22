using Robust.Shared.GameStates;

namespace Content.Shared.Timing;

/// <summary>
/// Applies UseDelay whenever the entity shoots.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(UseDelaySystem))]
public sealed partial class UseDelayOnShootComponent : Component
{
    /// <summary>
    /// <see cref="UseDelayInfo"/> ID this applies to.
    /// </summary>
    [DataField]
    public string UseDelayId = UseDelaySystem.DefaultId;
}

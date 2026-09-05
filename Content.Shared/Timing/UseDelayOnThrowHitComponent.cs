using Robust.Shared.GameStates;

namespace Content.Shared.Timing;

/// <summary>
///     Activates UseDelay when a thrown item hits something.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(UseDelaySystem))]
public sealed partial class UseDelayOnThrowHitComponent : Component
{
    /// <summary>
    /// <see cref="UseDelayInfo"/> ID this applies to.
    /// </summary>
    [DataField]
    public string UseDelayId = UseDelaySystem.DefaultId;
}

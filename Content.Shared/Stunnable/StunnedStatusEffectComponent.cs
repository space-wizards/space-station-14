using Robust.Shared.GameStates;

namespace Content.Shared.Stunnable;

/// <summary>
/// Stun as a status effect.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedStunSystem))]
public sealed partial class StunnedStatusEffectComponent : Component
{
    /// <summary>
    /// Should we try to remove the <see cref="StunnedComponent"/> from the target entity when the status ends?
    /// </summary>
    [DataField]
    public bool Remove = true;
}

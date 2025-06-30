using Robust.Shared.GameStates;

namespace Content.Shared.Stunnable;

/// <summary>
/// Knockdown as a status effect.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedStunSystem))]
public sealed partial class KnockdownStatusEffectComponent : Component
{
    /// <summary>
    /// Should we try to remove the <see cref="KnockedDownComponent"/> from the target entity when the status ends?
    /// </summary>
    [DataField]
    public bool Remove = true;
}

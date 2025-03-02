using Content.Shared.Revenant.Systems;

namespace Content.Shared.Revenant.Components;

/// <summary>
///     Makes the target solid, visible, and applies a slowdown.
///     Meant to be used in conjunction with statusEffectSystem
/// </summary>
[RegisterComponent, Access(typeof(SharedCorporealSystem))]
public sealed partial class CorporealComponent : Component
{
    /// <summary>
    ///     The debuff applied when the component is present.
    /// </summary>
    [DataField]
    public float MovementSpeedDebuff = 0.66f;
}

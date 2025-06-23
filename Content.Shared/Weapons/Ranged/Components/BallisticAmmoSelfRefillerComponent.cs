using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
/// This component, analogous to <c>BatterySelfRechargerComponent</c>, will attempt insert ballistic ammunition into
/// its owner's <see cref="BallisticAmmoProviderComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause, Access(typeof(SharedGunSystem))]
public sealed partial class BallisticAmmoSelfRefillerComponent : Component
{
    /// <summary>
    /// Whether or not the refilling behavior is active.
    /// </summary>
    [DataField, ViewVariables, AutoNetworkedField]
    public bool AutoRefill = true;

    /// <summary>
    /// How often a new piece of ammunition is inserted into the owner's <see cref="BallisticAmmoProviderComponent"/>.
    /// </summary>
    [DataField, ViewVariables, AutoNetworkedField]
    public TimeSpan AutoRefillRate;

    /// <summary>
    /// If true, causes the refilling behavior to be delayed by at least <see cref="AutoRefillPauseDuration"/> after
    /// the owner is fired.
    /// </summary>
    [DataField, ViewVariables, AutoNetworkedField]
    public bool FiringPausesAutoRefill = false;

    /// <summary>
    /// How long to pause for if <see cref="FiringPausesAutoRefill"/> is true.
    /// </summary>
    [DataField, ViewVariables, AutoNetworkedField]
    public TimeSpan AutoRefillPauseDuration = TimeSpan.Zero;

    /// <summary>
    /// What entity to spawn and attempt to insert into the owner.
    /// </summary>
    [DataField(required: true), ViewVariables, AutoNetworkedField]
    public EntProtoId AmmoProto;

    /// <summary>
    /// When the next auto refill should occur. This is just implementation state.
    /// </summary>
    [ViewVariables, AutoNetworkedField, AutoPausedField]
    public TimeSpan NextAutoRefill = TimeSpan.Zero;
}

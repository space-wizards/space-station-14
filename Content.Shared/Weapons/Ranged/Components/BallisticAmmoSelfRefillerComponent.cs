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
    [DataField, AutoNetworkedField]
    public bool AutoRefill = true;

    /// <summary>
    /// How often a new piece of ammunition is inserted into the owner's <see cref="BallisticAmmoProviderComponent"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan AutoRefillRate;

    /// <summary>
    /// If true, causes the refilling behavior to be delayed by at least <see cref="AutoRefillPauseDuration"/> after
    /// the owner is fired.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool FiringPausesAutoRefill = false;

    /// <summary>
    /// How long to pause for if <see cref="FiringPausesAutoRefill"/> is true.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan AutoRefillPauseDuration = TimeSpan.Zero;

    /// <summary>
    /// What entity to spawn and attempt to insert into the owner. If null, uses
    /// <see cref="BallisticAmmoProviderComponent.Proto"/>. If that's also null, this component does nothing but log
    /// errors.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId? AmmoProto;

    /// <summary>
    /// If true, EMPs will pause this component's behavior.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AffectedByEmp = false;

    /// <summary>
    /// When the next auto refill should occur. This is just implementation state.
    /// </summary>
    [ViewVariables, AutoNetworkedField, AutoPausedField]
    public TimeSpan NextAutoRefill = TimeSpan.Zero;
}

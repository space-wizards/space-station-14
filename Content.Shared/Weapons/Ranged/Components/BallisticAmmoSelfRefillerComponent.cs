using Content.Shared.Power.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
/// This component, analogous to <see cref="BatterySelfRechargerComponent"/>, will attempt insert ballistic ammunition
/// into its owner's <see cref="BallisticAmmoProviderComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true), AutoGenerateComponentPause,
 Access(typeof(SharedGunSystem))]
public sealed partial class BallisticAmmoSelfRefillerComponent : Component
{
    /// <summary>
    /// True if the refilling behavior is active, false otherwise.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AutoRefill = true;

    /// <summary>
    /// How often a new piece of ammunition is inserted into the owner's <see cref="BallisticAmmoProviderComponent"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan AutoRefillRate = TimeSpan.FromSeconds(1);

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
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextAutoRefill = TimeSpan.Zero;
}

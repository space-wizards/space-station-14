using Content.Shared.Database;
using Content.Shared.DeviceLinking;
using Content.Shared.Power.Components;
using Content.Shared.Singularity.EntitySystems;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
/// Component for making ammo out of powered network. Requires and works with <see cref="PowerStateComponent"/>
/// to drain more power while it is ON and shooting.
/// Power consumption is stable and does not spike at moment of projectile creation.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedNetworkPoweredAmmoProviderSystem), typeof(BatteryWeaponFireModesSystem), typeof(SharedEmitterSystem))]
public sealed partial class NetworkPoweredAmmoProviderComponent : AmmoProviderComponent
{
    /// <summary>
    /// The projectile or hitscan entity to spawn when firing.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Prototype;

    /// <summary>
    /// Whether the power switch is on AND the machine has enough power (so is actively firing)
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsPowered = false;

    /// <summary>
    /// Signal port that turns on the emitter.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> OnPort = "On";

    /// <summary>
    /// Signal port that turns off the emitter.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> OffPort = "Off";

    /// <summary>
    /// Signal port that toggles the emitter on or off.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> TogglePort = "Toggle";

    /// <summary>
    /// Log type for event of toggling device On or Off.
    /// Will not add admin event if set to null.
    /// </summary>
    [DataField]
    public LogType? AdminLogToggleLevel;
}

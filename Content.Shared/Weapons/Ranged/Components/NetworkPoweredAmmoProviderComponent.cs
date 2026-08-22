using Content.Shared.DeviceLinking;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
/// System for making power network an ammo provider.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedNetworkPoweredAmmoProviderSystem), typeof(BatteryWeaponFireModesSystem))]
public sealed partial class NetworkPoweredAmmoProviderComponent : AmmoProviderComponent
{
    /// <summary>
    /// Whether the emitter is switched to emitting or now.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsOn;

    // Whether the power switch is on AND the machine has enough power (so is actively firing)
    [ViewVariables] public bool IsPowered;

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
}

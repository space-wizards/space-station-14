using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
/// Allows battery weapons to fire different types of projectiles
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(BatteryWeaponFireModesSystem))]
[AutoGenerateComponentState]
public sealed partial class BatteryWeaponFireModesComponent : Component
{
    /// <summary>
    /// A list of the different firing modes the weapon can switch between
    /// </summary>
    [DataField(required: true)]
    [AutoNetworkedField]
    public List<BatteryWeaponFireMode> FireModes = new();

    /// <summary>
    /// The currently selected firing mode
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public int CurrentFireMode;
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class BatteryWeaponFireMode
{
    /// <summary>
    /// The projectile prototype associated with this firing mode
    /// </summary>
    [DataField("proto", required: true)]
    public EntProtoId Prototype = default!;

    /// <summary>
    /// The battery cost to fire the projectile associated with this firing mode
    /// </summary>
    [DataField]
    public float FireCost = 100;
}

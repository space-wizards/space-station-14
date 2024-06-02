using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Server.DeltaV.Weapons.Ranged.Systems;

namespace Content.Server.DeltaV.Weapons.Ranged.Components;

/// <summary>
/// Allows for energy gun to switch between three modes. This also changes the sprite accordingly.
/// </summary>
/// <remarks>This is BatteryWeaponFireModesSystem with additional changes to allow for different sprites.</remarks>
[RegisterComponent]
[Access(typeof(EnergyGunSystem))]
[AutoGenerateComponentState]
public sealed partial class EnergyGunComponent : Component
{
    /// <summary>
    /// A list of the different firing modes the energy gun can switch between
    /// </summary>
    [DataField("fireModes", required: true)]
    [AutoNetworkedField]
    public List<EnergyWeaponFireMode> FireModes = new();

    /// <summary>
    /// The currently selected firing mode
    /// </summary>
    [DataField("currentFireMode")]
    [AutoNetworkedField]
    public EnergyWeaponFireMode? CurrentFireMode = default!;
}

[DataDefinition]
public sealed partial class EnergyWeaponFireMode
{
    /// <summary>
    /// The projectile prototype associated with this firing mode
    /// </summary>
    [DataField("proto", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Prototype = default!;

    /// <summary>
    /// The battery cost to fire the projectile associated with this firing mode
    /// </summary>
    [DataField("fireCost")]
    public float FireCost = 100;

    /// <summary>
    /// The name of the selected firemode
    /// </summary>
    [DataField("name")]
    public string Name = string.Empty;

    /// <summary>
    /// What RsiState we use for that firemode if it needs to change.
    /// </summary>
    [DataField("state")]
    public string State = string.Empty;
}

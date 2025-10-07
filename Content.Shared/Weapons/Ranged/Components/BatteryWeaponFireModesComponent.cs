using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

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
    public List<BatteryWeaponFireMode> FireModes = new();

    /// <summary>
    /// The currently selected firing mode (index in <see cref="FireModes"/>).
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
    /// Icon that can represent mode in UI.
    /// </summary>
    [DataField]
    public SpriteSpecifier ModeIcon;

    /// <summary>
    /// The battery cost to fire the projectile associated with this firing mode
    /// </summary>
    [DataField]
    public float FireCost = 100;
}

[Serializable, NetSerializable]
public enum BatteryWeaponFireModeVisuals : byte
{
    State
}

/// <summary>
/// Message for changing battery weapon fire mode.
/// Uses index of mode in <see cref="BatteryWeaponFireModesComponent.FireModes"/>.
/// </summary>
[Serializable, NetSerializable]
public sealed class BatteryWeaponFireModeChangeMessage : BoundUserInterfaceMessage
{
    public int ModeIndex { get; set; }
}

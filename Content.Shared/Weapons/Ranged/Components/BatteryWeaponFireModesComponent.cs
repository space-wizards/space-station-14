using Content.Shared.Hands.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
/// Allows battery weapons to fire different types of projectiles
/// </summary>
[RegisterComponent, NetworkedComponent]
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

    /// <summary>
    /// Layers to add to the sprite of the player that is holding this entity (for changing gun color)
    /// </summary>
    [DataField]
    public Dictionary<HandLocation, List<PrototypeLayerData>> InhandVisuals = new();

    [DataField]
    public Dictionary<HandLocation, List<PrototypeLayerData>> WieldedInhandVisuals = new();

    /// <summary>
    /// Layers to add to the sprite of the player that is wearing this entity (for changing gun color)
    /// </summary>
    [DataField]
    public Dictionary<string, List<PrototypeLayerData>> ClothingVisuals = new();

}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class BatteryWeaponFireMode
{
    /// <summary>
    /// The projectile prototype associated with this firing mode
    /// </summary>
    [DataField("proto", required: true)]
    public EntProtoId Prototype;

    /// <summary>
    /// The battery cost to fire the projectile associated with this firing mode
    /// </summary>
    [DataField]
    public float FireCost = 100;

    /// <summary>
    /// The color that the firemode should show on the gun sprite
    /// </summary>
    [DataField]
    public Color Color = Color.Blue;
}

[Serializable, NetSerializable]
public enum BatteryWeaponFireModeVisuals : byte
{
    State
}
[Serializable, NetSerializable]
public enum BatteryWeaponFireModeVisualizer : byte
{
    Color,
}

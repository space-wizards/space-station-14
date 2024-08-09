using Content.Shared.Inventory;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Reflect;

/// <summary>
/// Entities with this component have a chance to reflect projectiles and hitscan shots
/// Uses <c>ItemToggleComponent</c> to control reflection.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ReflectComponent : Component, IClothingSlots
{
    /// <summary>
    /// What we reflect.
    /// </summary>
    [DataField]
    public ReflectType Reflects = ReflectType.Energy | ReflectType.NonEnergy;

    /// <summary>
    /// Probability for a projectile to be reflected.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ReflectProb = 0.25f;

    [DataField, AutoNetworkedField]
    public Angle Spread = Angle.FromDegrees(45);

    [DataField]
    public SoundSpecifier? SoundOnReflect = new SoundPathSpecifier("/Audio/Weapons/Guns/Hits/laser_sear_wall.ogg");

    /// <summary>
    /// The reflect component only works if the item is equipped
    /// in this inventory slot
    /// </summary>
    [DataField]
    public SlotFlags Slots { get; set; } = SlotFlags.NONE;
}

[Flags]
public enum ReflectType : byte
{
    None = 0,
    NonEnergy = 1 << 0,
    Energy = 1 << 1,
}

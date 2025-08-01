using Content.Shared.Inventory;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Reflect;

/// <summary>
/// Entities with this component have a chance to reflect projectiles and hitscan shots
/// Uses <c>ItemToggleComponent</c> to control reflection.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ReflectComponent : Component
{
    /// <summary>
    /// What we reflect.
    /// </summary>
    [DataField]
    public ReflectType Reflects = ReflectType.Energy | ReflectType.NonEnergy;

    /// <summary>
    /// Select in which inventory slots it will reflect.
    /// By default, it will reflect in any inventory position, except pockets.
    /// </summary>
    [DataField]
    public SlotFlags SlotFlags = SlotFlags.WITHOUT_POCKET;

    /// <summary>
    /// Is it allowed to reflect while being in hands.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ReflectingInHands = true;

    /// <summary>
    /// Can only reflect when placed correctly.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool InRightPlace;

    /// <summary>
    /// Probability for a projectile to be reflected.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ReflectProb = 0.25f;

    /// <summary>
    /// STARLIGHT: Enhanced reflection chances for specific bullet types.
    /// Key is the bullet/hitscan prototype ID, value is the reflection probability.
    /// If a bullet type is not in this dictionary, uses ReflectProb instead.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<string, float> EnhancedReflection = new();

    /// <summary>
    /// Probability for a projectile to be reflected.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Angle Spread = Angle.FromDegrees(45);

    // 🌟Starlight🌟
    [DataField("overrideAngle"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public Angle? OverrideAngle = null;

    /// <summary>
    /// The sound to play when reflecting.
    /// </summary>
    [DataField]
    public SoundSpecifier? SoundOnReflect = new SoundPathSpecifier("/Audio/Weapons/Guns/Hits/laser_sear_wall.ogg");
}

[Flags, Serializable, NetSerializable]
public enum ReflectType : byte
{
    None = 0,
    NonEnergy = 1 << 0,
    Energy = 1 << 1,
}

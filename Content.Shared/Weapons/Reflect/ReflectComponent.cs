using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Reflect;

/// <summary>
/// Entities with this component have a chance to reflect projectiles and hitscan shots
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class ReflectComponent : Component
{
    /// <summary>
    /// Can only reflect when enabled
    /// </summary>
    [DataField("enabled"), ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled = true;

    /// <summary>
    /// Probability for a projectile to be reflected.
    /// </summary>
    [DataField("reflectProb"), ViewVariables(VVAccess.ReadWrite)]
    public float ReflectProb;

    [DataField("spread"), ViewVariables(VVAccess.ReadWrite)]
    public Angle Spread = Angle.FromDegrees(5);

    [DataField("soundOnReflect")]
    public SoundSpecifier? SoundOnReflect = new SoundPathSpecifier("/Audio/Weapons/Guns/Hits/laser_sear_wall.ogg");
}

[Serializable, NetSerializable]
public sealed class ReflectComponentState : ComponentState
{
    public bool Enabled;
    public float ReflectProb;
    public Angle Spread;
    public ReflectComponentState(bool enabled, float reflectProb, Angle spread)
    {
        Enabled = enabled;
        ReflectProb = reflectProb;
        Spread = spread;
    }
}

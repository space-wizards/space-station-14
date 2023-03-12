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
    public bool Enabled;

    [DataField("hitscanChance"), ViewVariables(VVAccess.ReadWrite)]
    public float HitscanChance;

    [DataField("projectileChance"), ViewVariables(VVAccess.ReadWrite)]
    public float ProjectileChance;

    [DataField("spread"), ViewVariables(VVAccess.ReadWrite)]
    public Angle Spread = Angle.FromDegrees(5);

    [DataField("onReflect")]
    public SoundSpecifier? OnReflect;
}

[Serializable, NetSerializable]
public sealed class ReflectComponentState : ComponentState
{
    public bool Enabled;
    public float HitscanChance;
    public float ProjectileChance;
    public Angle Spread;
    public ReflectComponentState(bool enabled, float hitscanChance, float projectileChance, Angle spread)
    {
        Enabled = enabled;
        HitscanChance = hitscanChance;
        ProjectileChance = projectileChance;
        Spread = spread;
    }
}

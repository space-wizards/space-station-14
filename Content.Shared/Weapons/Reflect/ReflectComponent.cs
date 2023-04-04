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
    /// Reflect chance for hitscan weapons (lasers) and projectiles with heat damage (disabler)
    /// </summary>
    [DataField("energeticChance"), ViewVariables(VVAccess.ReadWrite)]
    public float EnergeticChance;

    [DataField("kineticChance"), ViewVariables(VVAccess.ReadWrite)]
    public float KineticChance;

    [DataField("spread"), ViewVariables(VVAccess.ReadWrite)]
    public Angle Spread = Angle.FromDegrees(5);

    [DataField("onReflect")]
    public SoundSpecifier? OnReflect = new SoundPathSpecifier("/Audio/Weapons/Guns/Hits/laser_sear_wall.ogg");
}

[Serializable, NetSerializable]
public sealed class ReflectComponentState : ComponentState
{
    public bool Enabled;
    public float EnergeticChance;
    public float KineticChance;
    public Angle Spread;
    public ReflectComponentState(bool enabled, float energeticChance, float kineticChance, Angle spread)
    {
        Enabled = enabled;
        EnergeticChance = energeticChance;
        KineticChance = kineticChance;
        Spread = spread;
    }
}

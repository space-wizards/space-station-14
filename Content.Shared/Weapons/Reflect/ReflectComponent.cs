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

    [DataField("chance"), ViewVariables(VVAccess.ReadWrite)]
    public float Chance;

    [DataField("spread"), ViewVariables(VVAccess.ReadWrite)]
    public Angle Spread = Angle.FromDegrees(5);

    [DataField("onReflect")]
    public SoundSpecifier? OnReflect;
}

[Serializable, NetSerializable]
public sealed class ReflectComponentState : ComponentState
{
    public bool Enabled;
    public float Chance;
    public Angle Spread;
    public ReflectComponentState(bool enabled, float chance, Angle spread)
    {
        Enabled = enabled;
        Chance = chance;
        Spread = spread;
    }
}

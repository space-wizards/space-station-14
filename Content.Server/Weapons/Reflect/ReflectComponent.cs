using Robust.Shared.Audio;

namespace Content.Server.Weapons.Reflect;

/// <summary>
/// Entities with this component have a chance to reflect projectiles and hitscan shots
/// </summary>
[RegisterComponent]
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

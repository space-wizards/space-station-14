using Robust.Shared.Audio;

namespace Content.Server._FTL.Weapons;

/// <summary>
/// This is used for tracking a weapon pad.
/// </summary>
[RegisterComponent]
public sealed class WeaponTargetingComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)] public bool CanFire = true;
    [DataField("cooldownTime"), ViewVariables(VVAccess.ReadWrite)]
    public float CooldownTime = 5f;

    [DataField("cooldownSound"), ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier CooldownSound = new SoundPathSpecifier("/Audio/Weapons/click.ogg");

    [DataField("isLinked"), ViewVariables(VVAccess.ReadWrite)]
    public bool IsLinked;
}

/// <summary>
/// Added to an entity using station map so when its parent changes we reset it.
/// </summary>
[RegisterComponent]
public sealed class WeaponTargetingUserComponent : Component
{
    [DataField("mapUid")] public EntityUid Map;
}

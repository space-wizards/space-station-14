namespace Content.Client.Weapons.Melee.Components;

/// <summary>
/// Used for melee attack animations. Typically just has a fadeout.
/// </summary>
[RegisterComponent]
public sealed class WeaponArcVisualsComponent : Component
{
    [ViewVariables, DataField("animation")]
    public WeaponArcAnimation Animation = WeaponArcAnimation.None;
}

public enum WeaponArcAnimation : byte
{
    None,
    Thrust,
    Slash,
}

using Content.Shared.Weapons.Reflect;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Hitscan.Components;

/// <summary>
/// Hitscan entities with this component will get reflected by certain things (E.G energy swords).
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HitscanReflectComponent : Component
{
    /// <summary>
    /// The reflective type, will only reflect from entities that have a matching reflection type.
    /// </summary>
    [DataField]
    public ReflectType ReflectiveType = ReflectType.Energy;

    /// <summary>
    /// The maximum number of reflections the laser will make. <see cref="CurrentReflections"/>
    /// </summary>
    [DataField]
    public int MaxReflections = 3;

    /// <summary>
    /// Current number of times this hitscan entity was reflected. Will not be more than <see cref="MaxReflections"/>
    /// </summary>
    [DataField]
    public int CurrentReflections;
}

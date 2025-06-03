using Content.Shared.Weapons.Reflect;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Hitscan.Components;

/// <summary>
/// Hitscan entities with this component will get reflected by certain things (E.g energy swords)
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HitscanReflectComponent : Component
{
    [DataField]
    public ReflectType ReflectiveType = ReflectType.Energy;

    [DataField]
    public int MaxReflections = 3;

    /// <summary>
    /// Current number of times this hitscan entity was reflected.
    /// </summary>
    [DataField]
    public int CurrentReflections;
}

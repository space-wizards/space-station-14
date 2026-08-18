using Robust.Shared.GameStates;

namespace Content.Server.Weapons.Hitscan.Components;

/// <summary>
/// Allows for hitscan entities to ignite their targets. This component modifies the ignition chance, as well as how many stacks are added once ignited.
/// </summary>
[RegisterComponent]
public sealed partial class HitscanIgniteComponent : Component
{
    /// <summary>
    /// The chance the target has to ignite when hit by a hitscan.
    /// </summary>
    [DataField]
    public float IgniteChance = 0.25f;

    /// <summary>
    /// How many fire stacks are added if ignition occurs.
    /// </summary>
    [DataField]
    public float FireStacks = 1f;
}

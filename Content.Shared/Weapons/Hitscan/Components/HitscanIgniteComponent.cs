using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Hitscan.Components;

/// <summary>
/// Hitscan entities that have this component will have the chance to ignite the target of the hitscan.
/// </summary>
[RegisterComponent,NetworkedComponent]
public sealed partial class HitscanIgniteComponent : Component
{
    /// <summary>
    /// The chance each hitscan entity has to ignite the target.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float IgniteChance = 0.25f;

    /// <summary>
    /// How many fire stacks are added, if the target is already on fire.
    /// </summary>
    [DataField,AutoNetworkedField]
    public float AddedFireStacks = 1f;
}

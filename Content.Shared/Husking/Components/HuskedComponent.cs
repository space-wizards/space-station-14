using Content.Shared.Husking.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Husking.Components;

/// <summary>
/// Means this entity has been husked. They are unrecognizable until healed.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(HuskingSystem))]
public sealed partial class HuskedComponent : Component
{
    /// <summary>
    /// The entity storing the original appearance of this entity.
    /// </summary>
    [DataField]
    public EntityUid? OriginalAppearance;
}

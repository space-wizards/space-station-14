using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Requires that a changeling has obtained the highest amount of unique identities out of all other changelings.
/// Checks against identities stored on the mind.
/// </summary>
[RegisterComponent, Access(typeof(ChangelingObjectiveSystem))]
public sealed partial class ChangelingMostIdentitiesConditionComponent : Component
{
    /// <summary>
    /// Whether this objective requires identities to have been gained via devouring.
    /// </summary>
    [DataField]
    public bool RequireDevour;
}

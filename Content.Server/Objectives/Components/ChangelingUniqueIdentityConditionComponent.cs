using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Requires that a changeling has obtained X unique identities. Checks against identities stored on the mind.
/// Depends on <see cref="NumberObjectiveComponent"/> to function.
/// </summary>
[RegisterComponent, Access(typeof(ChangelingObjectiveSystem))]
public sealed partial class ChangelingUniqueIdentityConditionComponent : Component
{
    /// <summary>
    /// Whether this objective requires identities to have been gained via devouring.
    /// </summary>
    [DataField]
    public bool RequireDevour;
}

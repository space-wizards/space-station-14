using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Requires that a changeling has obtained X unique identities.
/// Depends on <see cref="NumberObjectiveComponent"/> to function.
/// </summary>
[RegisterComponent, Access(typeof(ChangelingObjectiveSystem))]
public sealed partial class ChangelingUniqueIdentityConditionComponent : Component
{
    /// <summary>
    /// The amount of identities that have been already devoured.
    /// </summary>
    [DataField]
    public int UniqueIdentities;
}

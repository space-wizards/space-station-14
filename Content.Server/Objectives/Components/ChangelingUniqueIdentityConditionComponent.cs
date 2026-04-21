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
    /// Whether the target must be dead
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int UniqueIdentities;
}

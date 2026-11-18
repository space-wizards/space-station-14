using Content.Server.Objectives.Systems;
using Content.Shared.Antag;
using Robust.Shared.Prototypes;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Requires that there are a certain number of other antagonists assigned for this objective to be given.
/// </summary>
[RegisterComponent, Access(typeof(MultipleAntagsRequirementSystem))]
public sealed partial class MultipleAntagsRequirementComponent : Component
{
    /// <summary>
    /// Number of antagonists, excluding yourself, that have to exist.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int Count = 1;

    /// <summary>
    /// The specific antag groups we are checking against/counting.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public required List<ProtoId<AntagSpecifierPrototype>> Antags;
}

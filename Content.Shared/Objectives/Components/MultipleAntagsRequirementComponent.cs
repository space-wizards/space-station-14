using Content.Shared.Objectives.Systems;
using Content.Shared.Antag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Objectives.Components;

/// <summary>
/// Requires that there are a certain number of other antagonists assigned for this objective to be given.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
[Access(typeof(MultipleAntagsRequirementSystem))]
public sealed partial class MultipleAntagsRequirementComponent : Component
{
    /// <summary>
    /// Number of antagonists, including yourself, that have to exist.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Count = 2;

    /// <summary>
    /// The specific antag groups we are checking against/counting.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public HashSet<ProtoId<AntagSpecifierPrototype>> Antags;
}

using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared.CF.Character;

/// <summary>
/// A group of jobs shown in the character builder's job list.
/// </summary>
[Prototype("cfJobGroup")]
public sealed partial class CFJobGroupPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    /// <summary>
    /// Higher weights are listed first.
    /// </summary>
    [DataField]
    public int Weight;

    [DataField]
    public List<ProtoId<JobPrototype>> Roles = new();
}

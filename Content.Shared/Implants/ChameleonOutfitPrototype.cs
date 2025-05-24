using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared.Implants;

// TODO: Add field for job icon (for agent id)?
// TODO: Think of logic for changing the agent id and stuff, maybe we need more fields here as well?
[Prototype]
public sealed partial class ChameleonOutfitPrototype : IPrototype, IEquipmentLoadout
{
    /// <inheritdoc/>
    [ViewVariables, IdDataField]
    public string ID { get; private set; } = string.Empty;

    // job this prototype is based off of
    [DataField]
    public ProtoId<JobPrototype>? Job;

    [DataField]
    public Dictionary<string, EntProtoId> Equipment { get; set; } = new();

    [DataField]
    public List<EntProtoId> Inhand { get; set; } = new();

    [DataField]
    public Dictionary<string, List<EntProtoId>> Storage { get; set; } = new();
}

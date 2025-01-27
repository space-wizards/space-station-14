using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Heretic.Prototypes;

[Serializable, NetSerializable, DataDefinition]
[Prototype("hereticKnowledge")]
public sealed partial class HereticKnowledgePrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField] public string? Path;

    [DataField] public int Stage = 1;

    /// <summary>
    ///     Indicates that this should not be on a main branch.
    /// </summary>
    [DataField] public bool SideKnowledge = false;

    /// <summary>
    ///     What event should be raised
    /// </summary>
    [DataField] public object? Event;

    /// <summary>
    ///     What rituals should be given
    /// </summary>
    [DataField] public List<ProtoId<HereticRitualPrototype>>? RitualPrototypes;

    /// <summary>
    ///     What actions should be given
    /// </summary>
    [DataField] public List<EntProtoId>? ActionPrototypes;

    /// <summary>
    ///     Used for codex
    /// </summary>
    [DataField] public string LocName = string.Empty;

    /// <summary>
    ///     Used for codex
    /// </summary>
    [DataField] public string LocDesc = string.Empty;
}

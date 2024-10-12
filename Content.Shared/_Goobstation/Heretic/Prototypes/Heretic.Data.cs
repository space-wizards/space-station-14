using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

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

    [DataField] public object? Event;

    [DataField] public List<ProtoId<HereticRitualPrototype>>? RitualPrototypes;

    [DataField] public List<EntProtoId>? ActionPrototypes;
}

[Serializable, NetSerializable, DataDefinition]
[Prototype("hereticRitual")]
public sealed partial class HereticRitualPrototype : IPrototype, ICloneable
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField] public string Name = "heretic-ritual-unknown";

    /// <summary>
    ///     How many entitites with specific names are required for the ritual?
    /// </summary>
    [DataField] public Dictionary<string, int>? RequiredEntityNames;

    /// <summary>
    ///     How many items with certain tags are required for the ritual?
    /// </summary>
    [DataField] public Dictionary<ProtoId<TagPrototype>, int>? RequiredTags;

    /// <summary>
    ///     Is there a custom behavior that needs to be executed?
    /// </summary>
    [DataField] public List<RitualCustomBehavior>? CustomBehaviors;

    /// <summary>
    ///     How many other entities will be created from the ritual?
    /// </summary>
    [DataField] public Dictionary<EntProtoId, int>? Output;
    /// <summary>
    ///     What event will be raised on success?
    /// </summary>
    [DataField] public object? OutputEvent;
    /// <summary>
    ///     What knowledge will be given on success?
    /// </summary>
    [DataField] public ProtoId<HereticKnowledgePrototype>? OutputKnowledge;

    /// <summary>
    ///     Icon for ritual in radial menu.
    /// </summary>
    [DataField] public SpriteSpecifier Icon = new SpriteSpecifier.Rsi(new("_Goobstation/Heretic/amber_focus.rsi"), "icon");

    /// <remarks> Please use this instead of editing the prototype. Shit WILL break if you don't. </remarks>
    public object Clone()
    {
        return new HereticRitualPrototype()
        {
            ID = ID,
            Name = Name,
            RequiredEntityNames = RequiredEntityNames,
            RequiredTags = RequiredTags,
            CustomBehaviors = CustomBehaviors,
            Output = Output,
            OutputEvent = OutputEvent,
            OutputKnowledge = OutputKnowledge,
            Icon = Icon
        };
    }
}

[Serializable, NetSerializable, DataDefinition] public sealed partial class EventHereticAscension : EntityEventArgs { }
[Serializable, NetSerializable, DataDefinition] public sealed partial class EventHereticRerollTargets : EntityEventArgs { }
[Serializable, NetSerializable, DataDefinition] public sealed partial class EventHereticUpdateTargets : EntityEventArgs { }

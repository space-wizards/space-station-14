using Content.Shared.Chat.Prototypes;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.Markings;

[DataDefinition]
public sealed partial class MarkingEmoteData
{
    [DataField(required: true)]
    public ProtoId<EmotePrototype> EmotePrototype = default!;

    [DataField] public HashSet<ProtoId<MarkingPrototype>>? RequiredMarkings = null;
    [DataField] public List<ProtoId<MarkingPrototype>>? RequiredMarkingsAny = null;
}

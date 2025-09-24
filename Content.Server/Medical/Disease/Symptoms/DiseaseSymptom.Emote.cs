using Robust.Shared.Prototypes;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Medical.Disease;
using Content.Server.Chat.Systems;

namespace Content.Server.Medical.Disease.Symptoms;

[DataDefinition]
public sealed partial class SymptomEmote : SymptomBehavior
{
    /// <summary>
    /// Emote prototype to execute.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<EmotePrototype>? EmoteId { get; private set; }
}

public sealed partial class SymptomEmote
{
    [Dependency] private readonly ChatSystem _chat = default!;

    /// <summary>
    /// Triggers an emote on the carrier.
    /// </summary>
    public override void OnSymptom(EntityUid uid, DiseasePrototype disease)
    {
        if (EmoteId is not { } emoteProto)
            return;

        _chat.TryEmoteWithChat(uid, emoteProto, ignoreActionBlocker: true);
    }
}

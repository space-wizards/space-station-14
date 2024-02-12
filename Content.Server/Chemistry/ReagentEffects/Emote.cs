using Content.Server.Chat.Systems;
using Content.Server.Chat.V2;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Chemistry.ReagentEffects;

/// <summary>
///     Tries to force someone to emote (scream, laugh, etc).
/// </summary>
[UsedImplicitly]
public sealed partial class Emote : ReagentEffect
{
    [DataField("emote", customTypeSerializer: typeof(PrototypeIdSerializer<EmotePrototype>))]
    public string? EmoteId;

    [DataField]
    public bool ShowInChat;

    // JUSTIFICATION: Emoting is flavor, so same reason popup messages are not in here.
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;

    public override void Effect(ReagentEffectArgs args)
    {
        if (EmoteId == null)
            return;

        var emote = args.EntityManager.System<ChatSystem>();
        if (ShowInChat)
            emote.TryEmoteWithChat(args.SolutionEntity, EmoteId);
        else
            emote.TryEmoteWithoutChat(args.SolutionEntity, EmoteId);
    }
}

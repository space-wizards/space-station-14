using Content.Server.Chat.Systems;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Chemistry.ReagentEffects;

/// <summary>
///     Tries to force someone to emote (scream, laugh, etc).
/// </summary>
[UsedImplicitly]
public sealed class Emote : ReagentEffect
{
    [DataField("emote", customTypeSerializer: typeof(PrototypeIdSerializer<EmotePrototype>))]
    public string? EmoteId;

    [DataField("showInChat")]
    public bool ShowInChat;

    public override void Effect(ReagentEffectArgs args)
    {
        if (EmoteId == null)
            return;

        var chatSys = args.EntityManager.System<ChatSystem>();
        if (ShowInChat)
            chatSys.TryEmoteWithChat(args.SolutionEntity, EmoteId, ChatTransmitRange.GhostRangeLimit);
        else
            chatSys.TryEmoteWithoutChat(args.SolutionEntity, EmoteId);

    }
}

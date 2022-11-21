using Content.Server.Chat.Systems;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Chemistry.ReagentEffects;

/// <summary>
///     Forces someone to audibly emote (scream, laugh, etc).
/// </summary>
[UsedImplicitly]
public sealed class Emote : ReagentEffect
{
    [DataField("emote", customTypeSerializer: typeof(PrototypeIdSerializer<EmotePrototype>))]
    public string? EmoteId;

    public override void Effect(ReagentEffectArgs args)
    {
        if (EmoteId == null)
            return;

        var chatSys = args.EntityManager.System<ChatSystem>();
        chatSys.TryEmoteWithoutChat(args.SolutionEntity, EmoteId);
    }
}

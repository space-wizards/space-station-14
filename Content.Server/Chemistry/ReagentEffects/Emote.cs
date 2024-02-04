using Content.Server.Chat.Systems;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Content.Shared.Chemistry.Components;

namespace Content.Server.Chemistry.ReagentEffects;

/// <summary>
///     Tries to force someone to emote (scream, laugh, etc).
/// </summary>
[UsedImplicitly]
public sealed partial class Emote : ReagentEffect
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    [DataField("emote", customTypeSerializer: typeof(PrototypeIdSerializer<EmotePrototype>))]
    public string? EmoteId;

    [DataField]
    TimeSpan? Cooldown;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    TimeSpan NextEmote = TimeSpan.Zero;

    [DataField]
    public bool ShowInChat;

    // JUSTIFICATION: Emoting is flavor, so same reason popup messages are not in here.
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;

    public override void Effect(ReagentEffectArgs args)
    {
        if (EmoteId == null)
            return;
        
        if (effect.cooldown != null &&
            _gameTiming.CurTime <= effect.cooldown + proto.Cooldown)
            return;

        var chatSys = args.EntityManager.System<ChatSystem>();
        if (ShowInChat)
            chatSys.TryEmoteWithChat(args.SolutionEntity, EmoteId, ChatTransmitRange.GhostRangeLimit);
        else
            chatSys.TryEmoteWithoutChat(args.SolutionEntity, EmoteId);

    }
}

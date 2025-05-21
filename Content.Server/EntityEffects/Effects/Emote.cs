using Content.Server.Chat.Systems;
using Content.Shared.Chat.Prototypes;
using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.EntityEffects.Effects;

/// <summary>
///     Tries to force someone to emote (scream, laugh, etc). Still respects whitelists/blacklists and other limits unless specially forced.
/// </summary>
[UsedImplicitly]
public sealed partial class Emote : EntityEffect
{
    /// <summary>
    ///     The emote the entity will preform.
    /// </summary>
    [DataField("emote", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EmotePrototype>))]
    public string EmoteId;

    /// <summary>
    ///     If the emote should be recorded in chat.
    /// </summary>
    [DataField]
    public bool ShowInChat;

    /// <summary>
    ///     If the forced emote will be listed in the guidebook.
    /// </summary>
    [DataField]
    public bool ShowInGuidebook;

    /// <summary>
    ///     If true, the entity will preform the emote even if they normally can't.
    /// </summary>
    [DataField]
    public bool Force = false;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        if (!ShowInGuidebook)
            return null; // JUSTIFICATION: Emoting is mostly flavor, so same reason popup messages are not in here.

        return Loc.GetString("reagent-effect-guidebook-emote", ("chance", Probability), ("emote", EmoteId));
    }

    public override void Effect(EntityEffectBaseArgs args)
    {
        var chatSys = args.EntityManager.System<ChatSystem>();
        if (ShowInChat)
            chatSys.TryEmoteWithChat(args.TargetEntity, EmoteId, ChatTransmitRange.GhostRangeLimit, forceEmote: Force);
        else
            chatSys.TryEmoteWithoutChat(args.TargetEntity, EmoteId);
    }
}

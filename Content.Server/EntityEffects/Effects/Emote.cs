using Content.Server.Chat.Systems;
using Content.Shared.Chat.Prototypes;
using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.EntityEffects.Effects;

/// <summary>
///     How an emote will show up, in chat with a popup, only as a popup, or neither
/// </summary>
public enum EmoteVisiblity : byte
{
    ChatAndPopup,
    Popup,
    Invisible
}

/// <summary>
///     Tries to force someone to emote (scream, laugh, etc). Still respects whitelists/blacklists and other limits of the specified emote unless forced.
/// </summary>
[UsedImplicitly]
public sealed partial class Emote : EntityEffect
{
    [DataField("emote", customTypeSerializer: typeof(PrototypeIdSerializer<EmotePrototype>))]
    public string? EmoteId;
    /// <summary>
    ///     Determines if the emote text will show up in chat with a popup, be popup only, or be invisible
    /// </summary>
    [DataField]
    public EmoteVisiblity Visibility = EmoteVisiblity.Popup;

    [DataField]
    public bool Force = false;

    // JUSTIFICATION: Emoting is flavor, so same reason popup messages are not in here.
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (EmoteId == null)
            return;

        var chatSys = args.EntityManager.System<ChatSystem>();
        switch (Visibility)
        {
            case EmoteVisiblity.ChatAndPopup:
                chatSys.TryEmoteWithChat(args.TargetEntity, EmoteId, ChatTransmitRange.GhostRangeLimit, forceEmote: Force);
                break;
            case EmoteVisiblity.Popup:
                chatSys.TryEmoteWithChat(args.TargetEntity, EmoteId, ChatTransmitRange.HideChat, forceEmote: Force);
                break;
            case EmoteVisiblity.Invisible:
                chatSys.TryEmoteWithoutChat(args.TargetEntity, EmoteId);
                break;
        }
    }
}

using Content.Server.Administration;
using Content.Server.Chat.Managers;
using Content.Server.Popups;
using Content.Server.Prayer;
using Content.Server.Tips;
using Content.Shared.Administration;
using Content.Shared.Chat;
using Content.Shared.Popups;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;

namespace Content.Server.Toolshed.Commands.Misc;

[ToolshedCommand, AdminCommand(AdminFlags.Fun)]
public sealed class MsgCommand : ToolshedCommand
{
    [Dependency] private IChatManager _chatManager = default!;

    private PrayerSystem? _prayer;
    private PopupSystem? _popup;
    private TipsSystem? _tips;

    [CommandImplementation("subtle")]
    public IEnumerable<EntityUid> Subtle(IInvocationContext ctx, [PipedArgument] IEnumerable<EntityUid> targets, string popup, string message)
    {
        _prayer ??= GetSys<PrayerSystem>();

        foreach (var ent in targets)
        {
            if (!TryComp<ActorComponent>(ent, out var actor))
                continue;

            _prayer.SendSubtleMessage(actor.PlayerSession, ctx.Session, message, popup);

            yield return ent;
        }
    }

    [CommandImplementation("chat")]
    public IEnumerable<EntityUid> Chat([PipedArgument] IEnumerable<EntityUid> targets, string message)
    {
        foreach (var ent in targets)
        {
            if (!TryComp<ActorComponent>(ent, out var actor))
                continue;

            _chatManager.ChatMessageToOne(ChatChannel.Local, message, message, EntityUid.Invalid, false, actor.PlayerSession.Channel);
            yield return ent;
        }
    }

    [CommandImplementation("popup")]
    public IEnumerable<EntityUid> Popup([PipedArgument] IEnumerable<EntityUid> targets, string popup, PopupType type, bool recipientOnly)
    {
        _popup ??= GetSys<PopupSystem>();

        foreach (var ent in targets)
        {
            if (recipientOnly)
                _popup.PopupEntity(popup, ent, ent, type); // If recipientOnly, show the popup only to the recipient.
            else
                _popup.PopupEntity(popup, ent, type); // Otherwise, show it to everyone within PVS of the target entity.
            yield return ent;
        }
    }

    [CommandImplementation("tippy")]
    public IEnumerable<EntityUid> Tippy([PipedArgument] IEnumerable<EntityUid> targets, string message, EntProtoId prototype, float speakTime, float slideTime, float waddleInterval)
    {
        _tips ??= GetSys<TipsSystem>();

        foreach (var ent in targets)
        {
            if (!TryComp<ActorComponent>(ent, out var actor))
                continue;

            _tips.SendTippy(actor.PlayerSession, message, prototype, speakTime, slideTime, waddleInterval);

            yield return ent;
        }
    }
}

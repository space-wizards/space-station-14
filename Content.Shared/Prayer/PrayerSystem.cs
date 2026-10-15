using Content.Shared.Administration.Logs;
using Content.Shared.Bible.Components;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared.Prayer;

public abstract partial class PrayerSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private INetManager _net = default!;

    [SubscribeLocalEvent]
    private void AddPrayVerb(Entity<PrayableComponent> ent, ref GetVerbsEvent<ActivationVerb> args)
    {
        var user = args.User;

        // if the user doesn't have an actor then don't add the verb
        if (!TryComp(user, out ActorComponent? actor))
            return;

        // this is to prevent ghosts from using it
        if (!args.CanInteract)
            return;

        var prayerVerb = new ActivationVerb
        {
            Text = Loc.GetString(ent.Comp.Verb),
            Icon = ent.Comp.VerbImage,
            Act = () =>
            {
                if (ent.Comp.BibleUserOnly && !HasComp<BibleUserComponent>(user))
                {
                    _popup.PopupEntity(Loc.GetString("prayer-popup-notify-pray-locked"),
                        ent.Owner,
                        actor.PlayerSession,
                        PopupType.Large);
                    return;
                }

                OpenPrayerDialog(ent, user, actor.PlayerSession);
            },
            Impact = LogImpact.Low,
        };
        args.Verbs.Add(prayerVerb);
    }

    protected virtual void OpenPrayerDialog(Entity<PrayableComponent> ent, EntityUid user, ICommonSession session) { }

    /// <summary>
    /// Subtly messages a player by giving them a popup and a chat message. Server only.
    /// </summary>
    /// <param name="target">The IPlayerSession that you want to send the message to</param>
    /// <param name="source">The IPlayerSession that sent the message</param>
    /// <param name="messageString">The main message sent to the player via the chatbox</param>
    /// <param name="popupMessage">The popup to notify the player, also prepended to the messageString</param>
    public void SendSubtleMessage(ICommonSession target,
        ICommonSession? source,
        string messageString,
        string popupMessage)
    {
        if (!_net.IsServer || target.AttachedEntity is not { } targetEntity)
            return;

        var message = popupMessage == "" ? "" : popupMessage + (messageString == "" ? "" : $" \"{messageString}\"");

        _popup.PopupEntity(popupMessage, targetEntity, target, PopupType.Large);
        _net.ServerSendMessage(new MsgChatMessage
        {
            Message = new ChatMessage(ChatChannel.Local, messageString, message, NetEntity.Invalid, null),
        },
            target.Channel);
        _adminLogger.Add(LogType.AdminMessage,
            LogImpact.Low,
            $"{ToPrettyString(targetEntity):player} received subtle message from {source?.Name ?? "unknown source"}: {message}");
    }
}

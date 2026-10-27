using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.Popups;
using Content.Shared.Bible.Components;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Popups;
using Content.Shared.Prayer;
using Robust.Shared.Player;

namespace Content.Server.Prayer;

/// <inheritdoc/>
public sealed partial class ServerPrayerSystem : PrayerSystem
{
    [Dependency] private IAdminLogManager _adminLogger = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private QuickDialogSystem _quickDialog = default!;

    /// <inheritdoc/>
    public override void Pray(Entity<PrayableComponent> ent, ICommonSession sender, string message)
    {
        if (sender.AttachedEntity is null)
            return;

        _popup.PopupEntity(Loc.GetString(ent.Comp.SentMessage),
            sender.AttachedEntity.Value,
            sender,
            PopupType.Medium);

        _chat.SendAdminAnnouncement($"{Loc.GetString(ent.Comp.NotificationPrefix)} <{sender.Name}>: {message}");
        _adminLogger.Add(LogType.AdminMessage,
            LogImpact.Low,
            $"{ToPrettyString(sender.AttachedEntity.Value):player} sent prayer ({Loc.GetString(ent.Comp.NotificationPrefix)}): {message}");
    }

    /// <summary>
    /// Subtly messages a player by giving them a popup and a chat message.
    /// </summary>
    public void SendSubtleMessage(ICommonSession target, ICommonSession? source, string messageString, string popupMessage)
    {
        if (target.AttachedEntity is not { } targetEntity)
            return;

        var message = popupMessage == "" ? "" : popupMessage + (messageString == "" ? "" : $" \"{messageString}\"");

        _popup.PopupEntity(popupMessage, targetEntity, target, PopupType.Large);
        _chat.ChatMessageToOne(ChatChannel.Local, messageString, message, EntityUid.Invalid, false, target.Channel);
        _adminLogger.Add(LogType.AdminMessage,
            LogImpact.Low,
            $"{ToPrettyString(targetEntity):player} received subtle message from {source?.Name ?? "unknown source"}: {message}");
    }

    protected override void OpenPrayerDialog(Entity<PrayableComponent> ent, EntityUid user, ICommonSession session)
    {
        _quickDialog.OpenDialog(session,
            Loc.GetString(ent.Comp.Verb),
            Loc.GetString("prayer-popup-notify-pray-ui-message"),
            (string message) =>
            {
                // Make sure the player still controls the same entity and the Prayable entity+component still exist
                if (session.AttachedEntity != user
                    || !TryComp<ActorComponent>(user, out var actor)
                    || actor.PlayerSession != session
                    || !TryComp<PrayableComponent>(ent.Owner, out var prayable))
                    return;

                if (prayable.BibleUserOnly && !HasComp<BibleUserComponent>(user))
                    return;

                Pray((ent.Owner, prayable), session, message);
            });
    }
}

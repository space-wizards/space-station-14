using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.Popups;
using Content.Shared.Database;
using Content.Shared.Popups;
using Content.Shared.Prayer;
using Robust.Shared.Player;
using Content.Shared.Bible.Components;

namespace Content.Server.Prayer;

/// <summary>
/// System to handle subtle messages and praying
/// </summary>
/// <remarks>
/// Rain is a professional developer and this did not take 2 PRs to fix subtle messages
/// </remarks>
public sealed partial class ServerPrayerSystem : PrayerSystem
{
    [Dependency] private IAdminLogManager _adminLogger = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private QuickDialogSystem _quickDialog = default!;

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

    /// <summary>
    /// Sends a message to the admin channel with a message and username
    /// </summary>
    /// <param name="sender">The IPlayerSession who sent the original message</param>
    /// <param name="ent">Prayable entity used to make the prayer</param>
    /// <param name="message">Message to be sent to the admin chat</param>
    /// <remarks>
    /// You may be wondering, "Why the admin chat, specifically? Nobody even reads it!"
    /// Exactly.
    ///  </remarks>
    public void Pray(Entity<PrayableComponent> ent, ICommonSession sender, string message)
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
}

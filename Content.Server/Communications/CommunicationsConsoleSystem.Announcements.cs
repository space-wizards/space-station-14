using System.Globalization;
using Content.Server.Access.Systems;
using Content.Server.Chat.V2;
using Content.Server.Interaction;
using Content.Server.Popups;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.CCVar;
using Content.Shared.Chat.V2;
using Content.Shared.Communications;
using Content.Shared.Database;
using Content.Shared.Emag.Components;

namespace Content.Server.Communications;

public sealed partial class CommunicationsConsoleSystem
{
    [Dependency] private readonly IdCardSystem _idCardSystem = default!;
    [Dependency] private readonly CommunicationsConsoleSystem _comms = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    private int _maxAnnouncementMessageLength;

    private void InitializeAnnouncements()
    {
        SubscribeNetworkEvent<AttemptCommunicationConsoleAnnouncementMessage>(ev => HandleAttemptAnnouncementEvent(GetEntity(ev.Console), GetEntity(ev.Sender),  ev.Message));

        _maxAnnouncementMessageLength = _cfg.GetCVar(CCVars.ChatMaxAnnouncementLength);
        _cfg.OnValueChanged(CCVars.ChatMaxAnnouncementLength, maxLen => _maxAnnouncementMessageLength = maxLen);
    }

    private void HandleAttemptAnnouncementEvent(EntityUid console, EntityUid sender, string message)
    {
        if (!TryComp<CommunicationsConsoleComponent>(console, out var comp))
        {
            _popupSystem.PopupEntity(Loc.GetString("comms-console-announcement-failed"), console, sender);

            return;
        }

        if (message.Length > _maxAnnouncementMessageLength)
        {
            _popupSystem.PopupEntity(Loc.GetString("comms-console-max-message-length"), console, sender);

            return;
        }

        if (comp.AnnouncementCooldownRemaining <= 0f)
        {
            _popupSystem.PopupEntity(Loc.GetString("comms-console-cooldown-remaining"), console, sender);

            return;
        }

        if (!_interaction.InRangeUnobstructed(console, sender))
        {
            _popupSystem.PopupEntity(Loc.GetString("comms-console-too-far-from-console"), console, sender);

            return;
        }

        if (!HasComp<EmaggedComponent>(console) && TryComp<AccessReaderComponent>(console, out var access) && !_accessReaderSystem.IsAllowed(sender, console, access))
        {
            _popupSystem.PopupEntity(Loc.GetString("comms-console-permission-denied"), console, sender);

            return;
        }

        SendCommunicationConsoleAnnouncement(console, sender, message, comp);
    }

    public void SendCommunicationConsoleAnnouncement(EntityUid console, EntityUid sender, string message, CommunicationsConsoleComponent comp)
    {
        var author = Loc.GetString("comms-console-announcement-unknown-sender");

        if (!_idCardSystem.TryFindIdCard(sender, out var id))
        {
            author =
                $"{id.Comp.FullName} ({CultureInfo.CurrentCulture.TextInfo.ToTitleCase(id.Comp.JobTitle ?? string.Empty)})"
                    .Trim();
        }

        comp.AnnouncementCooldownRemaining = comp.Delay;
        _comms.UpdateCommsConsoleInterface(console, comp);

        message = SharedChatSystem.SanitizeAnnouncement(message);

        var ev = new CommunicationConsoleAnnouncementEvent(sender, console, message);
        RaiseLocalEvent(ref ev);

        Loc.TryGetString(comp.Title, out var title);
        title ??= comp.Title;

        message += "\n" + Loc.GetString("comms-console-announcement-sent-by") + " " + author;

        if (comp.Global)
        {
            _chat.DispatchGlobalAnnouncement(message, title, announcementSound: comp.Sound, colorOverride: comp.Color);

            _adminLogger.Add(LogType.Chat, LogImpact.Low,
                $"{ToPrettyString(sender):player} has sent the following global announcement: {message}");
        }
        else
        {
            _chat.DispatchStationAnnouncement(console, message, title, colorOverride: comp.Color);

            _adminLogger.Add(LogType.Chat, LogImpact.Low,
                $"{ToPrettyString(sender):player} has sent the following station announcement: {message}");
        }
    }
}

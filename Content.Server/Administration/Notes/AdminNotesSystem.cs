using Content.Server.Administration.Commands;
using Content.Server.Chat.Managers;
using Content.Server.EUI;
using Content.Shared.Database;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Administration.Notes;

public sealed class AdminNotesSystem : EntitySystem, IPostInjectInit
{
    [Dependency] private readonly IConsoleHost _console = default!;
    [Dependency] private readonly IAdminNotesManager _notes = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly EuiManager _euis = default!;

    public const string SawmillId = "admin.notes_system";
    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GetVerbsEvent<Verb>>(AddVerbs);
        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    private void AddVerbs(GetVerbsEvent<Verb> ev)
    {
        if (EntityManager.GetComponentOrNull<ActorComponent>(ev.User) is not {PlayerSession: var user} ||
            EntityManager.GetComponentOrNull<ActorComponent>(ev.Target) is not {PlayerSession: var target})
        {
            return;
        }

        if (!_notes.CanView(user))
        {
            return;
        }

        var verb = new Verb
        {
            Text = Loc.GetString("admin-notes-verb-text"),
            Category = VerbCategory.Admin,
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/examine.svg.192dpi.png")),
            Act = () => _console.RemoteExecuteCommand(user, $"{OpenAdminNotesCommand.CommandName} \"{target.UserId}\""),
            Impact = LogImpact.Low
        };

        ev.Verbs.Add(verb);
    }

    private async void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus != SessionStatus.Connected)
            return;

        var messages = await _notes.GetNewMessages(e.Session.UserId);
        var watchlists = await _notes.GetActiveWatchlists(e.Session.UserId);

        if (!_playerManager.TryGetPlayerData(e.Session.UserId, out var playerData))
        {
            _sawmill.Error($"Could not get player data for ID {e.Session.UserId}");
        }

        var username = playerData?.UserName ?? e.Session.UserId.ToString();
        foreach (var watchlist in watchlists)
        {
            _chat.SendAdminAlert(Loc.GetString("admin-notes-watchlist", ("player", username), ("message", watchlist.Message)));
        }

        foreach (var message in messages)
        {
            var messageString = Loc.GetString("admin-notes-new-message", ("admin", message.CreatedBy?.LastSeenUserName ?? "[System]"), ("message", message.Message));
            // Only open the popup if the user hasn't seen it yet
            if (!message.Seen)
            {
                var ui = new AdminMessageEui();
                _euis.OpenEui(ui, e.Session);
                ui.SetMessage(message);
            }
            // Send the message anyway
            _chat.DispatchServerMessage(e.Session, messageString);
        }
    }

    public void PostInject()
    {
        _sawmill = _logManager.GetSawmill(SawmillId);
    }
}

using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Configuration;

namespace Content.Server.Motd;

public sealed class MOTDSystem : EntitySystem
{
    #region Dependencies
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    #endregion Dependencies

    private string _messageOfTheDay = "";

    public override void Initialize()
    {
        base.Initialize();
        _messageOfTheDay = _configurationManager.GetCVar(CCVars.MOTD);
        _configurationManager.OnValueChanged(CCVars.MOTD, _onMOTDChanged);
        SubscribeLocalEvent<GameRunLevelChangedEvent>(_onGameRunLevelChanged);
        SubscribeLocalEvent<PlayerJoinedLobbyEvent>(_onPlayerJoinedLobby);
    }

    public override void Shutdown()
    {
        _configurationManager.UnsubValueChanged(CCVars.MOTD, _onMOTDChanged);
        base.Shutdown();
    }

    /// <summary>
    /// Sends the message of the day, if any, to all connected players.
    /// </summary>
    public void TrySendMOTD()
    {
        if (string.IsNullOrEmpty(_messageOfTheDay))
            return;
        
        var wrappedMessage = Loc.GetString("motd-wrap-message", ("motd", _messageOfTheDay));
        _chatManager.ChatMessageToAll(ChatChannel.Server, _messageOfTheDay, wrappedMessage, source: EntityUid.Invalid, hideChat: false, recordReplay: true);
    }

    /// <summary>
    /// Sends the message of the day, if any, to a specific player.
    /// </summary>
    public void TrySendMOTD(IPlayerSession player)
    {
        if (string.IsNullOrEmpty(_messageOfTheDay))
            return;
        
        var wrappedMessage = Loc.GetString("motd-wrap-message", ("motd", _messageOfTheDay));
        _chatManager.ChatMessageToOne(ChatChannel.Server, _messageOfTheDay, wrappedMessage, source: EntityUid.Invalid, hideChat: false, client: player.ConnectedClient);
    }

    /// <summary>
    /// Sends the message of the day, if any, to a specific players console and chat.
    /// </summary>
    /// <remarks>
    /// This is used by the MOTD console command because we can't tell whether the player is using `console or /console so we send the message to both.
    /// </remarks>
    public void TrySendMOTD(IConsoleShell shell)
    {
        if (string.IsNullOrEmpty(_messageOfTheDay))
            return;
        
        var wrappedMessage = Loc.GetString("motd-wrap-message", ("motd", _messageOfTheDay));
        shell.WriteLine(wrappedMessage);
        if (shell.Player is IPlayerSession player)
            _chatManager.ChatMessageToOne(ChatChannel.Server, _messageOfTheDay, wrappedMessage, source: EntityUid.Invalid, hideChat: false, client: player.ConnectedClient);
    }

    private void _onPlayerJoinedLobby(PlayerJoinedLobbyEvent ev)
    {
        TrySendMOTD(ev.PlayerSession);
    }

    private void _onGameRunLevelChanged(GameRunLevelChangedEvent ev)
    {
        switch(ev.New)
        {
            case GameRunLevel.PreRoundLobby:
                TrySendMOTD();
                break;
        }
    }

    private void _onMOTDChanged(string val)
    {
        if (val == _messageOfTheDay)
            return;
        
        _messageOfTheDay = val;
        TrySendMOTD();
    }
}

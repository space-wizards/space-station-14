using Content.Server.Administration.Managers;
using Content.Server.Afk.Events;
using Content.Server.GameTicking;
using Content.Shared.CCVar;
using Content.Shared.Instruments;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Input;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Afk;

/// <summary>
/// Actively checks for AFK players regularly and issues an event whenever they go afk.
/// </summary>
public sealed partial class AFKSystem : EntitySystem
{
    [Dependency] private IAdminManager _admin = default!;
    [Dependency] private IAfkManager _afkManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private GameTicker _ticker = default!;
    [Dependency] private IConfigurationManager _cfg = default!;

    /// <summary>
    /// Don't need to do it every tick.
    /// </summary>
    private const float CheckDelay = 10f;

    private TimeSpan _nextCheckTime;
    private float _adminAfkTime;
    private float _afkTime;

    private readonly HashSet<ICommonSession> _afkPlayers = new();

    public override void Initialize()
    {
        base.Initialize();
        _playerManager.PlayerStatusChanged += OnPlayerChange;
        _afkManager.PlayerDidActionEvent += OnPlayerAction;
        Subs.CVar(_cfg, CCVars.AfkTime, OnAfkTimeChanged, true);
        Subs.CVar(_cfg, CCVars.AdminAfkTime, OnAdminAfkTimeChanged, true);

        SubscribeNetworkEvent<FullInputCmdMessage>(HandleInputCmd);
        // Temporary until instruments use BUIs like normal.
        SubscribeNetworkEvent<InstrumentStartMidiEvent>(HandleMidiStart);
        SubscribeNetworkEvent<InstrumentStopMidiEvent>(HandleMidiStop);
        SubscribeNetworkEvent<InstrumentMidiEventEvent>(HandleMidiEvent);
        SubscribeNetworkEvent<InstrumentSetChannelsEvent>(HandleMidiSetChannels);
        SubscribeLocalEvent<BoundUserInterfaceMessageReceivedEvent>(OnBoundUiMessageReceived);
    }

    private void HandleInputCmd(FullInputCmdMessage msg, EntitySessionEventArgs args)
    {
        if (!_playerManager.KeyMap.TryGetKeyFunction(msg.InputFunctionId, out _))
            return;

        if (!Enum.IsDefined(msg.State))
            return;

        _afkManager.PlayerDidAction(args.SenderSession);
    }

    private void HandleMidiStart(InstrumentStartMidiEvent msg, EntitySessionEventArgs args)
    {
        _afkManager.PlayerDidAction(args.SenderSession);
    }

    private void HandleMidiStop(InstrumentStopMidiEvent msg, EntitySessionEventArgs args)
    {
        _afkManager.PlayerDidAction(args.SenderSession);
    }

    private void HandleMidiEvent(InstrumentMidiEventEvent msg, EntitySessionEventArgs args)
    {
        _afkManager.PlayerDidAction(args.SenderSession);
    }

    private void HandleMidiSetChannels(InstrumentSetChannelsEvent msg, EntitySessionEventArgs args)
    {
        _afkManager.PlayerDidAction(args.SenderSession);
    }

    private void OnBoundUiMessageReceived(ref BoundUserInterfaceMessageReceivedEvent args)
    {
        if (!TryComp<ActorComponent>(args.Actor, out var actor))
            return;

        _afkManager.PlayerDidAction(actor.PlayerSession);
    }

    private void OnPlayerChange(object? sender, SessionStatusEventArgs e)
    {
        switch (e.NewStatus)
        {
            case SessionStatus.Disconnected:
                _afkPlayers.Remove(e.Session);
                break;
        }
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _afkPlayers.Clear();
        _playerManager.PlayerStatusChanged -= OnPlayerChange;
        _afkManager.PlayerDidActionEvent -= OnPlayerAction;
        _cfg.UnsubValueChanged(CCVars.AfkTime, OnAfkTimeChanged);
    }

    private void OnPlayerAction(ICommonSession session)
    {
        if (!_afkPlayers.Remove(session))
            return;

        var ev = new UnAFKEvent(session);
        RaiseLocalEvent(ref ev);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // If disabled then ignore flagging anything (including admins).
        if (_afkTime <= 0)
            return;

        if (_timing.CurTime < _nextCheckTime)
            return;

        _nextCheckTime = _timing.CurTime + TimeSpan.FromSeconds(CheckDelay);
        // Flag everyone as non-afk unless the game is running.
        if (!CanFlagAfk(_ticker.RunLevel))
        {
            MarkAllInGamePlayersActive();

            // Technically we can double-fire afk events here but shouldn't matter.
            // We just want AFK timers reset by the time we get into round.
            return;
        }

        foreach (var pSession in Filter.GetAllPlayers())
        {
            if (pSession.Status != SessionStatus.InGame)
                continue;

            var isAfk = _afkManager.IsAfk(pSession);

            // If admin afk timer disabled then ignore them.
            if (_adminAfkTime == 0f && _admin.IsAdmin(pSession))
            {
                isAfk = false;
            }

            if (isAfk && _afkPlayers.Add(pSession))
            {
                var ev = new AFKEvent(pSession);
                RaiseLocalEvent(ref ev);
                continue;
            }

            if (!isAfk && _afkPlayers.Remove(pSession))
            {
                var ev = new UnAFKEvent(pSession);
                RaiseLocalEvent(ref ev);
            }
        }
    }

    private static bool CanFlagAfk(GameRunLevel runLevel)
    {
        return runLevel == GameRunLevel.InRound;
    }

    private void OnAdminAfkTimeChanged(float value)
    {
        _adminAfkTime = value;
    }

    private void OnAfkTimeChanged(float value)
    {
        var wasEnabled = _afkTime > 0;
        var isEnabled = value > 0;

        // Reset AFK timers if we turn it on / off
        if (wasEnabled != isEnabled)
            MarkAllInGamePlayersActive();

        _afkTime = value;
    }

    private void MarkAllInGamePlayersActive()
    {
        foreach (var pSession in Filter.GetAllPlayers())
        {
            if (pSession.Status != SessionStatus.InGame)
                continue;

            _afkManager.PlayerDidAction(pSession);
        }
    }
}

using Content.Server.Afk.Events;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.EUI;
using Content.Shared.Afk;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Afk;

/// <summary>
/// Once a player is AFK handles confirmation and subsequent disconnection.
/// </summary>
public sealed partial class AfkConfirmSystem : EntitySystem
{
    [Dependency] private IAfkManager _afkManager = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private EuiManager _eui = default!;
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private IPlayerManager _players = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IAdminLogManager _adminLogger = default!;

    private readonly Dictionary<ICommonSession, AfkConfirmation> _confirmations = new();
    private readonly Dictionary<ICommonSession, AfkConfirmation> _tempConfirmation = new();

    public override void Initialize()
    {
        base.Initialize();

        // Unafking does NOT clear it, require them to confirm via the window so they don't just random mash buttons.
        SubscribeLocalEvent<AFKEvent>(OnAfk);
        _players.PlayerStatusChanged += OnPlayerStatusChanged;
        _cfg.OnValueChanged(CCVars.AfkTime, OnAfkTimeChanged);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        foreach (var confirmation in _confirmations.Values)
        {
            confirmation.Eui?.Close();
        }

        _confirmations.Clear();
        _players.PlayerStatusChanged -= OnPlayerStatusChanged;
        _cfg.UnsubValueChanged(CCVars.AfkTime, OnAfkTimeChanged);
    }

    private void OnAfk(ref AFKEvent ev)
    {
        TryStartConfirmation(ev.Session);
    }

    /// <summary>
    /// Starts an AFK confirmation prompt if none exists.
    /// </summary>
    /// <param name="session">Session to check.</param>
    /// <param name="requireAttached">Are they required to be attached to an entity.</param>
    /// <returns></returns>
    public bool TryStartConfirmation(ICommonSession session, bool requireAttached = false)
    {
        var timeout = _cfg.GetCVar(CCVars.AfkConfirmTimeout);

        if (timeout <= 0
            || session.Status == SessionStatus.Disconnected
            || _confirmations.ContainsKey(session)
            || requireAttached && session.AttachedEntity == null)
        {
            return false;
        }

        var deadline = _timing.RealTime + TimeSpan.FromSeconds(timeout);
        var eui = new AfkConfirmEui(this, deadline);
        _confirmations[session] = new AfkConfirmation(eui, deadline);
        _eui.OpenEui(eui, session);
        _adminLogger.Add(LogType.Connection, LogImpact.Low,
            $"{session.Name} ({session.UserId}) was shown the AFK confirmation window with {timeout} seconds to respond.");

        var message = Loc.GetString("afk-system-afk-warning", ("seconds", MathF.Ceiling(timeout)));
        _chat.ChatMessageToOne(ChatChannel.Server, message, message, EntityUid.Invalid, false, session.Channel);
        return true;
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (args.NewStatus == SessionStatus.Disconnected)
            _confirmations.Remove(args.Session);
    }

    private void OnAfkTimeChanged(float value)
    {
        if (value > 0)
            return;

        foreach (var confirmation in _confirmations.Values)
        {
            confirmation.Eui?.Close();
        }

        _confirmations.Clear();
    }

    public void Confirm(ICommonSession session)
    {
        if (!_confirmations.Remove(session))
            return;

        _afkManager.PlayerDidAction(session);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_confirmations.Count == 0)
            return;

        _tempConfirmation.Clear();

        foreach (var (session, c) in _confirmations)
        {
            _tempConfirmation.Add(session, c);
        }

        foreach (var (session, confirmation) in _tempConfirmation)
        {
            if (session.Status == SessionStatus.Disconnected)
            {
                _confirmations.Remove(session);
                continue;
            }

            if (_timing.RealTime < confirmation.Deadline)
                continue;

            _confirmations.Remove(session);
            confirmation.Eui?.Close();
            _adminLogger.Add(LogType.Connection, LogImpact.Medium,
                $"{session.Name} ({session.UserId}) timed out on the AFK confirmation window and was disconnected.");
            session.Channel.Disconnect(Loc.GetString("afk-system-kick-reason"));
        }
    }

    [PublicAPI]
    public bool ClearConfirmation(ICommonSession session)
    {
        if (!_confirmations.Remove(session, out var confirmation))
            return false;

        confirmation.Eui?.Close();
        return true;
    }

    internal void AddConfirmationForTest(ICommonSession session, TimeSpan deadline)
    {
        _confirmations[session] = new AfkConfirmation(null, deadline);
    }

    internal bool HasConfirmation(ICommonSession session)
    {
        return _confirmations.ContainsKey(session);
    }

    private sealed record AfkConfirmation(AfkConfirmEui? Eui, TimeSpan Deadline);
}

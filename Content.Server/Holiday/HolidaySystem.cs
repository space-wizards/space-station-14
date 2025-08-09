using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Shared.Holiday;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Player;

namespace Content.Server.Holiday;

/// <inheritdoc />
public sealed class HolidaySystem : SharedHolidaySystem
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        RefreshCurrentHolidays();

        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChanged);
        _player.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    #region Event Handlers

    /// <summary>
    ///     Reset holidays when going to lobby, and run holiday specific code at round start.
    /// </summary>
    private void OnRunLevelChanged(GameRunLevelChangedEvent eventArgs)
    {
        switch (eventArgs.New)
        {
            case GameRunLevel.PreRoundLobby:
                RefreshCurrentHolidays(announce: false);
                break;
            case GameRunLevel.InRound:
                DoGreet();
                DoCelebrate();
                break;
            case GameRunLevel.PostRound:
                // TODO post round celebration.
                break;
        }
    }

    /// <summary>
    ///     Send new client sessions the active holidays.
    /// </summary>
    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus == SessionStatus.Connected)
        {
            RaiseNetworkEvent(new DoRefreshHolidaysEvent(CurrentDate), e.Session);
        }
    }

    #endregion
    #region Public API

    /// <inheritdoc />
    [PublicAPI]
    public override void RefreshCurrentHolidays(bool announce = true)
    {
        base.RefreshCurrentHolidays(announce);

        var now = DateTime.Now;

        SetActiveHolidays(now);
        RaiseNetworkEvent(new DoRefreshHolidaysEvent(now));

        if (announce)
            DoGreet();
    }

    /// <inheritdoc />
    [PublicAPI]
    public override void RefreshCurrentHolidays(DateTime date, bool announce = true)
    {
        base.RefreshCurrentHolidays(date, announce);

        RaiseNetworkEvent(new DoRefreshHolidaysEvent(date));

        if (announce)
            DoGreet();
    }

    #endregion

    /// <summary>
    ///     Send a chat message to the server announcing the holidays.
    /// </summary>
    private void DoGreet()
    {
        if (!_enabled)
            return;

        foreach (var holiday in CurrentHolidays)
        {
            _chatManager.DispatchServerAnnouncement(holiday.Greet());
        }
    }

    /// <summary>
    ///     Function called at round start to run shenanigans (code) stored by each active holiday.
    /// </summary>
    private void DoCelebrate()
    {
        if (!_enabled)
            return;

        foreach (var holiday in CurrentHolidays)
        {
            holiday.Celebrate();
        }
    }
}

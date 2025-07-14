using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Shared.Holiday;

namespace Content.Server.Holiday;

/// <inheritdoc />
public sealed class HolidaySystem : SharedHolidaySystem
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private DateTime CurrentDate;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        RefreshCurrentHolidays();

        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChanged);
        _player.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    private void OnRunLevelChanged(GameRunLevelChangedEvent eventArgs)
    {
        switch (eventArgs.New)
        {
            case GameRunLevel.PreRoundLobby:
                RefreshCurrentHolidays();
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
            RaiseNetworkEvent(new UpdateHolidaysEvent(CurrentDate), e.Session);
        }
    }

    /// <summary>
    ///     Iterates through all <see cref="HolidayPrototype"/>s and sets if they should be active.
    ///     Networks active holidays to client.
    /// </summary>
    private void RefreshCurrentHolidays()
    {
        CurrentDate = DateTime.Now;

        SetActiveHolidays(CurrentDate);
        RaiseNetworkEvent(new UpdateHolidaysEvent(CurrentDate));
    }

    /// <summary>
    ///     Send a chat message to the server announcing the holiday at round start.
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

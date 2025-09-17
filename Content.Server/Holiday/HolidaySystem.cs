using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Shared.Holiday;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.Holiday;

/// <inheritdoc />
public sealed class HolidaySystem : SharedHolidaySystem
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChanged);
    }

    /// <summary>
    /// At round start, create the current holidays list and run holiday specific code.
    /// </summary>
    private void OnRunLevelChanged(GameRunLevelChangedEvent eventArgs)
    {
        if (eventArgs.New != GameRunLevel.InRound)
            return;

        RefreshCurrentHolidays();
        DoCelebrate();
    }

    #region Public API

    /// <inheritdoc />
    [PublicAPI]
    public override void RefreshCurrentHolidays(bool announce = true)
    {
        SetActiveHolidays(DateTime.Now);

        if (announce)
            DoGreet();
    }

    /// <inheritdoc />
    [PublicAPI]
    public override void RefreshCurrentHolidays(DateTime date, bool announce = true)
    {
        base.RefreshCurrentHolidays(date, announce);

        if (announce)
            DoGreet();
    }

    #endregion

    /// <summary>
    /// Send a chat message to the server announcing the holidays.
    /// </summary>
    private void DoGreet()
    {
        if (!TryGetInstance(out var singleton) || !singleton.Value.Comp.Enabled)
            return;

        foreach (var holidayId in singleton.Value.Comp.CurrentHolidays)
        {
            var holiday = _prototypeManager.Index(holidayId);
            _chatManager.DispatchServerAnnouncement(holiday.Greet());
        }
    }

    /// <summary>
    /// Function called at round start to run shenanigans (code) stored by each active holiday.
    /// </summary>
    private void DoCelebrate()
    {
        if (!TryGetInstance(out var singleton) || !singleton.Value.Comp.Enabled)
            return;

        foreach (var holidayId in singleton.Value.Comp.CurrentHolidays)
        {
            var holiday = _prototypeManager.Index(holidayId);
            holiday.Celebrate();
        }
    }
}

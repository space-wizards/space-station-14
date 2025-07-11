using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Shared.Holiday;
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
                break;
        }
    }

    /// <summary>
    ///     Iterates through all <see cref="HolidayPrototype"/>s and sets if they should be active.
    ///     Networks active holidays to client.
    /// </summary>
    private void RefreshCurrentHolidays()
    {
        CurrentHolidays.Clear();

        // If we're festive-less, leave CurrentHolidays empty
        if (!_enabled)
        {
            RaiseNetworkEvent(new HolidaysRefreshedEvent(Enumerable.Empty<HolidayPrototype>()));
            return;
        }

        var now = DateTime.Now;

        // Festively find what holidays we're celebrating
        foreach (var holiday in _prototypeManager.EnumeratePrototypes<HolidayPrototype>())
        {
            if (holiday.ShouldCelebrate(now))
            {
                CurrentHolidays.Add(holiday);
            }
        }

        RaiseNetworkEvent(new HolidaysRefreshedEvent(CurrentHolidays));
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

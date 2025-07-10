using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Shared.Holiday;

namespace Content.Server.Holiday;

// To move to shared, this needs
//      - a good way to periodically updated the holiday (i.e. shared knowing when we go to lobby)
//      - serverwide announcements in shared
/// <inheritdoc />
public sealed class HolidaySystem : SharedHolidaySystem
{
    [Dependency] private readonly IChatManager _chatManager = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChanged);
    }

    private void OnRunLevelChanged(GameRunLevelChangedEvent eventArgs)
    {
        if (!Enabled)
            return;

        switch (eventArgs.New)
        {
            case GameRunLevel.PreRoundLobby:
                RefreshCurrentHolidays(); // Part one of keeping this in server
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
    ///     Send a chat message to the server announcing the holiday.
    /// </summary>
    private void DoGreet()
    {
        foreach (var holiday in CurrentHolidays)
        {
            _chatManager.DispatchServerAnnouncement(holiday.Greet()); // Part two of keeping this in server
        }
    }
}

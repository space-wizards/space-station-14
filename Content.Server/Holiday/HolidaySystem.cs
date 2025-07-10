using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Shared.Holiday;

namespace Content.Server.Holiday;

/// <summary>
/// This handles...
/// </summary>
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
        if (!Enabled) return;

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

    public void DoGreet()
    {
        foreach (var holiday in CurrentHolidays)
        {
            _chatManager.DispatchServerAnnouncement(holiday.Greet());
        }
    }
}

using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Shared.Holiday;

namespace Content.Server.Holiday
{
    public sealed class ServerHolidaySystem : EntitySystem
    {
        [Dependency] private readonly HolidaySystem _holidayShared = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChanged);
        }

        private void OnRunLevelChanged(GameRunLevelChangedEvent eventArgs)
        {
            if (!_holidayShared.Enabled) return;

            switch (eventArgs.New)
            {
                case GameRunLevel.PreRoundLobby:
                    _holidayShared.RefreshCurrentHolidays();
                    break;
                case GameRunLevel.InRound:
                    DoGreet();
                    _holidayShared.DoCelebrate();
                    break;
                case GameRunLevel.PostRound:
                    break;
            }
        }

        public void DoGreet()
        {
            foreach (var holiday in _holidayShared.CurrentHolidays)
            {
                _chatManager.DispatchServerAnnouncement(holiday.Greet());
            }
        }
    }
}

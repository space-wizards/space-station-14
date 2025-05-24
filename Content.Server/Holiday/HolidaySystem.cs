using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Shared.CCVar;
using Content.Shared.Holiday;
using Robust.Shared.Configuration;

namespace Content.Server.Holiday
{
    public sealed class HolidaySystem : SharedHolidaySystem
    {
        [Dependency] private readonly IConfigurationManager _configManager = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeNetworkEvent<RequestHolidayEnabledEvent>(OnRequestHolidayEnabled);
            SubscribeNetworkEvent<RequestWhatDateItIsEvent>(OnWhatDateItIs);

            Subs.CVar(_configManager, CCVars.HolidaysEnabled, OnHolidaysEnableChange);
            SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChanged);
        }

        private void OnHolidaysEnableChange(bool enabled)
        {
            RaiseNetworkEvent(new HolidayEnablingEvent(enabled));
            RefreshCurrentHolidays();
        }

        /// <summary>
        /// Used to push enabled status to new clients.
        /// </summary>
        private void OnRequestHolidayEnabled(RequestHolidayEnabledEvent ev)
        {
            bool isEnabled = true;
            if (!_configManager.GetCVar(CCVars.HolidaysEnabled))
                isEnabled = false;

            RaiseNetworkEvent(new HolidayEnablingEvent(isEnabled));
            RefreshCurrentHolidays();
        }

        private void ProvideDate()
        {
            DateTime date = DateTime.Now;
            RaiseNetworkEvent(new ProvideWhatDateItIsEvent(date));
        }

        private void OnWhatDateItIs(RequestWhatDateItIsEvent ev)
        {
            ProvideDate();
        }

        private void OnRunLevelChanged(GameRunLevelChangedEvent eventArgs)
        {
            if (!Enabled) return;

            switch (eventArgs.New)
            {
                case GameRunLevel.PreRoundLobby:
                    ProvideDate();
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

        public override void DoCelebrate()
        {
            foreach (var holiday in CurrentHolidays)
            {
                holiday.Celebrate();
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
}

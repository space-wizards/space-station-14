using System;
using System.Collections.Generic;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.Holiday.Interfaces;
using Content.Shared;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Holiday
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class HolidayManager : IHolidayManager
    {
        [Dependency] private readonly IConfigurationManager _configManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IGameTicker _gameTicker = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;

        [ViewVariables]
        private readonly List<HolidayPrototype> _currentHolidays = new();

        [ViewVariables]
        private bool _enabled = true;

        public void RefreshCurrentHolidays()
        {
            _currentHolidays.Clear();

            if (!_enabled) return;

            var now = DateTime.Now;

            foreach (var holiday in _prototypeManager.EnumeratePrototypes<HolidayPrototype>())
            {
                if(holiday.ShouldCelebrate(now))
                    _currentHolidays.Add(holiday);
            }
        }

        public void DoGreet()
        {
            foreach (var holiday in _currentHolidays)
            {
                _chatManager.DispatchServerAnnouncement(holiday.Greet());
            }
        }

        public void DoCelebrate()
        {
            foreach (var holiday in _currentHolidays)
            {
                holiday.Celebrate();
            }
        }

        public IEnumerable<HolidayPrototype> GetCurrentHolidays()
        {
            return _currentHolidays;
        }

        public bool IsCurrentlyHoliday(string holiday)
        {
            if (!_prototypeManager.TryIndex(holiday, out HolidayPrototype? prototype))
                return false;

            return _currentHolidays.Contains(prototype);
        }

        public void Initialize()
        {
            _configManager.OnValueChanged(CCVars.HolidaysEnabled, OnHolidaysEnableChange, true);

            _gameTicker.OnRunLevelChanged += OnRunLevelChanged;
        }

        private void OnHolidaysEnableChange(bool enabled)
        {
            _enabled = enabled;

            RefreshCurrentHolidays();
        }

        private void OnRunLevelChanged(GameRunLevelChangedEventArgs eventArgs)
        {
            if (!_enabled) return;

            switch (eventArgs.NewRunLevel)
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
    }
}

using System;
using System.Collections.Generic;
using Content.Server.GameTicking;
using Content.Server.Holiday.Interfaces;
using Content.Server.Interfaces.Chat;
using Content.Server.Interfaces.GameTicking;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Holiday
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class HolidayManager : IHolidayManager
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IGameTicker _gameTicker = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;

        [ViewVariables]
        private readonly List<HolidayPrototype> _currentHolidays = new();

        public void RefreshCurrentHolidays()
        {
            var now = DateTime.Now;

            _currentHolidays.Clear();

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
            if (!_prototypeManager.TryIndex(holiday, out HolidayPrototype prototype))
                return false;

            return _currentHolidays.Contains(prototype);
        }

        public void Initialize()
        {
            RefreshCurrentHolidays();
            _gameTicker.OnRunLevelChanged += OnRunLevelChanged;
        }

        private void OnRunLevelChanged(GameRunLevelChangedEventArgs eventArgs)
        {
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

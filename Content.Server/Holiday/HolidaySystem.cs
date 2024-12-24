using System.Linq;
using Content.Server.Chat.Managers;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Server.Holiday
{
    public sealed class HolidaySystem : EntitySystem
    {
        [Dependency] private readonly IConfigurationManager _configManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;

        [ViewVariables]
        private readonly List<HolidayPrototype> _currentHolidays = new();

        [ViewVariables]
        private bool _enabled = true;
        
        public bool enabled => _enabled;

        public override void Initialize()
        {
            Subs.CVar(_configManager, CCVars.HolidaysEnabled, OnHolidaysEnableChange);
        }

        public void RefreshCurrentHolidays()
        {
            _currentHolidays.Clear();

            if (!_enabled)
            {
                RaiseLocalEvent(new HolidaysRefreshedEvent(Enumerable.Empty<HolidayPrototype>()));
                return;
            }

            var now = DateTime.Now;

            foreach (var holiday in _prototypeManager.EnumeratePrototypes<HolidayPrototype>())
            {
                if (holiday.ShouldCelebrate(now))
                {
                    _currentHolidays.Add(holiday);
                }
            }

            RaiseLocalEvent(new HolidaysRefreshedEvent(_currentHolidays));
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
            
            if (_currentHolidays.Count == 0)
                RefreshCurrentHolidays();

            return _currentHolidays.Contains(prototype);
        }

        private void OnHolidaysEnableChange(bool enabled)
        {
            _enabled = enabled;

            RefreshCurrentHolidays();
        }
    }

    /// <summary>
    ///     Event for when the list of currently active holidays has been refreshed.
    /// </summary>
    public sealed class HolidaysRefreshedEvent : EntityEventArgs
    {
        public readonly IEnumerable<HolidayPrototype> Holidays;

        public HolidaysRefreshedEvent(IEnumerable<HolidayPrototype> holidays)
        {
            Holidays = holidays;
        }
    }
}

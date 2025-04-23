using System.Linq;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared.Holiday
{
    public sealed class HolidaySystem : EntitySystem
    {
        [Dependency] private readonly IConfigurationManager _configManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly INetManager _net = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

        [ViewVariables]
        private readonly List<HolidayPrototype> _currentHolidays = new();
        public List<HolidayPrototype> CurrentHolidays => _currentHolidays;

        [ViewVariables]
        private bool _enabled = true;
        public bool Enabled => _enabled;

        public override void Initialize()
        {
            Subs.CVar(_configManager, CCVars.HolidaysEnabled, OnHolidaysEnableChange);
            SubscribeLocalEvent<HolidayVisualsComponent, ComponentInit>(OnVisualsInit);
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

        public void DoCelebrate()
        {
            foreach (var holiday in _currentHolidays)
            {
                if (_net.IsServer)
                {
                    holiday.Celebrate();
                }
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

        private void OnHolidaysEnableChange(bool enabled)
        {
            _enabled = enabled;

            RefreshCurrentHolidays();
        }

        private void OnVisualsInit(Entity<HolidayVisualsComponent> ent, ref ComponentInit args)
        {
            foreach (var (key, holidays) in ent.Comp.Holidays)
            {
                if (!holidays.Any(h => IsCurrentlyHoliday(h)))
                    continue;
                _appearance.SetData(ent, HolidayVisuals.Holiday, key);
                break;
            }
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

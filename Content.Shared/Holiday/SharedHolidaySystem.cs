using System.Linq;
using Robust.Shared.Prototypes;

namespace Content.Shared.Holiday
{
    public abstract partial class SharedHolidaySystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

        public bool Enabled = true;

        private readonly List<HolidayPrototype> _currentHolidays = new();
        public List<HolidayPrototype> CurrentHolidays => _currentHolidays;

        private DateTime _currentDate = DateTime.Today;

        public override void Initialize()
        {
            SubscribeNetworkEvent<HolidayEnablingEvent>(OnHolidayCCvarChange);
            SubscribeNetworkEvent<ProvideWhatDateItIsEvent>(OnDateReceived);
            SubscribeLocalEvent<HolidayVisualsComponent, ComponentInit>(OnVisualsInit);

            RaiseNetworkEvent(new RequestHolidayEnabledEvent()); // for new clients
            RaiseNetworkEvent(new RequestWhatDateItIsEvent());
        }

        public void OnHolidayCCvarChange(HolidayEnablingEvent ev)
        {
            Enabled = ev.Enabled;
        }

        public void OnDateReceived(ProvideWhatDateItIsEvent ev)
        {
            _currentDate = ev.Date;
        }

        public void RefreshCurrentHolidays()
        {
            _currentHolidays.Clear();

            if (!Enabled)
            {
                RaiseLocalEvent(new HolidaysRefreshedEvent(Enumerable.Empty<HolidayPrototype>()));
                return;
            }

            RaiseNetworkEvent(new RequestWhatDateItIsEvent());
            var now = _currentDate;

            foreach (var holiday in _prototypeManager.EnumeratePrototypes<HolidayPrototype>())
            {
                if (holiday.ShouldCelebrate(now))
                {
                    _currentHolidays.Add(holiday);
                }
            }

            RaiseLocalEvent(new HolidaysRefreshedEvent(_currentHolidays));
        }

        public virtual void DoCelebrate()
        {
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

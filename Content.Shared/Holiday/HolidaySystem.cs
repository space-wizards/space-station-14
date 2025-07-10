using System.Linq;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Shared.Holiday
{
    public abstract class SharedHolidaySystem : EntitySystem
    {
        [Dependency] private readonly IConfigurationManager _configManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

        [ViewVariables]
        protected readonly List<HolidayPrototype> CurrentHolidays = new();

        [ViewVariables]
        protected bool Enabled = true;

        public override void Initialize()
        {
            base.Initialize();

            Subs.CVar(_configManager, CCVars.HolidaysEnabled, OnHolidaysEnableChange);
            SubscribeLocalEvent<HolidayVisualsComponent, ComponentInit>(OnVisualsInit);
        }

        public void RefreshCurrentHolidays()
        {
            CurrentHolidays.Clear();

            if (!Enabled)
            {
                RaiseLocalEvent(new HolidaysRefreshedEvent(Enumerable.Empty<HolidayPrototype>()));
                return;
            }

            var now = DateTime.Now;

            foreach (var holiday in _prototypeManager.EnumeratePrototypes<HolidayPrototype>())
            {
                if (holiday.ShouldCelebrate(now))
                {
                    CurrentHolidays.Add(holiday);
                }
            }

            RaiseLocalEvent(new HolidaysRefreshedEvent(CurrentHolidays));
        }

        public void DoCelebrate()
        {
            foreach (var holiday in CurrentHolidays)
            {
                holiday.Celebrate();
            }
        }

        public IEnumerable<HolidayPrototype> GetCurrentHolidays()
        {
            return CurrentHolidays;
        }

        public bool IsCurrentlyHoliday(string holiday)
        {
            if (!_prototypeManager.TryIndex(holiday, out HolidayPrototype? prototype))
                return false;

            return CurrentHolidays.Contains(prototype);
        }

        private void OnHolidaysEnableChange(bool enabled)
        {
            Enabled = enabled;

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

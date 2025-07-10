using System.Linq;
using Content.Shared.CCVar;
using JetBrains.Annotations;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Shared.Holiday
{
    /// <summary>
    ///     System for festivities!
    ///     Used to track what holidays are occuring and handle code relating to them.
    /// </summary>
    public abstract class SharedHolidaySystem : EntitySystem
    {
        [Dependency] private readonly IConfigurationManager _configManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

        [ViewVariables]
        protected readonly List<HolidayPrototype> CurrentHolidays = new(); // Should this be a HashSet?

        // CCvar
        [ViewVariables]
        protected bool Enabled = true;

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            Subs.CVar(_configManager, CCVars.HolidaysEnabled, OnHolidaysEnableChange);
            SubscribeLocalEvent<HolidayVisualsComponent, ComponentInit>(OnVisualsInit);
        }

        /// <summary>
        ///     Iterates through all <see cref="HolidayPrototype"/>s and sets if they should be active.
        /// </summary>
        protected void RefreshCurrentHolidays()
        {
            CurrentHolidays.Clear();

            // If we're festive-less, leave CurrentHolidays empty
            if (!Enabled)
            {
                RaiseLocalEvent(new HolidaysRefreshedEvent(Enumerable.Empty<HolidayPrototype>()));
                return;
            }

            var now = DateTime.Now;

            // Festively find what holidays we're celebrating
            foreach (var holiday in _prototypeManager.EnumeratePrototypes<HolidayPrototype>())
            {
                if (holiday.ShouldCelebrate(now))
                {
                    CurrentHolidays.Add(holiday);
                }
            }

            RaiseLocalEvent(new HolidaysRefreshedEvent(CurrentHolidays));
        }

        /// <summary>
        ///     Function called at round start to run shenanigans (code) stored by each active holiday.
        /// </summary>
        protected void DoCelebrate()
        {
            foreach (var holiday in CurrentHolidays)
            {
                holiday.Celebrate();
            }
        }

        /// <summary>
        ///     Function used when getting CCvar.
        /// </summary>
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

        #region Public API

        /// <returns> All currently active holidays. </returns>
        [PublicAPI]
        public IEnumerable<HolidayPrototype> GetCurrentHolidays()
        {
            return CurrentHolidays;
        }

        /// <returns> True if "holiday" is currently celebrated. </returns>
        [PublicAPI]
        public bool IsCurrentlyHoliday(ProtoId<HolidayPrototype> holiday)
        {
            return _prototypeManager.TryIndex(holiday, out var prototype)
                   && CurrentHolidays.Contains(prototype);
        }

        #endregion
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

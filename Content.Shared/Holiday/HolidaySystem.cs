using System.Linq;
using Content.Shared.CCVar;
using JetBrains.Annotations;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

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

        protected readonly List<HolidayPrototype> CurrentHolidays = new(); // Should this be a HashSet?

        // CCvar.
        protected bool _enabled;

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();
            Subs.CVar(_configManager, CCVars.HolidaysEnabled, value => _enabled = value, true);

            SubscribeLocalEvent<HolidayVisualsComponent, ComponentInit>(OnVisualsInit);
        }

        /// <summary>
        ///     Sets an enum so festive entities know what holiday(s) it is.
        /// </summary>
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

        /// <returns> All currently active holidays (if cvar is enabled). </returns>
        [PublicAPI]
        public IEnumerable<HolidayPrototype> GetCurrentHolidays()
        {
            return !_enabled ? Enumerable.Empty<HolidayPrototype>() : CurrentHolidays;
        }

        /// <returns> True if argument is currently celebrated. Always false if cvar is false. </returns>
        [PublicAPI]
        public bool IsCurrentlyHoliday(ProtoId<HolidayPrototype> holiday)
        {
            if (!_enabled)
                return false;

            return _prototypeManager.TryIndex(holiday, out var prototype)
                   && CurrentHolidays.Contains(prototype);
        }

        #endregion
    }

    /// <summary>
    ///     Event for when the list of currently active holidays has been refreshed.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class HolidaysRefreshedEvent(List<HolidayPrototype> holidays) : EntityEventArgs
    {
        public readonly List<HolidayPrototype> Holidays = holidays;
    }
}

using System.Linq;
using Content.Shared.CCVar;
using JetBrains.Annotations;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Holiday;

/// <summary>
///     System for festivities!
///     Used to track what holidays are occuring and handle code relating to them.
/// </summary>
public abstract class SharedHolidaySystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    protected DateTime CurrentDate;
    protected readonly List<HolidayPrototype> CurrentHolidays = new(); // Should this be a HashSet?

    // CCvar.
    protected bool _enabled;

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_configManager, CCVars.HolidaysEnabled, value => _enabled = value, true);

        SubscribeLocalEvent<HolidayVisualsComponent, ComponentInit>(OnVisualsInit);
        SubscribeLocalEvent<HolidaysGotRefreshedEvent>(OnVisualsRefresh);
    }

    /// <summary>
    ///     Sets an enum for what holiday(s) it is.
    /// </summary>
    private void OnVisualsInit(Entity<HolidayVisualsComponent> ent, ref ComponentInit args)
    {
        SetVisualData(ent);
    }

    /// <summary>
    ///     Resets an enum for what holiday(s) it is on all entities with <see cref="HolidayVisualsComponent"/>.
    /// </summary>
    private void OnVisualsRefresh(ref HolidaysGotRefreshedEvent args)
    {
        var query = AllEntityQuery<HolidayVisualsComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            _appearance.RemoveData(uid, HolidayVisuals.Holiday);
            SetVisualData((uid, comp));
        }
    }

    private void SetVisualData(Entity<HolidayVisualsComponent> ent)
    {
        foreach (var (key, holidays) in ent.Comp.Holidays)
        {
            if (!holidays.Any(h => IsCurrentlyHoliday(h)))
                continue;
            _appearance.SetData(ent, HolidayVisuals.Holiday, key);
            break;
        }
    }

    /// <summary>
    ///     Refreshes the currently active holidays.
    /// </summary>
    protected void SetActiveHolidays(DateTime now)
    {
        if (CurrentDate == now)
            return;

        CurrentHolidays.Clear();
        CurrentDate = now;

        // Festively find what holidays we're celebrating
        foreach (var holiday in _prototypeManager.EnumeratePrototypes<HolidayPrototype>())
        {
            if (holiday.ShouldCelebrate(CurrentDate))
            {
                CurrentHolidays.Add(holiday);
            }
        }

        var ev = new HolidaysGotRefreshedEvent();
        RaiseLocalEvent(ref ev);
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
    public bool IsCurrentlyHoliday([ForbidLiteral] ProtoId<HolidayPrototype> holiday)
    {
        if (!_enabled)
            return false;

        return _prototypeManager.TryIndex(holiday, out var prototype)
               && CurrentHolidays.Contains(prototype);
    }

    /// <summary>
    ///     Unpredicted. Sets the date used for checking holidays to the server clock.
    /// </summary>
    /// <param name="announce">If true, announce the holiday greeting.</param>
    [PublicAPI]
    public virtual void RefreshCurrentHolidays(bool announce = true) { }

    /// <summary>
    ///     Predicted. Sets the date used for checking holidays to <paramref name="date"/>.
    /// </summary>
    /// <remarks>
    ///     Note that DateTime.Now can be different between client and server.
    ///     Only call this method with a specific date.
    /// </remarks>
    /// <param name="announce">If true, announce the holiday greeting.</param>
    [PublicAPI]
    public virtual void RefreshCurrentHolidays(DateTime date, bool announce = true)
    {
        SetActiveHolidays(date);
    }

    #endregion
}

/// <summary>
///     Event for when the list of currently active holidays has been refreshed.
/// </summary>
[ByRefEvent]
public record struct HolidaysGotRefreshedEvent;

/// <summary>
///     Network event telling client to update its holidays.
/// </summary>
[Serializable, NetSerializable]
public sealed class DoRefreshHolidaysEvent(DateTime now) : EntityEventArgs
{
    public readonly DateTime Now = now;
}

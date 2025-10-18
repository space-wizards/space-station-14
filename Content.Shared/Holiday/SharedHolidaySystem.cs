using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.CCVar;
using JetBrains.Annotations;
using Robust.Shared.Configuration;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Holiday;

/// <summary>
/// System for festivities!
/// Used to track what holidays are occuring and handle code relating to them.
/// </summary>
public abstract class SharedHolidaySystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly SharedPvsOverrideSystem _pvsOverride = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    private Entity<CurrentHolidaySingletonComponent>? _cachedEntity;

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CurrentHolidaySingletonComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<CurrentHolidaySingletonComponent, ComponentShutdown>(OnComponentShutdown);

        SubscribeLocalEvent<HolidayVisualsComponent, ComponentInit>(OnVisualsInit);
        // TODO use HolidaysGotRefreshedEvent to update HolidayVisualsComponent and HolidayRsiSwapComponent
    }

    #region Internal

    /// <summary>
    /// Sets an enum key for the first list of holidays found.
    /// </summary>
    private void OnVisualsInit(Entity<HolidayVisualsComponent> ent, ref ComponentInit args)
    {
        foreach (var (key, holidays) in ent.Comp.Holidays)
        {
            if (!holidays.Any(IsCurrentlyHoliday))
                continue;
            _appearance.SetData(ent, HolidayVisuals.Holiday, key);
            break;
        }
    }

    /// <summary>
    /// Refreshes the currently active holidays.
    /// </summary>
    protected void SetActiveHolidays(DateTime now)
    {
        if (!TryGetInstance(out var singleton))
            return;

        var (_, comp) = singleton.Value;

        // No need to redo everything if the date hasn't changed.
        if (comp.CurrentDate == now)
            return;

        comp.CurrentHolidays.Clear();
        comp.CurrentDate = now;

        // Festively find what holidays we're celebrating
        foreach (var holiday in _prototypeManager.EnumeratePrototypes<HolidayPrototype>())
        {
            if (holiday.ShouldCelebrate(comp.CurrentDate))
            {
                comp.CurrentHolidays.Add(holiday);
            }
        }

        Dirty(singleton.Value);
        var ev = new HolidaysGotRefreshedEvent();
        RaiseLocalEvent(ref ev);
    }

    #endregion
    #region Singleton
    // This region is extremely similar to the methods found in https://github.com/space-wizards/RobustToolbox/pull/5683

    /// <summary>
    /// Attempts to get the singleton instance.
    /// This always succeeds on the server, where the instance will be created if it does not exist yet.
    /// On the client, this can return false if attempting to access the instance before it has been sent from the server.
    /// </summary>
    /// <param name="instance">The singleton Entity{CurrentHolidaySingletonComponent} instance.</param>
    /// <returns>True if the instance was found or created, otherwise false.</returns>
    protected bool TryGetInstance([NotNullWhen(true)] out Entity<CurrentHolidaySingletonComponent>? instance)
    {
        instance = FindOrCreateHolder();
        return instance != null;
    }

    private Entity<CurrentHolidaySingletonComponent>? FindOrCreateHolder()
    {
        if (_cachedEntity != null)
            return _cachedEntity;

        // Try to find the singleton if it wasn't cached for some reason (save/load)
        var query = EntityQueryEnumerator<CurrentHolidaySingletonComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            _cachedEntity = (uid, comp);
            return (uid, comp);
        }

        // Client can't create the singleton, so it needs to wait for server
        if (_netMan.IsClient)
            return null;

        return CreateHolder();
    }

    private Entity<CurrentHolidaySingletonComponent> CreateHolder()
    {
        var uid = Spawn(null, MapCoordinates.Nullspace);
        var comp = AddComp<CurrentHolidaySingletonComponent>(uid);
        _meta.SetEntityName(uid, "Current Holiday Holder");
        _pvsOverride.AddGlobalOverride(uid);

        comp.Enabled = _configManager.GetCVar(CCVars.HolidaysEnabled);
        RefreshCurrentHolidays(announce: false);

        return (uid, comp);
    }

    private void OnComponentInit(Entity<CurrentHolidaySingletonComponent> entity, ref ComponentInit args)
    {
        DebugTools.Assert(_cachedEntity == null);

        _cachedEntity = entity;
    }

    private void OnComponentShutdown(Entity<CurrentHolidaySingletonComponent> entity, ref ComponentShutdown args)
    {
        _cachedEntity = null;
    }

    #endregion
    #region Public API

    /// <returns> All currently active holidays (if cvar is enabled). </returns>
    [PublicAPI]
    public IEnumerable<ProtoId<HolidayPrototype>> GetCurrentHolidays()
    {
        if (!TryGetInstance(out var singleton) || !singleton.Value.Comp.Enabled)
            return [];

        return singleton.Value.Comp.CurrentHolidays;
    }

    /// <returns> True if <paramref name="holiday"/> is currently celebrated. Always false if cvar is false. </returns>
    [PublicAPI]
    public bool IsCurrentlyHoliday([ForbidLiteral] ProtoId<HolidayPrototype> holiday)
    {
        if (!TryGetInstance(out var singleton) || !singleton.Value.Comp.Enabled)
            return false;

        return _prototypeManager.TryIndex(holiday, out var prototype) &&
               singleton.Value.Comp.CurrentHolidays.Contains(prototype);
    }

    /// <summary>
    /// Unpredicted. Sets the time used for checking holidays to the server clock.
    /// </summary>
    /// <param name="announce">If true, announce the holiday greeting.</param>
    [PublicAPI]
    public virtual void RefreshCurrentHolidays(bool announce = true) { }

    /// <summary>
    /// Predicted. Sets the time used for checking holidays to provided <paramref name="date"/>.
    /// </summary>
    /// <remarks>
    /// Note that DateTime can be different between client and server.
    /// Only call this method with a specific time.
    /// </remarks>
    /// <param name="date">The new date to start celebrating.</param>
    /// <param name="announce">If true, announce the holiday greeting.</param>
    [PublicAPI]
    public virtual void RefreshCurrentHolidays(DateTime date, bool announce = true)
    {
        SetActiveHolidays(date);
    }

    #endregion
}

/// <summary>
/// Event for when the list of currently active holidays has been refreshed.
/// </summary>
[ByRefEvent]
public record struct HolidaysGotRefreshedEvent;

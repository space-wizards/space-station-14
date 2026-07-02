using System.Numerics;
using Content.Shared.Access.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.Clothing;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Emp;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Station;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Medical.SuitSensors;

public abstract partial class SharedSuitSensorSystem : EntitySystem
{
    [Dependency] private SharedStationSystem _stationSystem = default!;
    [Dependency] private MobStateSystem _mobStateSystem = default!;
    [Dependency] private SharedPopupSystem _popupSystem = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private MobThresholdSystem _mobThresholdSystem = default!;
    [Dependency] private SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedIdCardSystem _idCardSystem = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private DamageableSystem _damageable = default!;

    [Dependency] private EntityQuery<SuitSensorComponent> _sensorQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SuitSensorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawn);
        SubscribeLocalEvent<SuitSensorComponent, ClothingGotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<SuitSensorComponent, ClothingGotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<SuitSensorComponent, EmpPulseEvent>(OnEmpPulse);
        SubscribeLocalEvent<SuitSensorComponent, EmpDisabledRemovedEvent>(OnEmpFinished);
        SubscribeLocalEvent<SuitSensorComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<SuitSensorComponent, GetVerbsEvent<Verb>>(OnVerb);
        SubscribeLocalEvent<SuitSensorComponent, EntGotInsertedIntoContainerMessage>(OnInsert);
        SubscribeLocalEvent<SuitSensorComponent, EntGotRemovedFromContainerMessage>(OnRemove);
        SubscribeLocalEvent<SuitSensorComponent, SuitSensorChangeDoAfterEvent>(OnSuitSensorDoAfter);
    }

    /// <summary>
    /// Checks whether the sensor is assigned to a station or not
    /// and tries to assign an unassigned sensor to a station if it's currently on a grid.
    /// </summary>
    /// <returns>True if the sensor is assigned to a station or assigning it was successful. False otherwise.</returns>
    public bool CheckSensorAssignedStation(Entity<SuitSensorComponent> sensor)
    {
        if (!sensor.Comp.StationId.HasValue && Transform(sensor.Owner).GridUid == null)
            return false;

        sensor.Comp.StationId = _stationSystem.GetOwningStation(sensor.Owner);
        Dirty(sensor);
        return sensor.Comp.StationId.HasValue;
    }

    private void OnMapInit(Entity<SuitSensorComponent> ent, ref MapInitEvent args)
    {
        // Fallback
        ent.Comp.StationId ??= _stationSystem.GetOwningStation(ent.Owner);

        // generate random mode
        if (ent.Comp.RandomMode)
        {
            //make the sensor mode favor higher levels, except coords.
            var modesDist = new[]
            {
                SuitSensorMode.SensorOff,
                SuitSensorMode.SensorBinary, SuitSensorMode.SensorBinary,
                SuitSensorMode.SensorVitals, SuitSensorMode.SensorVitals, SuitSensorMode.SensorVitals,
                SuitSensorMode.SensorCords, SuitSensorMode.SensorCords
            };
            ent.Comp.Mode = _random.Pick(modesDist);
        }

        ent.Comp.NextUpdate = _timing.CurTime;
        Dirty(ent);
    }

    private void OnPlayerSpawn(PlayerSpawnCompleteEvent ev)
    {
        // If the player spawns in arrivals then the grid underneath them may not be appropriate.
        // in which case we'll just use the station spawn code told us they are attached to and set all of their
        // sensors.
        RecursiveSensor(ev.Mob, ev.Station);
    }

    private void RecursiveSensor(EntityUid uid, EntityUid stationUid)
    {
        var xform = Transform(uid);
        var enumerator = xform.ChildEnumerator;

        while (enumerator.MoveNext(out var child))
        {
            if (_sensorQuery.TryComp(child, out var sensor))
            {
                sensor.StationId = stationUid;
                Dirty(child, sensor);
            }

            RecursiveSensor(child, stationUid);
        }
    }

    private void OnEquipped(Entity<SuitSensorComponent> ent, ref ClothingGotEquippedEvent args)
    {
        ent.Comp.User = args.Wearer;
        Dirty(ent);
    }

    private void OnUnequipped(Entity<SuitSensorComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        ent.Comp.User = null;
        Dirty(ent);
    }

    private void OnEmpPulse(Entity<SuitSensorComponent> ent, ref EmpPulseEvent args)
    {
        args.Affected = true;
        args.Disabled = true;

        ent.Comp.PreviousMode = ent.Comp.Mode;
        SetSensor(ent.AsNullable(), SuitSensorMode.SensorOff, null);

        ent.Comp.PreviousControlsLocked = ent.Comp.ControlsLocked;
        ent.Comp.ControlsLocked = true;
        // SetSensor already calls Dirty
    }

    private void OnEmpFinished(Entity<SuitSensorComponent> ent, ref EmpDisabledRemovedEvent args)
    {
        SetSensor(ent.AsNullable(), ent.Comp.PreviousMode, null);
        ent.Comp.ControlsLocked = ent.Comp.PreviousControlsLocked;
    }

    /// <summary>
    /// Localize the SuitSensor status and push to the examination tooltip.
    /// </summary>
    /// <param name="ent">Entity with a <see cref="SuitSensorComponent"/> under examination.</param>
    /// <param name="args"><see cref="ExaminedEvent"/> arguments,
    /// used to determine range and retrieve the active mode.</param>
    /// <exception cref="InvalidOperationException">Invalid mode was provided.</exception>
    private void OnExamine(Entity<SuitSensorComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var locId = ent.Comp.Mode switch
        {
            SuitSensorMode.SensorOff => "suit-sensor-examine-off",
            SuitSensorMode.SensorBinary => "suit-sensor-examine-binary",
            SuitSensorMode.SensorVitals => "suit-sensor-examine-vitals",
            SuitSensorMode.SensorCords => "suit-sensor-examine-cords",
            _ => throw new InvalidOperationException($"Unknown {nameof(SuitSensorMode)}: {ent.Comp.Mode}"),
        };

        args.PushMarkup(Loc.GetString(locId));
    }

    private void OnVerb(Entity<SuitSensorComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        // check if user can change sensor
        if (ent.Comp.ControlsLocked)
            return;

        // standard interaction checks
        if (!args.CanInteract || args.Hands == null)
            return;

        if (!_interactionSystem.InRangeUnobstructed(args.User, args.Target))
            return;

        // check if target is incapacitated (cuffed, dead, etc)
        if (ent.Comp.User != null && args.User != ent.Comp.User && _actionBlocker.CanInteract(ent.Comp.User.Value, null))
            return;

        args.Verbs.UnionWith(new[]
        {
            CreateVerb(ent, args.User, SuitSensorMode.SensorOff),
            CreateVerb(ent, args.User, SuitSensorMode.SensorBinary),
            CreateVerb(ent, args.User, SuitSensorMode.SensorVitals),
            CreateVerb(ent, args.User, SuitSensorMode.SensorCords)
        });
    }

    private void OnInsert(Entity<SuitSensorComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.ActivationContainer)
            return;

        ent.Comp.User = args.Container.Owner;
        Dirty(ent);
    }

    private void OnRemove(Entity<SuitSensorComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.ActivationContainer)
            return;

        ent.Comp.User = null;
        Dirty(ent);
    }

    /// <summary>
    /// Create a verb for viewing and changing suit sensor behavior.
    /// </summary>
    /// <param name="ent">Entity with a <see cref="SuitSensorComponent"/> to be verbed.</param>
    /// <param name="userUid">Actor requesting the verb, used to identify if a foreign actor is requesting a verb.</param>
    /// <param name="mode">Current mode of the suit sensor.</param>
    /// <returns>A created <see cref="Verb"/> that will attempt to change to a specific mode.</returns>
    private Verb CreateVerb(Entity<SuitSensorComponent> ent, EntityUid userUid, SuitSensorMode mode)
    {
        return new Verb()
        {
            Text = GetModeName(mode),
            Message = GetModeDescription(mode),
            Disabled = ent.Comp.Mode == mode,
            Priority = -(int)mode, // sort them in descending order
            Category = VerbCategory.SetSensor,
            Act = () => TrySetSensor(ent.AsNullable(), mode, userUid)
        };
    }

    /// <summary>
    /// Gets the localized name of a suit sensor mode.
    /// </summary>
    /// <param name="mode">The <see cref="SuitSensorMode"/> requiring a name string.</param>
    /// <returns>A localized string containing the name of the suit sensor mode.</returns>
    /// <exception cref="InvalidOperationException">Invalid mode was provided.</exception>
    public string GetModeName(SuitSensorMode mode)
    {
        var locId = mode switch
        {
            SuitSensorMode.SensorOff => "suit-sensor-mode-off",
            SuitSensorMode.SensorBinary => "suit-sensor-mode-binary",
            SuitSensorMode.SensorVitals => "suit-sensor-mode-vitals",
            SuitSensorMode.SensorCords => "suit-sensor-mode-cords",
            _ => throw new InvalidOperationException($"Unknown {nameof(SuitSensorMode)}: {mode}"),
        };
        return Loc.GetString(locId);
    }

    /// <summary>
    /// Gets the localized description of a suit sensor mode.
    /// </summary>
    /// <param name="mode">The <see cref="SuitSensorMode"/> requiring a description.</param>
    /// <returns>A localized string containing the description of the suit sensor mode.</returns>
    /// <exception cref="InvalidOperationException">Invalid mode was provided.</exception>
    public string GetModeDescription(SuitSensorMode mode)
    {
        var locId = mode switch
        {
            SuitSensorMode.SensorOff => "suit-sensor-description-off",
            SuitSensorMode.SensorBinary => "suit-sensor-description-binary",
            SuitSensorMode.SensorVitals => "suit-sensor-description-vitals",
            SuitSensorMode.SensorCords => "suit-sensor-description-cords",
            _ => throw new InvalidOperationException($"Unknown {nameof(SuitSensorMode)}: {mode}"),
        };
        return Loc.GetString(locId);
    }

    /// <summary>
    /// Attempts to set <see cref="SuitSensorComponent"/> mode of the entity to the selected in params.
    /// Works instantly if the user is the player wearing the sensors and will start a DoAfter otherwise.
    /// </summary>
    /// <param name="sensors">Entity and its component that should be changed.</param>
    /// <param name="mode">Selected mode</param>
    /// <param name="userUid">userUid, when not equal to the <see cref="SuitSensorComponent.User"/>, creates doafter</param>
    public bool TrySetSensor(Entity<SuitSensorComponent?> sensors, SuitSensorMode mode, EntityUid userUid)
    {
        if (!Resolve(sensors, ref sensors.Comp, false))
            return false;

        if (sensors.Comp.User == null || userUid == sensors.Comp.User)
            SetSensor(sensors, mode, userUid);
        else
        {
            var doAfterEvent = new SuitSensorChangeDoAfterEvent(mode);
            var doAfterArgs = new DoAfterArgs(EntityManager, userUid, sensors.Comp.SensorsTime, doAfterEvent, sensors)
            {
                BreakOnMove = true,
                BreakOnDamage = true
            };

            _doAfterSystem.TryStartDoAfter(doAfterArgs);
        }
        return true;
    }

    private void OnSuitSensorDoAfter(Entity<SuitSensorComponent> sensors, ref SuitSensorChangeDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        SetSensor(sensors.AsNullable(), args.Mode, args.User);
    }

    /// <summary>
    /// Sets mode of the <see cref="SuitSensorComponent"/> of the chosen entity.
    /// Makes popup when <param name="userUid"> not null
    /// </summary>
    /// <param name="sensors">Entity and it's component that should be changed</param>
    /// <param name="mode">Selected mode</param>
    /// <param name="userUid">uid, required for the popup</param>
    public void SetSensor(Entity<SuitSensorComponent?> sensors, SuitSensorMode mode, EntityUid? userUid = null)
    {
        if (!Resolve(sensors, ref sensors.Comp, false))
            return;

        sensors.Comp.Mode = mode;
        Dirty(sensors);

        if (userUid != null)
        {
            var msg = Loc.GetString("suit-sensor-mode-state", ("mode", GetModeName(mode)));
            _popupSystem.PopupClient(msg, sensors, userUid.Value);
        }
    }

    /// <summary>
    /// Set all suit sensors on the equipment someone is wearing to the specified mode.
    /// </summary>
    public void SetAllSensors(EntityUid target, SuitSensorMode mode, SlotFlags slots = SlotFlags.All)
    {
        // iterate over all inventory slots
        var slotEnumerator = _inventory.GetSlotEnumerator(target, slots);
        while (slotEnumerator.NextItem(out var item, out _))
        {
            if (TryComp<SuitSensorComponent>(item, out var sensorComp))
                SetSensor((item, sensorComp), mode);
        }
    }

    /// <summary>
    /// Attempts to get full <see cref="SuitSensorStatus"/> from the <see cref="SuitSensorComponent"/>
    /// </summary>
    /// <param name="uid">Entity to get status</param>
    /// <returns>Full <see cref="SuitSensorStatus"/> of the chosen uid</returns>
    public SuitSensorStatus? GetSensorState(Entity<SuitSensorComponent?, TransformComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2, false))
            return null;

        var sensor = ent.Comp1;
        var transform = ent.Comp2;

        // check if sensor is enabled and worn by user
        if (sensor.Mode == SuitSensorMode.SensorOff || sensor.User == null || !HasComp<MobStateComponent>(sensor.User) || transform.GridUid == null)
            return null;

        // try to get mobs id from ID slot
        var userName = Loc.GetString("suit-sensor-component-unknown-name");
        var userJob = Loc.GetString("suit-sensor-component-unknown-job");
        var userJobIcon = "JobIconNoId";
        var userJobDepartments = new List<string>();

        if (_idCardSystem.TryFindIdCard(sensor.User.Value, out var card))
        {
            if (card.Comp.FullName != null)
                userName = card.Comp.FullName;
            if (card.Comp.LocalizedJobTitle != null)
                userJob = card.Comp.LocalizedJobTitle;
            userJobIcon = card.Comp.JobIcon;

            foreach (var department in card.Comp.JobDepartments)
                userJobDepartments.Add(Loc.GetString(ProtoMan.Index(department).Name));
        }

        // get health mob state
        var isAlive = false;
        if (TryComp(sensor.User.Value, out MobStateComponent? mobState))
            isAlive = !_mobStateSystem.IsDead(sensor.User.Value, mobState);

        // get mob total damage
        var totalDamage = _damageable.GetTotalDamage(sensor.User.Value).Int();

        // Get mob total damage crit threshold
        int? totalDamageThreshold = null;
        if (_mobThresholdSystem.TryGetThresholdForState(sensor.User.Value, MobState.Critical, out var critThreshold))
            totalDamageThreshold = critThreshold.Value.Int();

        // finally, form suit sensor status
        var status = new SuitSensorStatus(GetNetEntity(sensor.User.Value), GetNetEntity(ent.Owner), userName, userJob, userJobIcon, userJobDepartments);
        switch (sensor.Mode)
        {
            case SuitSensorMode.SensorBinary:
                status.IsAlive = isAlive;
                break;
            case SuitSensorMode.SensorVitals:
                status.IsAlive = isAlive;
                status.TotalDamage = totalDamage;
                status.TotalDamageThreshold = totalDamageThreshold;
                break;
            case SuitSensorMode.SensorCords:
                status.IsAlive = isAlive;
                status.TotalDamage = totalDamage;
                status.TotalDamageThreshold = totalDamageThreshold;
                EntityCoordinates coordinates;

                if (transform.GridUid != null)
                {
                    coordinates = new EntityCoordinates(transform.GridUid.Value,
                        Vector2.Transform(_transform.GetWorldPosition(transform),
                            _transform.GetInvWorldMatrix(transform.GridUid.Value)));
                }
                else if (transform.MapUid != null)
                {
                    coordinates = new EntityCoordinates(transform.MapUid.Value,
                        _transform.GetWorldPosition(transform));
                }
                else
                {
                    coordinates = EntityCoordinates.Invalid;
                }

                status.Coordinates = GetNetCoordinates(coordinates);
                break;
        }

        return status;
    }
}
